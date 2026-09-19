using F1Predictor.Infrastructure.AI;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace F1Predictor.Infrastructure.Tests.AI;

public sealed class AiOptionsValidationTests
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
    public void Validate_OpenAiWithoutApiKey_FailsOnStart()
    {
        using var provider = Build(new KeyValuePair<string, string?>("Ai:Provider", "OpenAi"));

        var act = () => provider.GetRequiredService<IOptions<AiOptions>>().Value;

        act.Should().Throw<OptionsValidationException>().WithMessage("*Ai:OpenAi:ApiKey*");
    }

    [Fact]
    public void Validate_EmbeddingsOpenAiWithoutApiKey_FailsOnStart()
    {
        using var provider = Build(new KeyValuePair<string, string?>("Ai:Embeddings:Provider", "OpenAi"));

        var act = () => provider.GetRequiredService<IOptions<AiOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }
}
