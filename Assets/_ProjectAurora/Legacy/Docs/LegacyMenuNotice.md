# Legacy Menu Notice

## Cena legada

`Assets/Scenes/MainMenu.unity`

## Script legado associado

`Assets/Scripts/MainMenuController.cs`

## Motivo

Essa cena pertence a uma fase anterior do projeto e ainda referencia o `MainMenuController` antigo por GUID. O fluxo oficial atual usa:

- `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`
- `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## Decisao

Nao apagar nem mover ainda, para evitar quebra de referencia Unity. A cena deve ser considerada legado e nao deve ser usada para novas features.

## Regra

Toda nova alteracao de menu deve ser feita no menu canonico:

`Assets/_ProjectAurora/Scenes/MainMenu.unity`
