"use client";

import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { useAnalysisStore, type ChatMessageState } from "@/features/analysis/analysis-store";
import { Button } from "@/shared/components/Button";
import { Card } from "@/shared/components/Card";
import { ErrorBanner } from "@/shared/components/ErrorBanner";
import { TableSkeleton } from "@/shared/components/Skeleton";
import { StatusAnnouncer } from "@/shared/components/StatusAnnouncer";
import { useAiStatus } from "@/shared/lib/use-ai-status";

interface AnalystChatProps {
  year: number;
}

/** Matches the backend validator's per-turn limit. */
const MAX_QUESTION_LENGTH = 2000;

const SUGGESTED_PROMPTS = [
  "Who is most likely on the podium and why?",
  "What does the leader need to clinch the title?",
  "How did the model do at the last race?",
] as const;

/** No typography plugin here, so the markdown gets a small set of spacing rules on its wrapper. */
const MARKDOWN_CLASSES =
  "text-sm leading-relaxed [&_p]:my-2 [&_ul]:my-2 [&_ul]:list-disc [&_ul]:pl-5 [&_ol]:my-2 [&_ol]:list-decimal [&_ol]:pl-5 [&_li]:my-0.5 [&_strong]:font-semibold [&_h1]:mt-4 [&_h1]:text-base [&_h1]:font-semibold [&_h2]:mt-4 [&_h2]:text-base [&_h2]:font-semibold [&_h3]:mt-3 [&_h3]:text-sm [&_h3]:font-semibold [&_a]:underline [&_code]:font-mono [&_code]:text-xs [&_table]:my-2 [&_table]:text-xs [&_th]:pr-3 [&_th]:text-left [&_td]:pr-3 [&>*:first-child]:mt-0 [&>*:last-child]:mb-0";

function announcementFor(messages: ChatMessageState[]): string | null {
  const last = messages[messages.length - 1];
  if (!last || last.role !== "assistant" || last.streaming) return null;
  if (last.error) return `The analyst could not answer: ${last.error}`;
  if (last.content) return "Answer complete";

  return null;
}

function MessageBubble({ message }: { message: ChatMessageState }) {
  if (message.role === "user") {
    return (
      <div className="flex justify-end">
        <p className="max-w-[85%] whitespace-pre-wrap rounded-(--radius) bg-(--color-accent) px-3 py-2 text-sm text-(--color-on-accent) sm:max-w-[75%]">
          {message.content}
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-start">
      <div className="max-w-full rounded-(--radius) border border-(--color-border) bg-(--color-surface) px-3 py-2 sm:max-w-[85%]">
        {message.content ? (
          <div className={MARKDOWN_CLASSES}>
            <ReactMarkdown remarkPlugins={[remarkGfm]}>{message.content}</ReactMarkdown>
            {message.streaming && (
              <span aria-hidden="true" className="ml-0.5 inline-block animate-pulse text-(--color-accent)">
                ▍
              </span>
            )}
          </div>
        ) : (
          message.streaming && (
            <span aria-hidden="true" className="inline-block animate-pulse text-(--color-accent)">
              ▍
            </span>
          )
        )}
      </div>
      {message.status && (
        <p className="mt-1 px-1 font-mono text-xs italic text-(--color-muted)">{message.status}</p>
      )}
      {message.error && <p className="mt-1 px-1 text-xs text-(--color-error-text)">{message.error}</p>}
    </div>
  );
}

export function AnalystChat({ year }: AnalystChatProps) {
  const { status: aiStatus, loaded, chatAvailable } = useAiStatus();
  const messages = useAnalysisStore((state) => state.messages);
  const streaming = useAnalysisStore((state) => state.streaming);
  const send = useAnalysisStore((state) => state.send);
  const abort = useAnalysisStore((state) => state.abort);
  const clear = useAnalysisStore((state) => state.clear);

  const [draft, setDraft] = useState("");
  const listRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Keep the newest text in view as it streams in; the list is its own scroll container so
  // this never yanks the page.
  useEffect(() => {
    const list = listRef.current;
    if (list) list.scrollTop = list.scrollHeight;
  }, [messages]);

  if (!loaded) {
    return (
      <Card title="Analyst">
        <TableSkeleton rows={3} />
      </Card>
    );
  }

  if (!chatAvailable) {
    return <ErrorBanner title="AI analyst not enabled" message="No AI provider is configured on this deployment." />;
  }

  const canSend = draft.trim().length > 0 && !streaming;

  const submit = (text: string) => {
    if (streaming || !text.trim()) return;
    setDraft("");
    void send(year, text);
    textareaRef.current?.focus();
  };

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    submit(draft);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault();
      submit(draft);
    }
  };

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-semibold">Analyst</h1>
        <p className="text-sm text-(--color-muted)">
          Ask about the {year} season. Every number the analyst quotes is looked up from the prediction
          models, the standings and the title odds — it does not guess.
          {aiStatus?.model && (
            <>
              {" "}
              <span className="font-mono text-xs">
                {aiStatus.provider} · {aiStatus.model}
              </span>
            </>
          )}
        </p>
      </div>

      <Card>
        <div
          ref={listRef}
          className="max-h-[55vh] min-h-48 space-y-3 overflow-y-auto pr-1"
          role="log"
          aria-label="Conversation with the analyst"
          aria-busy={streaming}
        >
          {messages.length === 0 ? (
            <div className="flex h-full min-h-48 flex-col items-start justify-center gap-2">
              <p className="text-sm text-(--color-muted)">Try one of these to start:</p>
              <div className="flex flex-wrap gap-2">
                {SUGGESTED_PROMPTS.map((prompt) => (
                  <button
                    key={prompt}
                    type="button"
                    onClick={() => submit(prompt)}
                    disabled={streaming}
                    className="rounded-(--radius-pill) border border-(--color-border) px-3 py-1.5 text-left text-sm transition-colors hover:bg-(--color-surface-hover) focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent) disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {prompt}
                  </button>
                ))}
              </div>
            </div>
          ) : (
            messages.map((message) => <MessageBubble key={message.id} message={message} />)
          )}
        </div>

        <form onSubmit={onSubmit} className="mt-4 border-t border-(--color-border) pt-4">
          <label htmlFor="analyst-question" className="sr-only">
            Your question
          </label>
          <textarea
            id="analyst-question"
            ref={textareaRef}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={onKeyDown}
            maxLength={MAX_QUESTION_LENGTH}
            rows={3}
            placeholder="Ask about the next race, the standings or the title odds…"
            aria-describedby="analyst-question-hint"
            className="w-full resize-y rounded-(--radius) border border-(--color-border) bg-(--color-background) px-3 py-2 text-sm placeholder:text-(--color-muted) focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--color-accent)"
          />
          <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
            <p id="analyst-question-hint" className="font-mono text-xs text-(--color-muted)">
              Enter to send · Shift+Enter for a new line · {draft.length}/{MAX_QUESTION_LENGTH}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="secondary"
                onClick={clear}
                disabled={messages.length === 0}
              >
                Clear
              </Button>
              {streaming && (
                <Button type="button" variant="secondary" onClick={abort}>
                  Stop
                </Button>
              )}
              <Button type="submit" disabled={!canSend} aria-busy={streaming}>
                {streaming ? "Answering…" : "Send"}
              </Button>
            </div>
          </div>
        </form>

        <StatusAnnouncer message={announcementFor(messages)} />
      </Card>
    </div>
  );
}
