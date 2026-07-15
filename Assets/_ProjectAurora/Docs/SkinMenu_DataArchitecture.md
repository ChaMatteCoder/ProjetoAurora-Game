# Menu de Skins - Arquitetura de Dados

## Componentes

### AuroraSkinDefinition

Cada skin é um `ScriptableObject` em `Assets/_ProjectAurora/Data/Skins/Definitions` com:

- identidade: `id`, `displayName`, `description`;
- conteúdo: `splashArt`, `previewPrefab`, `gameplayPrefab`;
- disponibilidade: `unlockedByDefault`, `futureUnlockId`;
- preparação futura: `futurePrice`, sem uso na UI atual;
- marcação de default: `isDefaultSkin`;
- ajuste de preview: posição, rotação, escala, distância e cor de fundo.

O ID é a chave estável usada pelo save. Nome visual, índice, caminho e referência de prefab não são persistidos.

### AuroraSkinCatalog

Asset: `Assets/_ProjectAurora/Data/Skins/AuroraSkinCatalog.asset`.

Responsabilidades:

- manter uma lista ordenada;
- resolver `GetById`;
- resolver `GetDefaultSkin`;
- validar entradas nulas, IDs vazios/duplicados, Splash Arts e quantidade de defaults.

Ordem atual:

1. `default`
2. `brazil`
3. `aurora-ceremonial`
4. `celestia-theme`
5. `corrupted`
6. `post-collapse-survivor`

### AuroraSkinCatalogBuilder

Menu: `Tools/Projeto Aurora/Skins/Rebuild Skin Catalog`.

O builder:

1. escaneia `Assets/_ProjectAurora/Art/Skin` por `Splash_`;
2. normaliza um ID seguro e determinístico;
3. configura cada imagem como Sprite/Single, sem mipmaps, Bilinear, High Quality e Max Size 2048;
4. preserva ajustes manuais das definições já existentes;
5. cria a definição quando necessário;
6. associa apenas modelos exatos ou explicitamente conhecidos;
7. mantém `previewPrefab` e `gameplayPrefab` nulos quando não há correspondência segura;
8. ordena o catálogo e registra ausências no Console.

O modelo padrão gera `PF_DrElias_Default_SkinPreview`, uma cópia sanitizada para preview. O prefab original não é modificado.

## Unlocks e economia

`AuroraSkinSelectionService.IsUnlocked` segue esta ordem:

1. skin default;
2. `unlockedByDefault`;
3. `AuroraCoinWallet.IsUnlocked(futureUnlockId, Skin)`.

Assim, a base já conversa com os unlocks centrais. `futurePrice` existe somente para evolução futura: não há preço, compra ou desconto nesta tela.

## Como adicionar uma skin

1. Adicionar `Splash_NomeDaSkin.png` em `Assets/_ProjectAurora/Art/Skin`.
2. Adicionar um prefab/modelo de preview correspondente, com nome inequívoco.
3. Executar `Tools/Projeto Aurora/Skins/Rebuild Skin Catalog`.
4. Abrir ou criar a `AuroraSkinDefinition` correspondente.
5. Preencher nome, descrição, `previewPrefab`, `gameplayPrefab`, `futureUnlockId` e `futurePrice`.
6. Ajustar offsets de preview apenas se o enquadramento automático precisar de correção artística.
7. Testar navegação, T-pose, seleção, retorno e persistência.

Se o builder não reconhecer o modelo com segurança, a associação deve ser feita manualmente na definição. Não se deve ampliar a busca para nomes vagos.
