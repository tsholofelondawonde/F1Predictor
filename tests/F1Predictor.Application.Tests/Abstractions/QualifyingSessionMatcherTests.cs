using F1Predictor.Application.Abstractions.OpenF1;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Abstractions;

public sealed class QualifyingSessionMatcherTests
{
    [Fact]
    public void Find_RaceWeekend_ReturnsQualifyingSession()
    {
        OpenF1Session[] sessions =
        [
            new() { SessionKey = 1, SessionType = "Practice", SessionName = "Practice 1" },
            new() { SessionKey = 2, SessionType = "Qualifying", SessionName = "Qualifying" },
            new() { SessionKey = 3, SessionType = "Race", SessionName = "Race" },
        ];

        var result = QualifyingSessionMatcher.Find(sessions, isSprint: false);

        result.Should().NotBeNull();
        result.SessionKey.Should().Be(2);
    }

    [Fact]
    public void Find_SprintWeekend_ReturnsSprintQualifyingSession()
    {
        OpenF1Session[] sessions =
        [
            new() { SessionKey = 1, SessionType = "Qualifying", SessionName = "Sprint Qualifying" },
            new() { SessionKey = 2, SessionType = "Race", SessionName = "Sprint" },
            new() { SessionKey = 3, SessionType = "Race", SessionName = "Race" },
        ];

        var result = QualifyingSessionMatcher.Find(sessions, isSprint: true);

        result.Should().NotBeNull();
        result.SessionKey.Should().Be(1);
    }

    [Fact]
    public void Find_NoMatchingSession_ReturnsNull()
    {
        OpenF1Session[] sessions =
        [
            new() { SessionKey = 1, SessionType = "Practice", SessionName = "Practice 1" },
        ];

        var result = QualifyingSessionMatcher.Find(sessions, isSprint: false);

        result.Should().BeNull();
    }
}
