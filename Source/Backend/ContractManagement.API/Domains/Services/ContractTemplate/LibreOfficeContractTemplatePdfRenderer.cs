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

        var executable = ResolveExecutablePath(_options.ExecutablePath);
        if (executable is null)
        {
            throw new ContractTemplatePdfRenderingException(
                "PdfConverterUnavailable",
                "Không tìm thấy LibreOffice. Hãy cài LibreOffice hoặc cấu hình " +
                "TemplatePdfRendering:ExecutablePath tới soffice executable.");
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

    internal static string? ResolveExecutablePath(string? configuredPath)
    {
        var configured = configuredPath?.Trim().Trim('"');
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (System.IO.File.Exists(configured))
            {
                return Path.GetFullPath(configured);
            }

            var configuredFromPath = FindOnPath(configured);
            if (configuredFromPath is not null)
            {
                return configuredFromPath;
            }
        }

        foreach (var candidate in GetDefaultExecutableCandidates())
        {
            if (System.IO.File.Exists(candidate))
            {
                return candidate;
            }
        }

        return FindOnPath(OperatingSystem.IsWindows()
            ? "soffice.exe"
            : "libreoffice");
    }

    private static IEnumerable<string> GetDefaultExecutableCandidates()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86);

            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                yield return Path.Combine(programFiles, "LibreOffice", "program",
                    "soffice.exe");
            }

            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                yield return Path.Combine(programFilesX86, "LibreOffice", "program",
                    "soffice.exe");
            }

            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return "/Applications/LibreOffice.app/Contents/MacOS/soffice";
            yield break;
        }

        yield return "/usr/bin/libreoffice";
        yield return "/usr/local/bin/libreoffice";
        yield return "/snap/bin/libreoffice";
    }

    private static string? FindOnPath(string executableName)
    {
        if (Path.IsPathRooted(executableName) ||
            executableName.IndexOfAny(new[] { Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar }) >= 0)
        {
            return null;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var directory in path.Split(Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim('"'), executableName);
                if (System.IO.File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            catch (Exception exception) when (exception is ArgumentException
                or NotSupportedException or PathTooLongException)
            {
                // Ignore malformed PATH entries and continue with the remaining entries.
            }
        }

        return null;
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
