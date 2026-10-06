using F1Predictor.Domain.Championship;
using FluentAssertions;
using Xunit;

namespace F1Predictor.Application.Tests.Championship;

public sealed class DriverFormModelTests
{
    [Fact]
    public void Fit_DriversWhoNeverLose_StaysFiniteAndBounded()
    {
        var forms = DriverFormModel.Fit(DominantSeason.Outcomes(races: 6));

        var strengths = forms.Select(f => f.Strength).Order().ToList();
        var median = strengths[strengths.Count / 2];

        forms.Single(f => f.DriverNumber == 1).Strength.Should().BeLessThan(50 * median);
        strengths.Should().NotContain(s => s <= 1.001e-3, "the floor is a safety net, not a result");
    }
}
