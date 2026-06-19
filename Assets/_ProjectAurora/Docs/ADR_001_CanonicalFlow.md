# ADR 001 - Fluxo Canonico do Projeto Aurora

## Status

Aceito.

## Contexto

O projeto cresceu com multiplas cenas, scripts duplicados e versoes experimentais. Para estabilizar o beta, foi definido um fluxo oficial.

## Decisao

- Cena de menu oficial: `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- Controller de menu oficial: `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`
- Cena de gameplay oficial: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
- Fluxo oficial: `MainMenu -> Beta03_Principal`
- Raiz canonica: `Assets/_ProjectAurora/`

## Consequencias

- Novas features devem ser implementadas sobre `Beta03_Principal`.
- Novos scripts devem ficar dentro de `Assets/_ProjectAurora/Scripts`.
- Assets novos devem ficar dentro de `Assets/_ProjectAurora`.
- `Assets/Scripts` e `Assets/Scenes` sao considerados legado, mas ainda nao podem ser removidos.
- Nenhuma nova cena `Beta04`/`Beta05` deve ser criada sem justificativa explicita.
- Builders de editor nao devem gerar novas cenas principais sem aprovacao.

## Regras

- Nao criar novo `MainMenuController`.
- Nao criar nova cena principal sem necessidade.
- Nao mover scripts legados enquanto houver referencia por GUID.
- Nao apagar cenas antigas antes de mapear dependencias.
- Toda feature nova deve preservar `MainMenu -> Beta03_Principal`.
