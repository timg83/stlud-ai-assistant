# ADR 0010: Host Backend on Azure App Service

## Status

Superseded by [ADR 0016](0016-host-runtime-on-container-apps-and-static-website.md)

## Context

Voor de runtime is gekozen voor Azure App Service.

## Decision

We hosten de backend-API op Azure App Service.

## Consequences

- Eenvoudige deployment voor web-API workloads.
- Container-specifieke flexibiliteit is lager dan bij Container Apps.
