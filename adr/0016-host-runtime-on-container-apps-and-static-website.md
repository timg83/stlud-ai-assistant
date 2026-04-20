# ADR 0016: Host Runtime on Container Apps and Static Website

## Status

Accepted

## Context

De oorspronkelijke runtimekeuze was Azure App Service voor de backend. In de gekozen subscription bleek Microsoft.Web quota voor de relevante App Service SKU's in de doellocatie niet beschikbaar, waardoor deployment blokkeerde.

## Decision

We hosten de backend op Azure Container Apps en de frontend widget als static website op Azure Storage.

## Consequences

- De backend wordt als containerimage gebouwd en uitgerold via Azure Container Registry.
- De frontend wordt statisch gebouwd en gepubliceerd naar het storage-account.
- De infrastructuur blijft volledig via Terraform provisionbaar, maar runtime-deployment vraagt ook build- en publish-stappen.
