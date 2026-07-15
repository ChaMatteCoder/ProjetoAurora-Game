# Lore System - Arquitetura de Dados

## Assets oficiais

- Textos: `Assets/_ProjectAurora/Data/Lore/Text/LORE_001.txt` a `LORE_024.txt`.
- Definições: `Assets/_ProjectAurora/Data/Lore/Definitions/LORE_XXX.asset`.
- Catálogo: `Assets/_ProjectAurora/Data/Lore/AuroraLoreCatalog.asset`.
- Builder: `Assets/_ProjectAurora/Scripts/Editor/Lore/AuroraLoreCatalogBuilder.cs`.

O runtime lê os textos por `TextAsset`; não usa `File.ReadAllText`, `AssetDatabase` ou varredura de disco.

## Tipos

`AuroraLoreDefinition` concentra ID, título, categoria, descrição curta, `TextAsset`, tipo de desbloqueio, preço, flags, ordem e IDs futuros de missão/coletável.

`AuroraLoreCatalog` mantém a ordem oficial, resolve por ID e valida referências, duplicidade, sequência, preços e regras de default/secreto.

`AuroraLoreService` é a autoridade de estado. Ele consulta o catálogo, sincroniza defaults, compra, recebe coleta, valida missão secreta, conta progresso e emite eventos.

## Categorias oficiais

| Tipo | IDs | Preço |
|---|---|---:|
| Default | LORE_008, LORE_009 | 0 |
| GameplayCollectible | 001, 003, 005, 006, 011, 013, 014, 017, 018, 019, 022, 023 | 0 |
| AuroraCoinPurchase | 002, 004 | 10 |
| AuroraCoinPurchase | 007, 010, 012 | 15 |
| AuroraCoinPurchase | 015, 016, 021 | 20 |
| SecretMission | LORE_020, LORE_024 | 0 |

Os preços são provisórios e ficam nas definições do catálogo; podem ser ajustados sem recompilar controllers.

## Save

- Campo reutilizado: `AuroraProgressSaveData.unlockedDataFiles`.
- IDs oficiais usam o prefixo `LORE_`.
- Ao inicializar, IDs `LORE_` inválidos são removidos e `LORE_008/009` são inseridos.
- IDs legados ou de outros sistemas fora do prefixo `LORE_` são preservados.
- O texto completo nunca é serializado no save.

## Como reconstruir

Use `Tools/Projeto Aurora/Lore/Rebuild Lore Catalog`. O builder extrai o título da primeira heading, gera descrição curta a partir do resumo, aplica a tabela oficial e valida UTF-8.

## Como adicionar uma Lore futura

1. Adicione um `.txt` UTF-8 à pasta oficial com ID único.
2. Expanda `OfficialLoreCount` e a tabela de categoria no builder.
3. Reconstrua o catálogo.
4. Corrija qualquer issue mostrada por `Validate Lore Files`.
5. Adicione teste de categoria, preço e regra de desbloqueio.
