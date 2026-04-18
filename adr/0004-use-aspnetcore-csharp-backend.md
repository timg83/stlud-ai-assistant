# ADR 0004: Use ASP.NET Core and C# for Backend

## Status

Accepted

## Context

Voor de backendstack is expliciet gekozen voor ASP.NET Core met C#.

## Decision

We bouwen de API en orkestratielaag in ASP.NET Core op .NET 10.

## Consequences

- Goede aansluiting op Azure-hosting en enterprise-beheer.
- Lokale buildvalidatie vereist een .NET 10 SDK in de omgeving.
