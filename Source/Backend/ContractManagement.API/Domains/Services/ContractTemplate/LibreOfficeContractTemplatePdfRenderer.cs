using System.Diagnostics;
using System.ComponentModel;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;

namespace ContractManagement.Domains.Services.ContractTemplate;

/// <summary>
/// Process-isolated LibreOffice conversion for a generated template preview.
/// LibreOffice is a deployment dependency; it is deliberately not bundled.
/// </summary>
public sealed class LibreOfficeContractTemplatePdfRenderer
    : IContractTemplatePdfRenderer
{
    private static readonly SemaphoreSlim ConversionGate = new(1, 1);
    private readonly TemplatePdfRenderingOptions _options;

    public LibreOfficeContractTemplatePdfRenderer(
        IOptions<TemplatePdfRenderingOptions> options)
    {
        _options = options.Value;
    }

    public async Task<byte[]> ConvertPreviewToPdfAsync(
        byte[] previewDocx,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previewDocx);
        VerifyOpenableDocx(previewDocx);

        var executable = _options.ExecutablePath?.Trim();
        if (string.IsNullOrWhiteSpace(executable) || !System.IO.File.Exists(executable))
        {
            throw new ContractTemplatePdfRenderingException(
                "PdfConverterUnavailable",
                "LibreOffice executable chưa được cấu hình hoặc không khả dụng.");
        }

        await ConversionGate.WaitAsync(cancellationToken);
        var root = Path.Combine(Path.GetTempPath(),
            $"contract-template-pdf-{Guid.NewGuid():N}");
        try
        {
            var inputDirectory = Path.Combine(root, "input");
            var outputDirectory = Path.Combine(root, "output");
            var profileDirectory = Path.Combine(root, "profile");
            Directory.CreateDirectory(inputDirectory);
            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory(profileDirectory);
            var inputPath = Path.Combine(inputDirectory, "preview.docx");
            await System.IO.File.WriteAllBytesAsync(inputPath, previewDocx, cancellationToken);

            using var process = new Process
            {
                StartInfo = CreateStartInfo(executable, inputPath, outputDirectory,
                    profileDirectory)
            };
            try
            {
                if (!process.Start())
                {
                    throw new ContractTemplatePdfRenderingException(
                        "PdfConverterUnavailable",
                        "Không thể khởi động LibreOffice để tạo PDF.");
                }
            }
            catch (ContractTemplatePdfRenderingException)
            {
                throw;
            }
            catch (Exception exception) when (exception is Win32Exception
                or UnauthorizedAccessException or IOException)
            {
                throw new ContractTemplatePdfRenderingException(
                    "PdfConverterUnavailable",
                    "Không thể khởi động LibreOffice để tạo PDF.");
            }

            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeout.Token);
            try
            {
                await process.WaitForExitAsync(linked.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested
                && !cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw new ContractTemplatePdfRenderingException(
                    "PdfRenderTimeout",
                    "LibreOffice vượt quá thời gian tạo PDF cho phép.");
            }

            var outputPath = Path.Combine(outputDirectory, "preview.pdf");
            if (process.ExitCode != 0 || !System.IO.File.Exists(outputPath))
            {
                throw new ContractTemplatePdfRenderingException(
                    "PdfRenderFailed",
                    "LibreOffice không tạo được PDF preview hợp lệ.");
            }

            var info = new FileInfo(outputPath);
            if (info.Length == 0 || info.Length > _options.MaxOutputBytes)
            {
                throw new ContractTemplatePdfRenderingException(
                    "PdfRenderInvalid",
                    "PDF preview rỗng hoặc vượt quá giới hạn 25 MiB.");
            }

            var pdf = await System.IO.File.ReadAllBytesAsync(outputPath, cancellationToken);
            if (pdf.Length < 5 || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            {
                throw new ContractTemplatePdfRenderingException(
                    "PdfRenderInvalid",
                    "PDF preview không có header %PDF- hợp lệ.");
            }

            return pdf;
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch
            {
                // Temporary cleanup is operational only and cannot change a
                // conversion or publish result.
            }

            ConversionGate.Release();
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string executable,
        string inputPath,
        string outputDirectory,
        string profileDirectory)
    {
        var profileUri = new Uri(profileDirectory + Path.DirectorySeparatorChar)
            .AbsoluteUri;
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        info.ArgumentList.Add("--headless");
        info.ArgumentList.Add("--nologo");
        info.ArgumentList.Add("--nodefault");
        info.ArgumentList.Add("--norestore");
        info.ArgumentList.Add($"-env:UserInstallation={profileUri}");
        info.ArgumentList.Add("--convert-to");
        info.ArgumentList.Add("pdf:writer_pdf_Export");
        info.ArgumentList.Add("--outdir");
        info.ArgumentList.Add(outputDirectory);
        info.ArgumentList.Add(inputPath);
        return info;
    }

    private static void VerifyOpenableDocx(byte[] document)
    {
        try
        {
            using var stream = new MemoryStream(document, writable: false);
            using var wordDocument = WordprocessingDocument.Open(stream, false);
            if (wordDocument.MainDocumentPart?.Document?.Body is null)
            {
                throw new InvalidDataException();
            }
        }
        catch (Exception exception) when (exception is OpenXmlPackageException
            or InvalidDataException or IOException)
        {
            throw new ContractTemplatePdfRenderingException(
                "PreviewDocxInvalid",
                "DOCX preview không thể mở để chuyển sang PDF.");
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The temporary directory cleanup remains best effort.
        }
    }
}
