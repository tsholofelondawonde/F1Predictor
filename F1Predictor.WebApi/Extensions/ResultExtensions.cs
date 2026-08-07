using SharedKernel;

namespace F1Predictor.WebApi.Extensions;

/// <summary>
/// Provides extension methods for pattern matching on <see cref="Result"/> and <see cref="Result{TIn}"/> types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes the appropriate delegate based on the success or failure of the <see cref="Result"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the result returned by the delegates.</typeparam>
    /// <param name="result">The <see cref="Result"/> to match on.</param>
    /// <param name="onSuccess">Delegate to execute if the result is successful.</param>
    /// <param name="onFailure">Delegate to execute if the result is a failure, receiving the failed <see cref="Result"/>.</param>
    /// <returns>The value returned by either <paramref name="onSuccess"/> or <paramref name="onFailure"/>.</returns>
    public static TOut Match<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Result, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess() : onFailure(result);
    }

    /// <summary>
    /// Executes the appropriate delegate based on the success or failure of the <see cref="Result{TIn}"/>.
    /// </summary>
    /// <typeparam name="TIn">The type of the value contained in the result.</typeparam>
    /// <typeparam name="TOut">The type of the result returned by the delegates.</typeparam>
    /// <param name="result">The <see cref="Result{TIn}"/> to match on.</param>
    /// <param name="onSuccess">Delegate to execute if the result is successful, receiving the value.</param>
    /// <param name="onFailure">Delegate to execute if the result is a failure, receiving the failed <see cref="Result{TIn}"/>.</param>
    /// <returns>The value returned by either <paramref name="onSuccess"/> or <paramref name="onFailure"/>.</returns>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Result<TIn>, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result);
    }
}
