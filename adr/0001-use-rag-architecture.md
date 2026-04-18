# ADR 0001: Use RAG Architecture

## Status

Accepted

## Context

De assistent mag alleen antwoorden geven op basis van gecontroleerde schoolinformatie uit documenten en websitepagina's.

## Decision

We gebruiken een Retrieval-Augmented Generation architectuur waarin retrieval uit goedgekeurde bronnen plaatsvindt voordat antwoordgeneratie wordt uitgevoerd.

## Consequences

- Antwoorden blijven herleidbaar naar bronmateriaal.
- De kennisbasis kan onafhankelijk van het model worden beheerd.
- Er is extra complexiteit in ingest, indexing en retrieval.
