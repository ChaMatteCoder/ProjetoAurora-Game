# Inventario de Legado

## Pastas consideradas legado

- `Assets/Scripts`
- `Assets/Scenes`

Essas pastas ainda podem conter scripts e cenas funcionais usados pelo beta. Elas nao devem ser movidas, apagadas ou renomeadas ate que todas as referencias em cenas e prefabs sejam mapeadas.

## Scripts de menu duplicados

- `Assets/Scripts/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

## Controller canonico

- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

## Observacao

- Nao remover scripts legados ainda, pois `Beta03_Principal` pode depender de referencias por GUID.
- Antes de qualquer remocao, e necessario mapear referencias em cenas e prefabs.

## Proximas limpezas futuras

- Consolidar controladores de menu.
- Separar `TutorialManager`.
- Reduzir `GameManager`.
- Revisar `GameOverManager`.
- Quebrar builders grandes.
- Mover outputs derivados para pasta ignorada.
