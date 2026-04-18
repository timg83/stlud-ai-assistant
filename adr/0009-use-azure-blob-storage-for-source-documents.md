# ADR 0009: Use Azure Blob Storage for Source Documents

## Status

Accepted

## Context

Bronbestanden en ingest artifacts moeten duurzaam en schaalbaar worden opgeslagen.

## Decision

We gebruiken Azure Blob Storage voor originele documenten, exports en ingest artifacts.

## Consequences

- Duidelijke scheiding tussen binary storage en metadata.
- Lifecycle-beheer en retentie moeten expliciet worden ingericht.
