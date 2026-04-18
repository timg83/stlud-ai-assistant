import { FormEvent, useState } from "react";

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

type ChatResponse = {
  answerText: string;
  confidence: string;
  sources: ChatSource[];
  escalation?: ChatEscalation | null;
  traceId: string;
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

    try {
      const response = await fetch(`${apiBaseUrl}/api/chat/query`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          question: currentQuestion,
          locale: navigator.language || "nl-NL",
        }),
      });

      if (!response.ok) {
        throw new Error(DEFAULT_ERROR_MESSAGE);
      }

      const data = (await response.json()) as ChatResponse;
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          text: data.answerText,
          sources: data.sources,
          escalation: data.escalation,
        },
      ]);
    } catch (submissionError) {
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

      <div className="chat-thread">
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
            <p>{message.text}</p>
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
          placeholder="Waar vind ik informatie over schooltijden of verlof?"
          disabled={isSubmitting}
          maxLength={MAX_QUESTION_LENGTH}
        />
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Versturen..." : "Verstuur vraag"}
        </button>
      </form>

      {error ? <p className="error-state">{error}</p> : null}
    </section>
  );
}
