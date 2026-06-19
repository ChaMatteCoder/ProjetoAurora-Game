# Menu Controller Dependency Report

## Controller canonico

- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

## Scripts analisados

- `Assets/Scripts/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

## Referencias encontradas

### `Assets/Scripts/MainMenuController.cs`

- GUID: `dd24859dc3c67094ab996ff87d0511df`
- Referencias serializadas encontradas:
  - `Assets/Scenes/MainMenu.unity`
- Prefabs encontrados:
  - Nenhum.
- Assets serializados encontrados:
  - Nenhum.
- Build Settings:
  - Nao referencia diretamente este script.

Observacao:

- Este script carrega a cena `"Game"` diretamente.
- A cena `Assets/Scenes/MainMenu.unity` nao e o menu canonico atual, mas ainda pode quebrar com Missing Script se este arquivo for removido.

### `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`

- GUID: `0a6db05cb19794e4db1aca5e4fb092c3`
- Referencias serializadas encontradas:
  - Nenhuma em `.unity`, `.prefab`, `.asset`, `.controller` ou `.overrideController` dentro de `Assets`.
- Prefabs encontrados:
  - Nenhum.
- Assets serializados encontrados:
  - Nenhum.
- Build Settings:
  - Nao referencia diretamente este script.

Observacao:

- Parece ser uma versao intermediaria organizada do menu.
- Mesmo sem referencia encontrada, nao deve ser removido ainda sem uma checagem final no Unity, porque scripts podem ser usados por tooling, cenas nao carregadas ou alteracoes locais futuras.

### `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

- GUID: `bef7d2d6ce235b14fb3632b51f39f50b`
- Referencias serializadas encontradas:
  - `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- Prefabs encontrados:
  - Nenhum prefab referencia diretamente este controller.
- Assets serializados encontrados:
  - Nenhum.
- Build Settings:
  - A cena `Assets/_ProjectAurora/Scenes/MainMenu.unity` esta no indice 0 do Build Settings.

Observacao:

- Este e o controller oficial do menu canonico.
- O fluxo canonico `MainMenu -> Beta03_Principal` depende deste script permanecer no lugar.

## Classificacao

| Script | Classificacao | Motivo |
| --- | --- | --- |
| `Assets/Scripts/MainMenuController.cs` | LEGADO AINDA REFERENCIADO | Referenciado por `Assets/Scenes/MainMenu.unity`. |
| `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs` | LEGADO SEM REFERENCIA ENCONTRADA | Nenhuma referencia serializada encontrada nas buscas feitas. |
| `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs` | CANONICO | Referenciado por `Assets/_ProjectAurora/Scenes/MainMenu.unity`, que e a cena de menu oficial. |

## Riscos

### `Assets/Scripts/MainMenuController.cs`

- Risco de remover/mover: medio.
- Motivo:
  - A cena legada `Assets/Scenes/MainMenu.unity` referencia o script por GUID.
  - Remover ou mover fora do Unity pode gerar Missing Script nessa cena.
- Cenas/prefabs que poderiam quebrar:
  - `Assets/Scenes/MainMenu.unity`
- Depende de GUID Unity:
  - Sim.
- Pode ser removido futuramente:
  - Sim, mas somente depois de decidir arquivar/remover ou migrar a cena legada `Assets/Scenes/MainMenu.unity`.
- Recomendacao atual:
  - Deixar como legado intocado.

### `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`

- Risco de remover/mover: baixo a medio.
- Motivo:
  - Nenhuma referencia serializada foi encontrada, mas e uma classe de menu com namespace proprio e pode estar preservada como transicao historica.
  - Remover agora nao e necessario para estabilizar o beta.
- Cenas/prefabs que poderiam quebrar:
  - Nenhuma referencia direta encontrada.
- Depende de GUID Unity:
  - Nao foi encontrada dependencia serializada por GUID.
- Pode ser removido futuramente:
  - Provavelmente sim, apos confirmacao no Unity e revisao de historico/uso por builders.
- Recomendacao atual:
  - Manter como legado sem referencia encontrada ate a etapa formal de limpeza.

### `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

- Risco de remover/mover: alto.
- Motivo:
  - E o controller canonico do menu oficial.
  - `Assets/_ProjectAurora/Scenes/MainMenu.unity` referencia este script por GUID.
  - O Build Settings inicia pelo menu oficial.
- Cenas/prefabs que poderiam quebrar:
  - `Assets/_ProjectAurora/Scenes/MainMenu.unity`
  - Fluxo `MainMenu -> Beta03_Principal`
- Depende de GUID Unity:
  - Sim.
- Pode ser removido futuramente:
  - Nao, enquanto for o menu canonico.
- Recomendacao atual:
  - Manter como controller oficial.

## Recomendacao

- Continuar usando `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs` como unico controller canonico de menu.
- Nao remover `Assets/Scripts/MainMenuController.cs` agora, porque ele ainda e referenciado por `Assets/Scenes/MainMenu.unity`.
- Nao remover `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs` agora, apesar de nao haver referencia serializada encontrada.
- Nao alterar `Build Settings`, cenas ou prefabs nesta etapa.

## Proxima acao sugerida

Proxima etapa segura, sem executar agora:

1. Abrir o Unity e confirmar visualmente que `Assets/_ProjectAurora/Scenes/MainMenu.unity` usa apenas `AuroraMainMenuController`.
2. Confirmar se `Assets/Scenes/MainMenu.unity` ainda precisa ser preservada como cena legada funcional.
3. Se a cena legada for arquivada em etapa futura, criar um plano separado para remover ou isolar `Assets/Scripts/MainMenuController.cs`.
4. Depois disso, revisar builders de menu para impedir que recriem controladores antigos.
