## Functioneel Ontwerp (MVP)

### Doel en Resultaat

De assistent helpt ouders en leerlingen bij het vinden van betrouwbare schoolinformatie door vragen in natuurlijke taal te beantwoorden op basis van goedgekeurde bronnen. De assistent geeft geen juridisch, medisch of persoonsspecifiek advies en voert geen transacties uit.

### Gebruikersrollen

1. Ouder/leerling (eindgebruiker)

- Stelt vragen via chatwidget op de schoolwebsite.
- Ontvangt antwoord met bronverwijzing(en).

2. Schooladministratie (contentbeheer)

- Voegt documenten/pagina's toe of verwijdert ze.
- Publiceert, archiveert en herindexeert content.
- Beoordeelt onbeantwoorde vragen en importfouten.

3. Functioneel beheer/ICT

- Beheert toegang, monitoring en operationele instellingen.

### Functionele Use Cases

1. Vraag stellen en antwoord ontvangen

- Input: vrije tekst (NL/EN, later uitbreidbaar).
- Output: kort antwoord, bronverwijzing, onzekerheidsmelding bij lage zekerheid.

2. Doorverwijzen bij twijfel

- Bij onvoldoende dekking verwijst de assistent naar contactkanaal (mail/telefoon/websitepagina).

3. Content publiceren

- Beheerder uploadt PDF/Word documenten of selecteert één of meerdere websitepagina.
- Item krijgt statusconcept, gevalideerd, gepubliceerd of gearchiveerd.

4. Content herindexeren

- Beheerder start herindexering na wijziging van content of laat het systeem de content automatisch herindexeren wanneer dit nodig is.

5. Kwaliteitsreview

- Beheerder ziet onbeantwoorde vragen/topvragen en verbetert contentset.

### Functionele Regels

1. De assistent mag alleen antwoorden uit goedgekeurde en gepubliceerde bronnen.
2. Elk inhoudelijk antwoord bevat minstens een bronverwijzing, behalve bij standaardafwijzingen of veiligheidsmeldingen.
3. Bij lage relevantie of conflicterende bronnen toont de assistent onzekerheid en een doorverwijzing.
4. Taalrespons volgt de taal van de vraag (NL/EN), tenzij gebruiker expliciet om andere taal vraagt.
5. Gesprekken worden minimaal gelogd en geanonimiseerd waar mogelijk.
