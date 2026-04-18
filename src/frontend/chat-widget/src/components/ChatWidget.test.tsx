import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ChatWidget, MAX_QUESTION_LENGTH } from "./ChatWidget";

describe("ChatWidget", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    cleanup();
  });

  it("verstuurt een vraag en toont antwoord met bron", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(
        JSON.stringify({
          answerText: "De lessen starten om 08:30.",
          confidence: "high",
          sources: [
            { sourceId: "1", title: "Schoolgids", locator: "p. 4", url: null },
          ],
          escalation: null,
          traceId: "trace-1",
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        },
      ),
    );

    render(<ChatWidget apiBaseUrl="http://localhost:5000" />);

    await user.type(
      screen.getByLabelText("Vraag"),
      "Hoe laat beginnen de lessen?",
    );
    await user.click(screen.getByRole("button", { name: "Verstuur vraag" }));

    await screen.findByText("De lessen starten om 08:30.");
    expect(screen.getByText("Schoolgids")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("toont foutmelding bij mislukte API-call", async () => {
    const user = userEvent.setup();
    vi.spyOn(globalThis, "fetch").mockRejectedValue(new Error("Netwerkfout"));

    render(<ChatWidget apiBaseUrl="http://localhost:5000" />);

    await user.type(
      screen.getByLabelText("Vraag"),
      "Wat zijn de schooltijden?",
    );
    await user.click(screen.getByRole("button", { name: "Verstuur vraag" }));

    await screen.findByText("Netwerkfout");
  });

  it("verstuurt geen lege vraag", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.spyOn(globalThis, "fetch");

    render(<ChatWidget apiBaseUrl="http://localhost:5000" />);

    await user.click(screen.getByRole("button", { name: "Verstuur vraag" }));

    await waitFor(() => {
      expect(fetchMock).not.toHaveBeenCalled();
    });
  });

  it("blokkeert vragen boven de maximale lengte", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.spyOn(globalThis, "fetch");

    render(<ChatWidget apiBaseUrl="http://localhost:5000" />);

    fireEvent.change(screen.getByLabelText("Vraag"), {
      target: { value: "a".repeat(MAX_QUESTION_LENGTH + 1) },
    });
    await user.click(screen.getByRole("button", { name: "Verstuur vraag" }));

    await screen.findByText(
      `Je vraag mag maximaal ${MAX_QUESTION_LENGTH} tekens bevatten.`,
    );
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
