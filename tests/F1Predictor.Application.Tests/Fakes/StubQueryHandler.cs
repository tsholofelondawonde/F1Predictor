using F1Predictor.Application.Abstractions.Messaging;
using SharedKernel;

namespace F1Predictor.Application.Tests.Fakes;

/// <summary>
/// A query handler that always fails, for wiring up dependencies (e.g. <c>AnalystTools</c>)
/// that a test does not otherwise exercise.
/// </summary>
internal sealed class StubQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<TResponse>(Error.Problem("Stub", "stub")));
}
