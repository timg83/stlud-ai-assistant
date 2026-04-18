## Security en Privacy by Design

1. Toegang

- RBAC voor beheerportaal, least privilege voor services.

2. Gegevensbescherming

- Encryptie in transit en at rest, secrets in Key Vault.

3. Logging

- Pseudonimisering van chatinhoud waar mogelijk, korte bewaartermijnen.

4. Data residency

- EU-regio voor productie.

5. Abuse protection

- Rate limiting, input length caps, basic prompt injection controles.

## Niet-Functionele Eisen (MVP)

1. Beschikbaarheid

- Doel: >= 99.5% tijdens schoolcommunicatie-uren.

2. Performance

- Doel: p95 responstijd <= 4 seconden voor standaardvragen.

3. Kwaliteit

- Doel: >= 85% antwoorden met correcte bronverwijzing in pilotset.

4. Beheerbaarheid

- Herindex jobstatus binnen beheerportaal zichtbaar.

5. Schaalbaarheid

- Ontwerp op groei van 50-300 naar >1000 bronnen zonder herbouw.
