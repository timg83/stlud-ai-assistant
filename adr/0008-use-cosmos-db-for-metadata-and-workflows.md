# ADR 0008: Use Cosmos DB for Metadata and Workflows

## Status

Accepted

## Context

Voor metadata-opslag is gekozen voor Cosmos DB.

## Decision

We slaan workflowstatus, review-items, chattraces en contentmetadata op in Azure Cosmos DB.

## Consequences

- Flexibel documentmodel voor evoluerende metadata.
- Partitionering en RU-kosten moeten zorgvuldig worden ontworpen.
