using System.Diagnostics.Metrics;

namespace ContractManagement.Domains.Services.ContractTemplate;

internal static class ContractPlaceholderMetrics
{
    private static readonly Meter Meter = new("ContractManagement.Placeholders", "1.0");
    internal static readonly Counter<long> Mutations = Meter.CreateCounter<long>("placeholder.definition.mutations");
    internal static readonly Counter<long> Resolved = Meter.CreateCounter<long>("placeholder.values.resolved");
    internal static readonly Counter<long> Defaulted = Meter.CreateCounter<long>("placeholder.values.defaulted");
    internal static readonly Counter<long> Failures = Meter.CreateCounter<long>("placeholder.values.failures");
    internal static readonly Histogram<double> ResolveDuration = Meter.CreateHistogram<double>("placeholder.resolve.duration", "ms");
}
