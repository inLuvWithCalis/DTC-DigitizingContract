namespace ContractManagement.API.Domains.DTOs.Responses.Admin;

public sealed class SystemHealthResponse
{
    public string Status { get; set; } = "Healthy";

    public DateTime GeneratedAt { get; set; }

    public ApiRuntimeHealthResponse Api { get; set; } = new();

    public DependencyHealthResponse CentralDatabase { get; set; } = new();

    public PrivateStorageHealthResponse PrivateStorage { get; set; } = new();

    public PdfRendererHealthResponse PdfRenderer { get; set; } = new();

    public OtpDeliveryHealthResponse OtpDelivery { get; set; } = new();

    public SessionStoreHealthResponse SessionStore { get; set; } = new();

    public int? FailedTenantCount { get; set; }
}

public sealed class ApiRuntimeHealthResponse
{
    public string Status { get; set; } = "Healthy";

    public string Version { get; set; } = "unknown";

    public DateTime StartedAt { get; set; }

    public long UptimeSeconds { get; set; }
}

public sealed class DependencyHealthResponse
{
    public string Status { get; set; } = "Unavailable";

    public string? Code { get; set; }
}

public sealed class PrivateStorageHealthResponse
{
    public string Status { get; set; } = "Unavailable";

    public bool Writable { get; set; }

    public bool MeetsCapacityThreshold { get; set; }

    public long? AvailableFreeSpaceBytes { get; set; }

    public long MinimumFreeSpaceBytes { get; set; }
}

public sealed class PdfRendererHealthResponse
{
    public string Status { get; set; } = "Unavailable";

    public string Mode { get; set; } = "LibreOffice";
}

public sealed class OtpDeliveryHealthResponse
{
    public string Status { get; set; } = "Healthy";

    public string ProviderMode { get; set; } = "Unknown";

    public int? BacklogCount { get; set; }

    public string BacklogCollection { get; set; } = "NotCollected";
}

public sealed class SessionStoreHealthResponse
{
    public string Status { get; set; } = "Healthy";

    public string Mode { get; set; } = "InMemory";
}
