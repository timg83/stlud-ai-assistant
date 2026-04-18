## Logische Architectuur

### Architectuurlagen

1. Presentatielaag

- Chatwidget op de schoolwebsite.

2. Applicatielaag

- Conversatie-API, orkestratie, promptbeleid, veiligheidsregels.

3. Kennislaag

- Retrievalservice op kennisindex met hybride search (vector + keyword).

4. Datalaag

- Bronbestanden, verrijkte documentsegmenten, metadata, auditlogs.

5. Beheerlaag

- Portaal voor contentbeheer, publicatie en kwaliteitsmonitoring.

### Logische Componenten

1. Widget Client

- Verstuurt vraag en toont antwoord, bronnen en escalatiebericht.

2. Chat API

- Authenticeert verzoeken, rate limiting, sessiebeheer, request-validatie.

3. Prompt Orchestrator

- Bouwt systeeminstructies op, injecteert relevante context, forceert antwoordformat.

4. Retrieval Engine

- Voert hybride zoekopdracht uit, reranking en confidence scoring.

5. Guardrail Engine

- Detecteert out-of-scope vragen, beleidsschendingen, lage zekerheid.

6. Content Ingestion Service

- Importeert PDF/Word/webpagina's, extraheert tekst, chunking, metadata-verrijking.

7. Content Governance Service

- Workflow voor concept -> validatie -> publicatie -> archief.

8. Analytics en Review Service

- Vangt feedback, onbeantwoorde vragen en kwaliteitsmetingen af.

### Hoofdflow Vraag-Antwoord

1. Gebruiker stelt vraag in widget.
2. Chat API valideert request en stuurt door naar Prompt Orchestrator.
3. Retrieval Engine haalt top-k contextsegmenten op uit gepubliceerde bronnen.
4. Guardrail Engine controleert dekking/veiligheid.
5. LLM genereert antwoord met verplichte bronsectie.
6. Chat API retourneert antwoord plus metadata (confidence, bronlinks, trace-id).
