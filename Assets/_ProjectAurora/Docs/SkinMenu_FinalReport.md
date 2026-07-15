# Menu de Skins - Relatório Final

## Resultado

A primeira versão funcional do menu foi instalada em `MainMenu.unity`, integrada ao botão `Extra -> SKIN` e ao save central. Ela permite visualizar seis Splash Arts, navegar, distinguir skin visualizada de equipada, exibir preview 3D quando existe modelo e restaurar a seleção.

## Catálogo atual

| ID | Splash | Preview 3D | Gameplay prefab | Estado inicial |
|---|---|---|---|---|
| `default` | Sim | Dr. Elias em T-pose | Sim | Equipada/desbloqueada |
| `brazil` | Sim | Não | Não | Desbloqueada, indisponível |
| `aurora-ceremonial` | Sim | Não | Não | Bloqueada |
| `celestia-theme` | Sim | Não | Não | Bloqueada |
| `corrupted` | Sim | Não | Não | Bloqueada |
| `post-collapse-survivor` | Sim | Não | Não | Bloqueada |

Todas as Splash Arts estão como Sprite Single, sem mipmaps, Bilinear, High Quality, Max Size 2048 e exibidas com `AspectRatioFitter` 16:9 em `Fit In Parent`.

## Implementação

- Dados: `AuroraSkinDefinition`, `AuroraSkinCatalog` e seis assets de definição.
- Builder: `Tools/Projeto Aurora/Skins/Rebuild Skin Catalog`.
- Instalador idempotente: `Tools/Projeto Aurora/Skins/Install Or Update Skin Menu`.
- Seleção: `AuroraSkinSelectionService` e `AuroraSkinSelectionController`.
- Preview: `AuroraSkinPreviewController`, layer `SkinPreview` e RenderTexture 1024x1024.
- Persistência: campo `selectedSkinId` no save central, sem sistema paralelo.
- UI: `SkinSelectionPanel` responsivo no Canvas existente, com navegação, estados e retorno ao Extra.

## Testes executados

- Compilação após cada rodada: 0 erros.
- `Tools/Projeto Aurora/Skins/Run Skin Menu Tests`: PASS, 139 assertions.
- Catálogo: 6 entradas, IDs únicos, ordem estável e um default.
- Importadores: dimensões e configurações das 6 Splash Arts verificadas.
- Save temporário: seleção, recarga, evento único e fallback de IDs inválidos/bloqueados.
- Runtime: abriu `MainMenu -> Extra -> SKIN`.
- Runtime: navegou default, Brasil e Cerimonial Aurora sem alterar a equipada.
- Runtime: skin bloqueada e skin sem modelo recusaram seleção.
- Runtime: wrap de navegação validado.
- Runtime: somente um preview, câmera ativa apenas no default e desligada ao voltar.
- Runtime: clique real em `VOLTAR` retornou ao hub do Extra.
- Resoluções: capturas nativas em 1920x1080 e 1280x720, sem cortes ou sobreposição.
- Regressão: `JOGAR` carregou `Beta03_Principal`.
- Console após o fluxo completo: 0 erros.

## Limitações desta versão

- Cinco artes ainda não possuem modelo 3D ou gameplay prefab.
- Não há compra, preço visível, raridade, conquista ou geração de skin.
- A skin salva ainda não troca automaticamente o visual na gameplay.
- Não foi adicionada rotação por mouse ou animação automática do preview.

## Como adicionar uma nova skin

1. Adicionar `Splash_NomeDaSkin.png` em `Assets/_ProjectAurora/Art/Skin`.
2. Adicionar um prefab/modelo de preview correspondente.
3. Executar `Tools/Projeto Aurora/Skins/Rebuild Skin Catalog`.
4. Preencher nome, descrição, `previewPrefab`, `gameplayPrefab`, unlock ID e preço futuro na definição.
5. Ajustar offsets apenas quando necessário.
6. Executar `Tools/Projeto Aurora/Skins/Run Skin Menu Tests` e validar visualmente no menu.

## Pendências futuras

- Produzir e validar os cinco modelos faltantes.
- Criar o adaptador de visual para gameplay quando rigs e Animator forem definidos.
- Conectar preços e compra somente numa rodada específica de loja/economia.
