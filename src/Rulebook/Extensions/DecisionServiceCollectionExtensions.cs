using Microsoft.Extensions.DependencyInjection;
using Rulebook.Compilation;
using Rulebook.Features.ABTesting;
using Rulebook.Features.FeatureFlags;
using Rulebook.Features.Segmentation;
using Rulebook.Operators;
using Rulebook.Parsing;

namespace Rulebook.Extensions;

public static class DecisionServiceCollectionExtensions
{
    public static IServiceCollection AddDecisions(
        this IServiceCollection services,
        Action<IOperatorRegistry>? configureOperators = null)
    {
        var operatorRegistry = new OperatorRegistry();
        configureOperators?.Invoke(operatorRegistry);

        services.AddSingleton<IOperatorRegistry>(operatorRegistry);
        services.AddSingleton<IExpressionParser, ExpressionParser>();
        services.AddSingleton<IBucketAssigner, DeterministicBucketAssigner>();
        services.AddSingleton<RuleCompiler>();
        services.AddSingleton<IDecisionEngine, DecisionEngine>();
        services.AddSingleton<SegmentEvaluator>();
        services.AddSingleton<FeatureFlagEvaluator>();
        services.AddSingleton<ExperimentEvaluator>();

        return services;
    }
}
