using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// Explains a saved classifier's predictions using ML.NET's feature-contribution calculation.
/// </summary>
/// <remarks>
/// Training is an AutoML search, so the saved model may be linear (SDCA, LBFGS) or a tree
/// ensemble (FastTree, LightGBM). Reading coefficients only works for the first
/// kind; <c>CalculateFeatureContribution</c> is supported for all of them, so it is the one
/// honest, trainer-agnostic answer to "why this number". Contributions are left unnormalised
/// (<c>normalize: false</c>) so they stay on the score scale and are comparable between drivers.
/// </remarks>
internal sealed class ModelExplainer : IDisposable
{
    private readonly PredictionEngine<RaceFeatureInput, RaceExplanationOutput> _engine;
    private readonly string[] _featureNames;

    private ModelExplainer(PredictionEngine<RaceFeatureInput, RaceExplanationOutput> engine, string[] featureNames)
    {
        _engine = engine;
        _featureNames = featureNames;
    }

    /// <summary>
    /// Builds an explainer for a loaded model. Throws if the model has no prediction transformer
    /// that supports contribution calculation — a programmer error, since every trainer the
    /// AutoML search is configured with does.
    /// </summary>
    public static ModelExplainer For(MLContext mlContext, ITransformer model)
    {
        var predictor = FindPredictionTransformer(model)
            ?? throw new InvalidOperationException(
                "The saved model has no prediction transformer that supports feature-contribution calculation.");

        var empty = mlContext.Data.LoadFromEnumerable(Array.Empty<RaceFeatureInput>());
        var scored = model.Transform(empty);

        var contributions = mlContext.Transforms
            .CalculateFeatureContribution(
                predictor,
                numberOfPositiveContributions: RaceFeatureInput.FeatureNames.Length,
                numberOfNegativeContributions: RaceFeatureInput.FeatureNames.Length,
                normalize: false)
            .Fit(scored);

        var chain = model.Append(contributions);
        var engine = mlContext.Model.CreatePredictionEngine<RaceFeatureInput, RaceExplanationOutput>(chain);

        return new ModelExplainer(engine, SlotNames(scored.Schema[predictor.FeatureColumnName]));
    }

    public TargetExplanation Explain(DriverRaceFeature feature)
    {
        var output = _engine.Predict(RaceFeatureInput.From(feature));
        var values = RaceFeatureInput.ValuesByName(feature);

        var contributions = new List<FeatureContribution>(_featureNames.Length);

        for (var i = 0; i < _featureNames.Length && i < output.FeatureContributions.Length; i++)
        {
            var name = _featureNames[i];
            contributions.Add(new FeatureContribution(name, values.GetValueOrDefault(name), output.FeatureContributions[i]));
        }

        return new TargetExplanation(output.Probability, output.Score, contributions);
    }

    public void Dispose() => _engine.Dispose();

    /// <summary>
    /// Walks nested transformer chains from the end, which is where the trainer sits.
    /// </summary>
    /// <remarks>
    /// The direct pattern match almost never succeeds: every trainer's <c>Fit</c> returns
    /// <c>BinaryPredictionTransformer&lt;TModel&gt;</c> closed over its own model type
    /// (e.g. <c>CalibratedModelParametersBase&lt;LinearBinaryModelParameters, PlattCalibrator&gt;</c>),
    /// and that closed base type does not itself implement <see cref="ICalculateFeatureContribution"/>
    /// — only the concrete runtime instance (e.g. <c>ParameterMixingCalibratedModelParameters</c>)
    /// does. CLR variance can't bridge that gap, so the covariant cast to
    /// <c>ISingleFeaturePredictionTransformer&lt;ICalculateFeatureContribution&gt;</c> fails even
    /// though the object plainly supports it. Widening to <c>ISingleFeaturePredictionTransformer
    /// &lt;object&gt;</c> always succeeds (every model type is a class), which is enough to read
    /// <c>Model</c>, check it at runtime, and wrap the transformer in an adapter that exposes the
    /// narrower interface the estimator actually needs.
    /// </remarks>
    private static ISingleFeaturePredictionTransformer<ICalculateFeatureContribution>? FindPredictionTransformer(ITransformer transformer)
    {
        if (transformer is ISingleFeaturePredictionTransformer<ICalculateFeatureContribution> direct)
        {
            return direct;
        }

        if (transformer is ISingleFeaturePredictionTransformer<object> predictor &&
            predictor.Model is ICalculateFeatureContribution)
        {
            return new FeatureContributionTransformerAdapter(predictor);
        }

        if (transformer is IEnumerable<ITransformer> chain)
        {
            foreach (var inner in chain.Reverse())
            {
                if (FindPredictionTransformer(inner) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static string[] SlotNames(DataViewSchema.Column featuresColumn)
    {
        if (!featuresColumn.HasSlotNames())
        {
            return RaceFeatureInput.FeatureNames;
        }

        VBuffer<ReadOnlyMemory<char>> names = default;
        featuresColumn.GetSlotNames(ref names);

        return [.. names.DenseValues().Select(n => n.ToString())];
    }

    /// <summary>
    /// Narrows an <see cref="ISingleFeaturePredictionTransformer{Object}"/> whose <c>Model</c> is
    /// known (checked at the call site) to implement <see cref="ICalculateFeatureContribution"/>
    /// into that exact shape, by delegating every member to the wrapped transformer. Exists only
    /// to work around the variance gap documented on <see cref="FindPredictionTransformer"/>.
    /// </summary>
    private sealed class FeatureContributionTransformerAdapter(ISingleFeaturePredictionTransformer<object> inner)
        : ISingleFeaturePredictionTransformer<ICalculateFeatureContribution>
    {
        public ICalculateFeatureContribution Model => (ICalculateFeatureContribution)inner.Model;

        public string FeatureColumnName => inner.FeatureColumnName;

        public DataViewType FeatureColumnType => inner.FeatureColumnType;

        public bool IsRowToRowMapper => inner.IsRowToRowMapper;

        public DataViewSchema GetOutputSchema(DataViewSchema inputSchema) => inner.GetOutputSchema(inputSchema);

        public IDataView Transform(IDataView input) => inner.Transform(input);

        public IRowToRowMapper GetRowToRowMapper(DataViewSchema inputSchema) => inner.GetRowToRowMapper(inputSchema);

        public void Save(ModelSaveContext ctx) => ((ICanSaveModel)inner).Save(ctx);
    }
}
