## Technisch Ontwerp

### Datamodel (Conceptueel)

1. SourceDocument

- source_id, titel, type (pdf/docx/web), eigenaar, taal, versie, status, published_at.

2. SourceChunk

- chunk_id, source_id, chunk_text, chunk_order, embedding_vector_ref, access_scope.

3. ChatSession

- session_id, locale, created_at, user_type (anoniem/ingelogd).

4. ChatMessage

- message_id, session_id, role, text, timestamp.

5. AnswerTrace

- trace_id, question_message_id, confidence, source_ids, policy_flags.

6. ReviewItem

- review_id, category (unanswered/import_error/feedback), status, assignee.

### Indexstrategie

1. Chunkgrootte

- Start met 500-900 tokens, overlap 10-15%.

2. Retrieval

- Hybride query: keyword plus vector, top-k 8-12, rerank naar top-3 tot top-5.

3. Filters

- Alleen status gepubliceerd en juiste taal/scope.

4. Versioning

- Nieuwe documentversies krijgen nieuwe indexentries; oude versies uit actieve searchfilter.

### API Ontwerp (Eerste versie)

1. POST /api/chat/query

- Input: question, locale, session_id(optional).
- Output: answer_text, sources[], confidence, escalation(optional), trace_id.

2. POST /api/content/upload

- Input: bestand plus metadata.
- Output: source_id, ingest_status.

3. POST /api/content/publish/{source_id}

- Output: publish_status, indexed_at.

4. POST /api/content/reindex

- Input: source_id of batchfilter.
- Output: job_id.

5. GET /api/review/items

- Output: lijst reviewitems met prioriteit.

### Antwoordcontract (LLM Output Schema)

1. answer

- Korte, feitelijke beantwoording.

2. sources

- Lijst met bronnaam, sectie/pagina en link waar mogelijk.

3. confidence

- low/medium/high.

4. fallback

- Boolean plus contactadvies bij low confidence.
