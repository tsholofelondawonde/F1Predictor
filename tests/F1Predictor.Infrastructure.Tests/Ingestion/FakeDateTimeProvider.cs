using SharedKernel;

namespace F1Predictor.Infrastructure.Tests.Ingestion;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
}
