namespace ContractManagement.Domains.Interfaces.File;

public interface IPrivateFileStorageHealthProbe
{
    Task<PrivateFileStorageHealthResult> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}

public sealed record PrivateFileStorageHealthResult(
    bool IsWritable,
    bool MeetsCapacityThreshold,
    long? AvailableFreeSpaceBytes,
    long MinimumFreeSpaceBytes);
