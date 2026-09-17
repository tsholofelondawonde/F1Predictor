using F1Predictor.Application.Abstractions.AI;
using F1Predictor.Infrastructure.AI;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace F1Predictor.Infrastructure.Tests.AI;

public sealed class AiRegistrationTests
{
    private static ServiceProvider Build(params KeyValuePair<string, string?>[] settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAi(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddAi_NoProviderConfigured_ChatUnavailableAndClientThrows()
    {
        using var provider = Build();

        var capabilities = provider.GetRequiredService<IAiCapabilities>();
        var client = provider.GetRequiredService<IChatClient>();

        capabilities.ChatAvailable.Should().BeFalse();
        capabilities.Provider.Should().Be("None");
        capabilities.Model.Should().BeNull();
        capabilities.EmbeddingsAvailable.Should().BeFalse();

        var act = () => client.GetResponseAsync("hi");
        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void AddAi_OllamaConfigured_ReportsModelAndResolvesClient()
    {
        using var provider = Build(
            new("Ai:Provider", "Ollama"),
            new("Ai:Ollama:Model", "llama3.1:8b"));

        var capabilities = provider.GetRequiredService<IAiCapabilities>();

        capabilities.ChatAvailable.Should().BeTrue();
        capabilities.Provider.Should().Be("Ollama");
        capabilities.Model.Should().Be("llama3.1:8b");
        provider.GetRequiredService<IChatClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddAi_OpenAiConfigured_FailsFastUntilStage4()
    {
        var act = () => Build(new KeyValuePair<string, string?>("Ai:Provider", "OpenAi")).GetRequiredService<IChatClient>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*OpenAi*not implemented*");
    }
}
