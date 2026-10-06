using F1Predictor.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace F1Predictor.Application.Tests.Fakes;

internal static class InMemoryDb
{
    public static ApplicationDbContext Create() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options, domainEventsDispatcher: null);
}
