using F1Predictor.Infrastructure;
using F1Predictor.Infrastructure.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Xunit;

namespace F1Predictor.Infrastructure.Tests.Ingestion;

public sealed class SchedulerRegistrationTests
{
    [Fact]
    public async Task AddScheduler_AllTriggersDisabled_StillInitialisesWithBothJobsRegistered()
    {
        // Ai:Enabled=false folds the provider to None, so AnalysisRefreshJob gets no interval
        // trigger; IngestionScheduler:Enabled=false drops the other one. Both jobs are still
        // registered so they can be TriggerJob'd on demand, which Quartz only allows if durable.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Enabled"] = "false",
                ["IngestionScheduler:Enabled"] = "false",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScheduler(configuration);

        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();

        (await scheduler.Exists(AnalysisRefreshJob.Key)).Should().BeTrue();
        (await scheduler.Exists(new JobKey(nameof(SeasonIngestionCoordinatorJob)))).Should().BeTrue();
    }
}
