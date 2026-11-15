# Kopiérbare Claude Code-prompts

## Første besked

```text
Jeg vil bygge hele Vagttilmelding PWA-systemet. Læs README_START_HER.md, CLAUDE.md, KRAV_OG_ACCEPTKRITERIER.md, TEKNISK_DESIGN.md, DESIGN_GUIDE.md samt original-PDF'en. Læs derefter STEP_00_PROJEKTSTART.md, og udfør KUN dette trin. Byg konkret, test hvad der kan testes, dokumentér status, og STOP. Vent på mit GO før næste trin. Husk at Claude Code-instruktioner ikke må med i Git.
```

## Næste step (ændr nummer/navn)

```text
GO til STEP_01_LOSNING_OG_OPSTART.md. Læs CLAUDE.md, den aktuelle STEP-fil og projektets eksisterende status. Implementér dette trin fuldt ud, kør relevante tests, ret fejl, opdatér dokumentation og rapportér ændringer, testresultater og eventuelle risici. STOP før næste trin. Claude-instruktioner må ikke committes.
```

## Revisionsprompt ved manglende tests

```text
Jeg godkender ikke dette step endnu. Gennemgå acceptkriterierne i step-filen, gennemfør manglende implementering og kør de relevante automatiske tests. Vis den konkrete PASS/FAIL-status og opdatér docs/STEP_STATUS.md. Gå ikke til næste step.
```

## Vigtig afslutning

Når STEP_20 er gennemført, kræv både en **kravmatrix** og en **reelt afprøvet installation** før ordet "færdig" bruges.
