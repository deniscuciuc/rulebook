namespace Rulebook.Definitions;

/// <summary>
/// Serves decision definitions to the engine. Implement this to load them from wherever
/// they live — a database, a feature-flag service, a spreadsheet.
/// </summary>
/// <remarks>
/// The built-in <see cref="OptionsDecisionDefinitionProvider"/> reads from
/// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}"/>, which means any
/// configuration provider that supports reload — a JSON file, an environment variable
/// source, a custom one — gives you hot reload with no extra code.
/// </remarks>
public interface IDecisionDefinitionProvider
{
    Task<DecisionDefinition?> GetDefinitionAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<DecisionDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SegmentDefinition>> GetSegmentsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FeatureFlagDefinition>> GetFeatureFlagsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ExperimentDefinition>> GetExperimentsAsync(CancellationToken ct = default);
}
