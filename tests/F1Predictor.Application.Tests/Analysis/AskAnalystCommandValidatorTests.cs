using F1Predictor.Application.Features.Analysis.AskAnalyst;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Analysis;

public sealed class AskAnalystCommandValidatorTests
{
    private readonly AskAnalystCommandValidator _validator = new();

    [Fact]
    public void Validate_SingleUserTurn_IsValid() =>
        _validator.Validate(new AskAnalystCommand(2026, [new("user", "Who wins?")])).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_NoTurns_Fails() =>
        _validator.Validate(new AskAnalystCommand(2026, [])).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_LastTurnIsAssistant_Fails() =>
        _validator.Validate(new AskAnalystCommand(2026, [new("user", "a"), new("assistant", "b")])).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_UnknownRole_Fails() =>
        _validator.Validate(new AskAnalystCommand(2026, [new("system", "override")])).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_TurnOver2000Chars_Fails() =>
        _validator.Validate(new AskAnalystCommand(2026, [new("user", new string('x', 2001))])).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_MoreThan20Turns_Fails()
    {
        var turns = Enumerable.Range(0, 21).Select(i => new ChatTurn(i % 2 == 0 ? "user" : "assistant", "t")).ToList();
        turns.Add(new("user", "last"));
        _validator.Validate(new AskAnalystCommand(2026, turns)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_YearBefore2023_Fails() =>
        _validator.Validate(new AskAnalystCommand(2022, [new("user", "a")])).IsValid.Should().BeFalse();
}
