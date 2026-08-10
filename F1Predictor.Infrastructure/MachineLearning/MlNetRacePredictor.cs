using F1Predictor.Application.Abstractions.MachineLearning;
using F1Predictor.Domain.Predictions;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace F1Predictor.Infrastructure.MachineLearning;

/// <summary>
/// Serves predictions from the saved model files, reloading them when training overwrites them.
/// </summary>
/// <remarks>
/// A prediction engine is built once per model file and reused, rather than per prediction —
/// building one is expensive and doing it per row is the classic way to make a served model
/// look slow. <see cref="PredictionEngine{TSrc,TDst}"/> is not thread-safe, so calls are
/// serialised through a lock; at a couple of dozen rows per request that costs nothing.
/// <para>
/// Microsoft.Extensions.ML's <c>PredictionEnginePool</c> would be the off-the-shelf answer,
/// but it wants its model file to exist at startup. Here the models only appear after the
/// first training run, so loading is deferred until a prediction is actually asked for.
/// </para>
/// </remarks>
internal sealed class MlNetRacePredictor(IOptions<ModelStorageOptions> options) : IRacePredictor, IDisposable
{
    private readonly MLContext _mlContext = new();
    private readonly ModelStorageOptions _options = options.Value;
    private readonly Lock _gate = new();

    private LoadedModel? _podium;
    private LoadedModel? _points;
    private bool _disposed;

    public bool ModelsAvailable =>
        File.Exists(_options.PathFor(PredictionTarget.Podium)) &&
        File.Exists(_options.PathFor(PredictionTarget.PointsFinish));

    public RaceProbabilities Predict(DriverRaceFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        var input = RaceFeatureInput.From(feature);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _podium = EnsureCurrent(_podium, PredictionTarget.Podium);
            _points = EnsureCurrent(_points, PredictionTarget.PointsFinish);

            return new RaceProbabilities(
                _podium.Engine.Predict(input).Probability,
                _points.Engine.Predict(input).Probability);
        }
    }

    /// <summary>
    /// Returns the cached model, reloading it if training has written a newer file since.
    /// </summary>
    private LoadedModel EnsureCurrent(LoadedModel? cached, PredictionTarget target)
    {
        var path = _options.PathFor(target);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"No trained model at '{path}'. Train the models before requesting predictions.");
        }

        var writtenAt = File.GetLastWriteTimeUtc(path);

        if (cached is not null && cached.WrittenAt == writtenAt)
        {
            return cached;
        }

        cached?.Engine.Dispose();

        var transformer = _mlContext.Model.Load(path, out _);
        var engine = _mlContext.Model.CreatePredictionEngine<RaceFeatureInput, RacePredictionOutput>(transformer);

        return new LoadedModel(engine, writtenAt);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _podium?.Engine.Dispose();
            _points?.Engine.Dispose();
            _disposed = true;
        }
    }

    private sealed record LoadedModel(
        PredictionEngine<RaceFeatureInput, RacePredictionOutput> Engine,
        DateTime WrittenAt);
}
