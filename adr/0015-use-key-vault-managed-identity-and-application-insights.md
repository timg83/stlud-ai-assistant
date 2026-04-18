# ADR 0015: Use Key Vault, Managed Identity and Application Insights

## Status

Accepted

## Context

Het ontwerp noemt secretsmanagement, managed identities en monitoring als kernonderdelen.

## Decision

We gebruiken Azure Key Vault voor secrets, Managed Identity voor service-to-service toegang en Application Insights voor observability.

## Consequences

- Minder geheimen in code of configuratie.
- Extra Azure-configuratie nodig voor identities en telemetry.
