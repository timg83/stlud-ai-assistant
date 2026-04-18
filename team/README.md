# Team Setup

Deze map definieert het agentteam in werkbare vorm voor deze workspace.

## Rollen

- Architect
- Teamlead
- Developer
- Reviewer
- Tester
- Security specialist

## Gebruik

- `team/prompts/` bevat per rol een werkprompt.
- `team/checklists/` bevat per rol een review- of uitvoeringschecklist.
- De teamlead bewaakt scope, sequencing en overdrachten tussen rollen.

## Werkwijze

1. Architect verfijnt ontwerp en ADR-impact.
2. Teamlead breekt werk op in uitvoerbare stories en bewaakt afhankelijkheden.
3. Developer implementeert.
4. Reviewer beoordeelt kwaliteit en regressierisico.
5. Tester valideert functioneel gedrag en testdekking.
6. Security specialist beoordeelt dreigingen, secrets en misbruikscenario's.
