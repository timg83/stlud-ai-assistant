import { FormEvent, useEffect, useRef, useState } from "react";
import Markdown from "react-markdown";

export const MAX_QUESTION_LENGTH = 1000;
const CONTROL_CHARACTERS_PATTERN = /[\u0000-\u001F\u007F]/g;
const DEFAULT_ERROR_MESSAGE = "De chatservice gaf geen geldig antwoord.";

type ChatSource = {
  sourceId: string;
  title: string;
  locator?: string | null;
  url?: string | null;
};

type ChatEscalation = {
  message: string;
  contactLabel?: string | null;
  contactUrl?: string | null;
};

type Message = {
  role: "user" | "assistant";
  text: string;
  sources?: ChatSource[];
  escalation?: ChatEscalation | null;
};

type ChatWidgetProps = {
  apiBaseUrl: string;
};

export function ChatWidget({ apiBaseUrl }: ChatWidgetProps) {
  const [question, setQuestion] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const threadRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (threadRef.current) {
      threadRef.current.scrollTop = threadRef.current.scrollHeight;
    }
  }, [messages]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedQuestion = question
      .replace(CONTROL_CHARACTERS_PATTERN, " ")
      .trim();

    if (!normalizedQuestion) {
      return;
    }

    if (normalizedQuestion.length > MAX_QUESTION_LENGTH) {
      setError(`Je vraag mag maximaal ${MAX_QUESTION_LENGTH} tekens bevatten.`);
      return;
    }

    const currentQuestion = normalizedQuestion;
    setError(null);
    setIsSubmitting(true);
    setMessages((prev) => [...prev, { role: "user", text: currentQuestion }]);
    setQuestion("");

    // Add placeholder assistant message for streaming
    const assistantIndex = messages.length + 1;
    setMessages((prev) => [...prev, { role: "assistant", text: "" }]);

    try {
      const response = await fetch(`${apiBaseUrl}/api/chat/stream`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          question: currentQuestion,
          locale: navigator.language || "nl-NL",
        }),
      });

      if (!response.ok || !response.body) {
        throw new Error(DEFAULT_ERROR_MESSAGE);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        buffer = lines.pop() || "";

        for (const line of lines) {
          if (!line.startsWith("data: ")) continue;
          const jsonStr = line.slice(6).trim();
          if (!jsonStr) continue;

          let evt;
          try {
            evt = JSON.parse(jsonStr);
          } catch {
            throw new Error(DEFAULT_ERROR_MESSAGE);
          }

          if (evt.type === "delta" && evt.delta) {
            setMessages((prev) => {
              const updated = [...prev];
              const msg = updated[assistantIndex];
              if (msg) {
                updated[assistantIndex] = {
                  ...msg,
                  text: msg.text + evt.delta,
                };
              }
              return updated;
            });
          } else if (evt.type === "done") {
            setMessages((prev) => {
              const updated = [...prev];
              const msg = updated[assistantIndex];
              if (msg) {
                updated[assistantIndex] = {
                  ...msg,
                  sources: evt.sources || [],
                  escalation: evt.escalation || null,
                };
              }
              return updated;
            });
          }
        }
      }
    } catch (submissionError) {
      setMessages((prev) => {
        const updated = [...prev];
        const msg = updated[assistantIndex];
        if (msg && !msg.text) {
          return updated.filter((_, i) => i !== assistantIndex);
        }
        return updated;
      });
      setError(
        submissionError instanceof Error
          ? submissionError.message
          : "Onbekende fout.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="widget-shell" aria-label="School AI Assistent">
      <header className="widget-header">
        <p className="widget-eyebrow">School AI Assistent</p>
        <h1>Stel een vraag</h1>
        <p className="widget-copy">
          De assistent antwoordt alleen op basis van goedgekeurde
          schoolinformatie.
        </p>
      </header>

      <div className="chat-thread" ref={threadRef}>
        {messages.length === 0 ? (
          <p className="empty-state">
            Vraag bijvoorbeeld naar schooltijden, verlof of het schoolreglement.
          </p>
        ) : null}
        {messages.map((message, index) => (
          <article
            key={`${message.role}-${index}`}
            className={`message message-${message.role}`}
          >
            {message.role === "assistant" && !message.text && isSubmitting ? (
              <div
                className="typing-indicator"
                aria-label="Antwoord wordt geladen"
              >
                <span aria-hidden="true" role="presentation"></span>
                <span aria-hidden="true" role="presentation"></span>
                <span aria-hidden="true" role="presentation"></span>
              </div>
            ) : message.role === "assistant" ? (
              <div className="markdown-body">
                <Markdown>{message.text}</Markdown>
              </div>
            ) : (
              <p>{message.text}</p>
            )}
            {message.sources && message.sources.length > 0 ? (
              <ul className="source-list">
                {message.sources.map((source) => (
                  <li key={source.sourceId}>
                    <strong>{source.title}</strong>
                    {source.locator ? <span> · {source.locator}</span> : null}
                  </li>
                ))}
              </ul>
            ) : null}
            {message.escalation ? (
              <p className="escalation">
                {message.escalation.message}
                {message.escalation.contactLabel &&
                message.escalation.contactUrl ? (
                  <>
                    {" "}
                    <a
                      href={message.escalation.contactUrl}
                      rel="noreferrer"
                      target="_blank"
                    >
                      {message.escalation.contactLabel}
                    </a>
                  </>
                ) : null}
              </p>
            ) : null}
          </article>
        ))}
      </div>

      <form className="chat-form" onSubmit={handleSubmit}>
        <label className="sr-only" htmlFor="question">
          Vraag
        </label>
        <textarea
          id="question"
          name="question"
          rows={4}
          value={question}
          onChange={(event) => setQuestion(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              event.currentTarget.form?.requestSubmit();
            }
          }}
          placeholder="Waar vind ik informatie over schooltijden of verlof?"
          disabled={isSubmitting}
          maxLength={MAX_QUESTION_LENGTH}
        />
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Bezig..." : "Verstuur vraag"}
        </button>
      </form>

      {error ? <p className="error-state">{error}</p> : null}
    </section>
  );
}
