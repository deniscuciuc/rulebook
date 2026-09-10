using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rulebook.Definitions;

/// <summary>
/// Registers a source of decision definitions. Pair with
/// <see cref="Rulebook.Extensions.DecisionServiceCollectionExtensions.AddDecisions"/>,
/// which registers the engine itself.
/// </summary>
public static class DecisionDefinitionServiceCollectionExtensions
{
    /// <summary>
    /// Binds definitions from the <c>Rulebook</c> configuration section and serves them
    /// through <see cref="IDecisionDefinitionProvider"/>.
    /// </summary>
    /// <remarks>
    /// Because this goes through <c>IOptionsMonitor</c>, a reloadable configuration source
    /// (<c>reloadOnChange: true</c> on a JSON file, say) updates the definitions in place —
    /// no restart, and no dependency on any particular configuration system.
    /// </remarks>
    public static IServiceCollection AddDecisionDefinitions(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = DecisionDefinitionSet.SectionPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DecisionDefinitionSet>(configuration.GetSection(sectionPath));
        services.TryAddSingleton<IDecisionDefinitionProvider, OptionsDecisionDefinitionProvider>();
        return services;
    }

    /// <summary>
    /// Registers definitions supplied in code.
    /// </summary>
    public static IServiceCollection AddDecisionDefinitions(
        this IServiceCollection services,
        Action<DecisionDefinitionSet> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.TryAddSingleton<IDecisionDefinitionProvider, OptionsDecisionDefinitionProvider>();
        return services;
    }

    /// <summary>
    /// Registers a custom definition source — a database, a feature-flag service, or a
    /// configuration framework of your own.
    /// </summary>
    public static IServiceCollection AddDecisionDefinitionProvider<TProvider>(
        this IServiceCollection services)
        where TProvider : class, IDecisionDefinitionProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDecisionDefinitionProvider, TProvider>();
        return services;
    }
}
