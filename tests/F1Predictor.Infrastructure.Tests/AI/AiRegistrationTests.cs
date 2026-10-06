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
    public async Task AddAi_NoProviderConfigured_ChatUnavailableAndClientThrows()
    {
        using var provider = Build();

        var capabilities = provider.GetRequiredService<IAiCapabilities>();
        var client = provider.GetRequiredService<IChatClient>();

        capabilities.ChatAvailable.Should().BeFalse();
        capabilities.Provider.Should().Be("None");
        capabilities.Model.Should().BeNull();
        capabilities.EmbeddingsAvailable.Should().BeFalse();

        // Awaited directly (rather than via a stored Func<Task> delegate) so CA2025/MA0134 can
        // verify the task completes, inside this using scope, before 'provider' is disposed.
        InvalidOperationException? thrown = null;
        try
        {
            await client.GetResponseAsync("hi");
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        thrown.Should().NotBeNull();
    }

    [Fact]
    public void AddAi_Disabled_OverridesConfiguredProvidersAndSkipsKeyValidation()
    {
        // No ApiKey on purpose: a paused AI layer must not fail validation for a provider it
        // is not going to use.
        using var provider = Build(
            new("Ai:Enabled", "false"),
            new("Ai:Provider", "OpenAi"),
            new("Ai:Embeddings:Provider", "OpenAi"));

        var capabilities = provider.GetRequiredService<IAiCapabilities>();

        capabilities.ChatAvailable.Should().BeFalse();
        capabilities.EmbeddingsAvailable.Should().BeFalse();
        capabilities.Provider.Should().Be("None");
        capabilities.Model.Should().BeNull();
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
    public void AddAi_OpenAiConfigured_ReportsModelAndResolvesClient()
    {
        using var provider = Build(
            new("Ai:Provider", "OpenAi"),
            new("Ai:OpenAi:ApiKey", "sk-test"),
            new("Ai:OpenAi:ChatModel", "ai/llama3.2"),
            new("Ai:OpenAi:Endpoint", "http://localhost:12434/engines/llama.cpp/v1"));

        var capabilities = provider.GetRequiredService<IAiCapabilities>();

        capabilities.ChatAvailable.Should().BeTrue();
        capabilities.Provider.Should().Be("OpenAi");
        capabilities.Model.Should().Be("ai/llama3.2");
        provider.GetRequiredService<IChatClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddAi_OpenAiConfigured_NoEndpointStillResolvesClient()
    {
        using var provider = Build(
            new("Ai:Provider", "OpenAi"),
            new("Ai:OpenAi:ApiKey", "sk-test"),
            new("Ai:OpenAi:ChatModel", "gpt-5-mini"));

        var capabilities = provider.GetRequiredService<IAiCapabilities>();

        capabilities.ChatAvailable.Should().BeTrue();
        capabilities.Model.Should().Be("gpt-5-mini");
        provider.GetRequiredService<IChatClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddAi_EmbeddingsIndependentOfChatProvider_OllamaChatWithOpenAiEmbeddings()
    {
        using var provider = Build(
            new("Ai:Provider", "Ollama"),
            new("Ai:Embeddings:Provider", "OpenAi"),
            new("Ai:Embeddings:Model", "text-embedding-3-small"),
            new("Ai:OpenAi:ApiKey", "sk-test"));

        var capabilities = provider.GetRequiredService<IAiCapabilities>();

        capabilities.ChatAvailable.Should().BeTrue();
        capabilities.Provider.Should().Be("Ollama");
        capabilities.EmbeddingsAvailable.Should().BeTrue();
        capabilities.EmbeddingModel.Should().Be("text-embedding-3-small");
        provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddAi_NoEmbeddingsConfigured_GeneratorThrowsOnUse()
    {
        using var provider = Build();
        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        InvalidOperationException? thrown = null;
        try
        {
            await generator.GenerateAsync(["hello"]);
        }
        catch (InvalidOperationException ex)
        {
            thrown = ex;
        }

        thrown.Should().NotBeNull();
    }

    [Fact]
    public void AddAi_OllamaConfigured_NormalisesEndpointTrailingSlash()
    {
        using var provider = Build(
            new("Ai:Provider", "Ollama"),
            new("Ai:Ollama:Endpoint", "http://host/ollama"));

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(OllamaHealthCheck.HttpClientName);

        client.BaseAddress.Should().Be(new Uri("http://host/ollama/"));
    }
}
