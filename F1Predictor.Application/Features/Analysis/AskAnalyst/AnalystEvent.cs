namespace F1Predictor.Application.Features.Analysis.AskAnalyst;

/// <param name="Type">"status" (a tool is being called), "delta" (text to append), "done", or "error".</param>
public sealed record AnalystEvent(string Type, string? Text)
{
    public static AnalystEvent Status(string text) => new("status", text);
    public static AnalystEvent Delta(string text) => new("delta", text);
    public static AnalystEvent Done() => new("done", null);
    public static AnalystEvent Error(string text) => new("error", text);
}
