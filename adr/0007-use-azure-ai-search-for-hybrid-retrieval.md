# ADR 0007: Use Azure AI Search for Hybrid Retrieval

## Status

Accepted

## Context

Het ontwerp vereist hybride retrieval met keyword en vector search.

## Decision

We gebruiken Azure AI Search als centrale index- en retrievalservice.

## Consequences

- Ondersteunt hybride zoekscenario's en filtering.
- Indexmodellering en chunkstrategie worden kritieke ontwerppunten.
