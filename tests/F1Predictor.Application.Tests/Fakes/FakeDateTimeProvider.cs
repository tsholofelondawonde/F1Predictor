using SharedKernel;

namespace F1Predictor.Application.Tests.Fakes;

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = DateTime.SpecifyKind(new DateTime(2026, 9, 17, 10, 0, 0), DateTimeKind.Utc);
}
