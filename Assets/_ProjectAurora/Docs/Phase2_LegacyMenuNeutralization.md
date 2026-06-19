# Phase 2 - Legacy Menu Neutralization

## 1. Build Settings verificado

Build Settings atual:

```text
0 - Assets/_ProjectAurora/Scenes/MainMenu.unity
1 - Assets/_ProjectAurora/Scenes/Beta03_Principal.unity
```

O fluxo canonico `MainMenu -> Beta03_Principal` permanece intacto.

## 2. Menu legado no Build Settings

`Assets/Scenes/MainMenu.unity` nao esta no Build Settings.

Nenhuma alteracao foi feita em `ProjectSettings/EditorBuildSettings.asset`.

## 3. Marcado como legado

Cena legada:

- `Assets/Scenes/MainMenu.unity`

Script legado associado:

- `Assets/Scripts/MainMenuController.cs`

Documento de aviso criado:

- `Assets/_ProjectAurora/Legacy/Docs/LegacyMenuNotice.md`

O aviso foi feito por documentacao, sem alterar a cena legada.

## 4. Arquivos preservados

Preservados sem alteracao:

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`
- `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`
- `ProjectSettings/EditorBuildSettings.asset`

## 5. Arquivos que nao devem ser usados em novas features

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`

Novas features de menu devem usar:

- `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

## 6. Proxima acao recomendada

Proxima etapa segura:

1. Validar manualmente no Unity o fluxo `Assets/_ProjectAurora/Scenes/MainMenu.unity -> Jogar -> Beta03_Principal`.
2. Em uma fase futura, revisar se `Assets/Scenes/MainMenu.unity` ainda precisa existir.
3. Antes de qualquer remocao, mapear referencias por GUID em cenas, prefabs e builders.
4. Somente depois arquivar ou remover legado com uma etapa dedicada.
