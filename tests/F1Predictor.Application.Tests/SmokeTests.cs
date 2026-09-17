using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void TestRunner_Runs_Passes() => true.Should().BeTrue();
}
