using Quartz;

namespace F1Predictor.Infrastructure.Tests.Ingestion;

/// <summary>
/// The bare minimum <see cref="IJobExecutionContext"/> needed to call
/// <see cref="Infrastructure.Ingestion.SeasonIngestionCoordinatorJob.Execute"/> directly in a
/// test, without standing up a real Quartz scheduler. Every member the job does not touch throws.
/// </summary>
internal sealed class FakeJobExecutionContext(IScheduler? scheduler = null) : IJobExecutionContext
{
    public CancellationToken CancellationToken => CancellationToken.None;

    public IScheduler Scheduler => scheduler ?? throw new NotSupportedException();
    public ITrigger Trigger => throw new NotSupportedException();
    public ICalendar? Calendar => throw new NotSupportedException();
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => throw new NotSupportedException();
    public int RefireCount => 0;
    public int RetryAttempt => 0;
    public JobDataMap MergedJobDataMap => throw new NotSupportedException();
    public IJobDetail JobDetail => throw new NotSupportedException();
    public IJob JobInstance => throw new NotSupportedException();
    public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => null;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => null;
    public string FireInstanceId => "fake";
    public object? Result { get; set; }
    public TimeSpan JobRunTime => TimeSpan.Zero;
}
