# ADR 0006: Use Azure OpenAI for Generation and Embeddings

## Status

Superseded by [ADR 0017](0017-use-azure-ai-foundry-for-ai-platform.md)

## Context

De assistent heeft generatieve antwoorden en embeddings voor retrieval nodig.

## Decision

We gebruiken Azure OpenAI voor zowel antwoordgeneratie als embeddinggeneratie.

## Consequences

- Eén AI-platform voor modelbeheer.
- Kosten en capaciteitsbeheer moeten expliciet worden bewaakt.
