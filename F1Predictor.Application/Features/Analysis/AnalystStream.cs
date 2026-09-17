using System.Runtime.CompilerServices;
using F1Predictor.Application.Features.Analysis.AskAnalyst;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace F1Predictor.Application.Features.Analysis;

/// <summary>
/// Turns the chat client's update stream into the small event vocabulary the SSE endpoint
/// sends. Errors become a final "error" event with a generic message — the exception is
/// logged here, never serialised to the client.
/// </summary>
internal static class AnalystStream
{
    public static async IAsyncEnumerable<AnalystEvent> Map(
        IAsyncEnumerable<ChatResponseUpdate> updates, ILogger logger, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = updates.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            bool moved;
            bool faulted = false;
            try
            {
                moved = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or IOException
                && !cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Analyst stream failed.");
                moved = false;
                faulted = true;
            }

            if (faulted)
            {
                yield return AnalystEvent.Error("The analyst stopped responding. Try again.");
                yield break;
            }

            if (!moved) break;

            foreach (var content in enumerator.Current.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent call:
                        yield return AnalystEvent.Status(AnalystTools.StatusFor(call.Name));
                        break;
                    case TextContent { Text.Length: > 0 } text:
                        yield return AnalystEvent.Delta(text.Text);
                        break;
                }
            }
        }

        yield return AnalystEvent.Done();
    }
}
