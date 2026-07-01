using Microsoft.Extensions.Configuration;

namespace ContractManagement.Infrastructure.DesignTime;

/// <summary>
/// Tìm appsettings.json của project Web API
/// khi chạy dotnet ef.
/// </summary>
internal static class DesignTimeConfiguration
{
    public static IConfigurationRoot Build()
    {
        string currentDirectory =
            Directory.GetCurrentDirectory();

        string[] possiblePaths =
        {
            /*
             * Khi chạy command trong project API.
             */
            currentDirectory,

            /*
             * Khi chạy command tại solution root.
             */
            Path.Combine(
                currentDirectory,
                "ContractManagement"),

            /*
             * Khi chạy command trong Infrastructure.
             */
            Path.GetFullPath(
                Path.Combine(
                    currentDirectory,
                    "..",
                    "ContractManagement"))
        };

        string? basePath =
            possiblePaths.FirstOrDefault(path =>
                File.Exists(
                    Path.Combine(
                        path,
                        "appsettings.json")));

        if (basePath is null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy appsettings.json "
                + "của project ContractManagement.");
        }

        string environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(
                "appsettings.json",
                optional: false)
            .AddJsonFile(
                $"appsettings.{environment}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}