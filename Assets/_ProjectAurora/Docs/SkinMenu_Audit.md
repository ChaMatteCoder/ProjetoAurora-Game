# Menu de Skins - Auditoria

Data da auditoria: 14/07/2026

Cena oficial: `Assets/_ProjectAurora/Scenes/MainMenu.unity`

Fluxo preservado: `MainMenu -> Beta03_Principal`

## Escopo verificado

- `AuroraMainMenuController` e navegação de painéis.
- Painel `Panel_Extra`, hub, botão `SKIN`, placeholder anterior e botão de retorno.
- `Canvas_MainMenu`, `CanvasScaler` em 1920x1080 e `EventSystem` existente.
- Pasta `Assets/_ProjectAurora/Art/Skin` e todos os arquivos `Splash_`.
- Prefabs e FBX relacionados ao Dr. Elias e a possíveis variações de skin.
- `AuroraProgressSaveData`, `AuroraProgressSaveService` e `AuroraCoinWallet`.
- `AuroraUnlockCatalog` e `AuroraPurchaseService`.
- Scripts existentes com termos Skin, Customization, CharacterVisual e Outfit.

## Splash Arts encontradas

Todas possuem 1672x941 pixels, razão 1,7768, compatível com 16:9.

| Arquivo | ID derivado | Nome no menu | Modelo associado |
|---|---|---|---|
| `Splash_Dr.Elias.png` | `default` | Dr. Elias | Sim |
| `Splash_Brazil Dr. Elias.png` | `brazil` | Brasil | Não |
| `Splash_Aurora Ceremonial Dr. Elias.png` | `aurora-ceremonial` | Cerimonial Aurora | Não |
| `Splash_CelestIA Theme Dr. Elias.png` | `celestia-theme` | Tema CelestIA | Não |
| `Splash_Corrupted Dr. Elias.png` | `corrupted` | Corrompido | Não |
| `Splash_Post-Collapse Survivor Dr. Elias.png` | `post-collapse-survivor` | Sobrevivente Pós-Colapso | Não |

## Modelos encontrados

Modelo válido do personagem principal:

- `Assets/_ProjectAurora/Characters/DrElias/Prefabs/DrElias_AnimatedVisual.prefab`
- Visual aninhado proveniente do FBX do Dr. Elias.

Foi criado um prefab de preview sanitizado, sem alterar o modelo original:

- `Assets/_ProjectAurora/Prefabs/UI/Menu/PF_DrElias_Default_SkinPreview.prefab`

Não foram encontrados prefabs ou FBX inequívocos para as cinco variações adicionais. As imagens sem prefixo `Splash_` também são artes 2D e não foram tratadas como modelos.

## Lacunas

Skins com Splash Art e sem modelo: `brazil`, `aurora-ceremonial`, `celestia-theme`, `corrupted` e `post-collapse-survivor`.

Modelos sem Splash Art: nenhum modelo de skin órfão foi identificado.

O builder usa associação exata e conhecida. Não há busca vaga que possa ligar uma arte ao modelo errado.

## Sistemas reaproveitados

- Save central: `AuroraProgressSaveData` e `AuroraProgressSaveService`.
- Autoridade de progresso em runtime: `AuroraCoinWallet`.
- Unlocks: listas `unlockedSkins` do save e consulta `AuroraCoinWallet.IsUnlocked`.
- Economia futura: `AuroraUnlockCatalog` e `AuroraPurchaseService`, sem compra no menu desta rodada.
- Navegação: `AuroraMainMenuController` e `AuroraMenuExtraController`.

Nenhum `PlayerPrefs`, save paralelo, catálogo paralelo de unlock ou nova cena foi criado.

## Riscos e decisões

- Cinco entradas são somente visuais até receberem `previewPrefab` e `gameplayPrefab`; elas não podem ser equipadas.
- `Brazil` fica desbloqueada por padrão, mas indisponível para seleção enquanto não houver modelo funcional.
- As demais variações ficam bloqueadas e visíveis no catálogo.
- A fonte do modelo padrão possui animação de corrida, mas o prefab de preview desativa o `Animator` e preserva a pose de referência em T.
- Esta versão salva a escolha, mas não substitui o visual do Player na gameplay.
- Nenhum asset de arte ou modelo existente foi movido, renomeado ou reimportado como rig diferente.
