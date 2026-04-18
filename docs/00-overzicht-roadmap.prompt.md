## Plan: School AI Assistent MVP - Overzicht en Roadmap

Bouw eerst een brongecontroleerde vraag-antwoord assistent als chatwidget op de bestaande schoolwebsite, gericht op ouders en leerlingen, met geselecteerde schoolinformatie als enige kennisbron. Mijn aanbeveling is een Azure-gebaseerde RAG-opzet: documenten en geselecteerde websitepagina's worden expliciet vrijgegeven door de schooladministratie, verwerkt tot een doorzoekbare kennisbasis, en de assistent antwoordt alleen met bronverwijzing, onzekerheidsmelding en menselijke doorverwijzing bij twijfel. Interne documenten achter login blijven in fase 1 standaard buiten scope totdat autorisatie- en publicatiebeleid expliciet zijn uitgewerkt.

## Steps

1. Definieer productscope en governance voor fase 1. Leg vast dat de assistent alleen informatieve vragen beantwoordt, bedoeld is voor ouders en leerlingen, en geen transacties of persoonsgebonden adviezen uitvoert. Bepaal ook inhoudelijke grenzen: schoolreglement, praktische schoolinformatie, veelgestelde vragen en geselecteerde beleidsdocumenten wel; medische, juridische of leerling-specifieke casussen niet.
2. Richt contentgovernance in. Ontwerp een eenvoudig beheermodel waarin de schooladministratie documenten en websitepagina's expliciet vrijgeeft, publiceert en intrekt. Neem metadata op zoals titel, bron, taal, status, versie, eigenaar en vervaldatum. Markeer interne documenten achter login voorlopig als uitgesloten tenzij de school ze expliciet omzet naar publiek deelbare kennis.
3. Kies de kernarchitectuur. Gebruik een RAG-architectuur op Microsoft/Azure: websitechatwidget -> backend/API -> orchestrationlaag met promptbeleid -> retrieval op kennisindex -> LLM voor antwoordgeneratie. Voeg guardrails toe zodat alleen antwoorden uit goedgekeurde bronnen komen, met verplicht broncitaat en fallback naar contactpersoon of pagina wanneer relevant bewijs ontbreekt.
4. Ontwerp content-inname voor bronnen. Voor fase 1: ingest van PDF, Word, publieke websitepagina's en losse FAQ-teksten. Gebruik een handmatige uploadstroom plus website-selectie, met periodieke herindexering voor zelden wijzigende content. Voor verspreide bronbestanden is een tijdelijke handmatige verzamelstap acceptabel in MVP, zolang elk item door een beheerder wordt goedgekeurd voor publicatie.
5. Ontwerp documentverwerking en kennisindex. Parse documenten, extraheer tekst, splits content in semantische segmenten, bewaar bronmetadata en taal, en indexeer voor hybride zoekopdrachten. Neem validatie op voor slechte scans, lege bestanden, dubbele documenten en verouderde versies. Zorg dat Nederlandse en Engelse content apart herkenbaar blijft zodat de assistent in de juiste taal kan antwoorden.
6. Definieer antwoordbeleid en UX. De assistent moet standaard in Nederlands en Engels kunnen antwoorden, met uitbreidbare taalondersteuning. Antwoorden bevatten: kort antwoord, verwijzing naar bron/document of pagina, expliciete onzekerheid bij lage relevantie, en doorverwijzing naar een medewerker of contactkanaal bij twijfel of ontbrekende informatie.
7. Ontwerp beheer- en reviewprocessen. Voorzie een klein beheerportaal of beheerscherm waarin schooladministratie documenten toevoegt, status ziet, content opnieuw indexeert en bronnen kan uitsluiten. Neem ook een reviewlijst op voor mislukte imports, bronnen zonder metadata en veelgestelde onbeantwoorde vragen.
8. Werk privacy, security en compliance uit. Verwerk alleen minimale gebruikersdata, definieer bewaartermijnen voor chatlogs, beperk toegang tot beheerschermen, en leg vast waar data staat en hoe leveranciers met gegevens omgaan. Omdat AVG/GDPR in scope is en Azure de voorkeursstack is, ligt een EU-hostingmodel voor de hand. Anonimiseer of minimaliseer gesprekslogging in fase 1 en leg een DPIA-achtige beoordeling vast voordat productie start.
9. Bouw de MVP in verifieerbare fasen. Fase A: contentmodel, ingest-pipeline en index. Fase B: backend met retrieval en antwoordbeleid. Fase C: chatwidget op website. Fase D: beheerfuncties en monitoring. Fase E: pilot met beperkte bronset en evaluatie op antwoordkwaliteit.
10. Plan acceptatie en uitrol. Start met een beperkte set van 20-40 representatieve documenten/pagina's uit de verwachte middelgrote bronset, meet kwaliteit op echte ouder-/leerlingvragen, en breid daarna pas uit naar de volledige 50-300 bronnen. Gebruik de pilot om topvragen, ontbrekende content en risico's op hallucinerende antwoorden te identificeren.

## Verification

1. Bevestig productkaders schriftelijk: doelgroep, talen, uitgesloten vraagtypen, en of interne documenten ooit publiek inzetbaar mogen worden.
2. Maak een representatieve contentinventaris van minimaal 20 documenten/pagina's en controleer per bron: eigenaar, actualiteit, taal, publiceerbaarheid en kwaliteit van extractie.
3. Valideer een prototype op echte vragen van ouders en leerlingen en meet: brondekking, juistheid, onzekerheidsafhandeling, taalkeuze en doorverwijzingen.
4. Voer een privacy- en securityreview uit op logging, toegangsbeheer, leverancierskeuze, datalocatie en bewaartermijnen voordat productie wordt vrijgegeven.
5. Doe een pilot op de website met beperkte zichtbaarheid en review wekelijkse logs op onbeantwoorde vragen, foutieve verwijzingen en ontbrekende bronnen.

## Decisions

- In scope voor fase 1: vraag-antwoord assistent voor ouders en leerlingen, via chatwidget op bestaande schoolwebsite.
- In scope voor bronnen: PDF, Word, publieke websitepagina's, losse FAQ-teksten; bronselectie via combinatie van uploaden en website-selectie.
- In scope voor kwaliteit: bronverwijzing, expliciete onzekerheid en menselijke doorverwijzing bij twijfel.
- Richting technologie: Microsoft/Azure stack, met RAG als aanbevolen aanpak.
- Talen: Nederlands en Engels direct ondersteunen; andere talen alleen als uitbreiding na MVP.
- Bronomvang: verwacht middelgroot, circa 50-300 documenten/pagina's, met relatief lage wijzigingsfrequentie.
- Buiten scope voor fase 1: transacties, leerlingspecifieke dossiers, automatische besluitvorming en gebruik van interne documenten zonder expliciet informatiebeleid.

## Further Considerations

1. Interne documenten achter login: aanbevolen keuze is fase 1 beperken tot publieke of expliciet vrijgegeven informatie. Pas daarna eventueel uitbreiden met autorisatie-gestuurde toegang.
2. Beheerinterface: aanbevolen keuze is starten met een klein intern beheerscherm voor administratie in plaats van directe koppelingen met alle bronsystemen. Dat verlaagt complexiteit en versnelt de pilot.
3. Uitrolstrategie: aanbevolen keuze is eerst een MVP met handmatige contentcuratie en periodieke herindexering; pas daarna automatiseren met SharePoint/Microsoft 365 koppelingen.
