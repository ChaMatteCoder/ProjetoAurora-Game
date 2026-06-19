# PROJETO:AURORA - Mapa Canonico de Arquitetura

## 1. Cena canonica recomendada

Cena recomendada para gameplay canonico:

`Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## 2. Motivo da escolha

`Beta03_Principal.unity` e a melhor fonte canonica atual porque:

- ja esta registrada em `ProjectSettings/EditorBuildSettings.asset` como cena de gameplay ativa, logo representa o fluxo que o projeto tenta executar hoje;
- contem Player/Dr. Elias;
- contem `GameManager`;
- contem `HUD Canvas`;
- contem `AuroraGameplayHUDController`;
- contem `UIManager`;
- contem sistema de tutorial;
- contem `Canvas_GameOver` e referencia `GameOverManager`;
- contem `Fase05 - Terminal Central` e `Terminal Central Access`, indicando que integra a reta final/beta;
- contem mais referencias para scripts em `Assets/_ProjectAurora/Scripts` do que as outras candidatas;
- parece ser a cena beta integrada mais avancada, nao apenas uma cena base ou legado.

Resumo das cenas analisadas:

| Cena | Player | GameManager | HUD | Tutorial | GameOver | Menu | Visual final | Classificacao |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Assets/_ProjectAurora/Scenes/FASE 01 - Laboratorio Limpo A/Fase01_SetorA_LaboratorioLimpo.unity` | Sim | Sim | Sim, com `AuroraGameplayHUDController` | Sim | Sim, com `Canvas_GameOver`/`GameOverManager` | Nao | Sim, Fase 01 detalhada | Cena de fase/base avancada |
| `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` | Sim | Sim | Sim, com `AuroraGameplayHUDController` | Sim | Sim, com `Canvas_GameOver`/`GameOverManager` | Nao | Sim, Fase 01 + Terminal Central integrado | Beta canonico recomendado |
| `Assets/Scenes/Game.unity` | Sim | Sim | Sim, mas sem `AuroraGameplayHUDController` referenciado diretamente | Sim | Parcial/legado; nao referencia `GameOverManager` | Nao | Sim, mas menos atual | Legado/base antiga |

Scripts principais referenciados por cena:

### `Fase01_SetorA_LaboratorioLimpo.unity`

Referencia scripts em `Assets/Scripts`:

- `AudioManager.cs`
- `CameraFollow.cs`
- `CelestIAController.cs`
- `GameManager.cs`
- `InteractableObject.cs`
- `LaserHazard.cs`
- `Obstacle.cs`
- `ObstacleSpawner.cs`
- `PlayerHealth.cs`
- `PlayerInteraction.cs`
- `PlayerRunner.cs`
- `SectorManager.cs`
- `TutorialManager.cs`
- `UIManager.cs`

Referencia scripts em `Assets/_ProjectAurora/Scripts`:

- `Player/DrEliasAnimationController.cs`
- `UI/AuroraGameplayHUDController.cs`
- `UI/CelestIACommPanel.cs`
- `UI/GameOverManager.cs`

### `Beta03_Principal.unity`

Referencia scripts em `Assets/Scripts`:

- `AudioManager.cs`
- `CameraFollow.cs`
- `CelestIAController.cs`
- `FinalCutsceneController.cs`
- `GameManager.cs`
- `InteractableObject.cs`
- `LaserHazard.cs`
- `Obstacle.cs`
- `ObstacleSpawner.cs`
- `PlayerHealth.cs`
- `PlayerInteraction.cs`
- `PlayerRunner.cs`
- `SectorManager.cs`
- `TutorialManager.cs`
- `UIManager.cs`

Referencia scripts em `Assets/_ProjectAurora/Scripts`:

- `Environment/TerminalFinalePresentation.cs`
- `Interactions/DoorInteractable.cs`
- `Interactions/LaserInteractable.cs`
- `Interactions/MovingBlockInteractable.cs`
- `Player/DrEliasAnimationController.cs`
- `UI/AuroraGameplayHUDController.cs`
- `UI/CelestIACommPanel.cs`
- `UI/GameOverManager.cs`

### `Assets/Scenes/Game.unity`

Referencia scripts em `Assets/Scripts`:

- `AudioManager.cs`
- `CameraFollow.cs`
- `CelestIAController.cs`
- `CelestIAHudController.cs`
- `GameManager.cs`
- `InteractableObject.cs`
- `LaserHazard.cs`
- `Obstacle.cs`
- `ObstacleSpawner.cs`
- `PlayerHealth.cs`
- `PlayerInteraction.cs`
- `PlayerRunner.cs`
- `SectorManager.cs`
- `TutorialManager.cs`
- `UIManager.cs`

Referencia scripts em `Assets/_ProjectAurora/Scripts`:

- Nenhum script runtime de `_ProjectAurora/Scripts` foi detectado diretamente na cena.

## 3. Cenas que devem virar legado

Recomendacao:

- `Assets/Scenes/Game.unity` deve ser tratada como legado/base antiga. Ela depende apenas de `Assets/Scripts`, nao referencia os sistemas novos de `_ProjectAurora/Scripts`, e nao parece representar o beta atual.
- `Assets/_ProjectAurora/Scenes/FASE 01 - Laboratorio Limpo A/Fase01_SetorA_LaboratorioLimpo.unity` deve ser preservada por enquanto como cena de fase/base. Ela e mais atual que `Assets/Scenes/Game.unity`, mas nao integra tanto conteudo beta quanto `Beta03_Principal.unity`.

Nada deve ser apagado ou movido antes de validar referencias de cenas, prefabs, build settings e fluxo de Play Mode.

## 4. Controlador de menu recomendado

Controlador canonico recomendado:

`Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

Motivos:

- e o unico dos tres controladores de menu referenciado pela cena atual `Assets/_ProjectAurora/Scenes/MainMenu.unity`;
- trabalha junto com `AuroraMenuCard`;
- carrega cenas candidatas atuais: `Beta03_Principal` e `Fase01_SetorA_LaboratorioLimpo`;
- esta na pasta mais especifica e mais nova: `Assets/_ProjectAurora/Scripts/UI/Menu`;
- e coerente com o menu visual por cards gerado recentemente.

Mapeamento dos tres controladores:

| Script | Referencias detectadas em cena/prefab | Situacao | Recomendacao |
| --- | --- | --- | --- |
| `Assets/Scripts/MainMenuController.cs` | Referenciado por `Assets/Scenes/MainMenu.unity` | Legado | Nao remover agora; provavelmente pertence ao menu antigo que carrega `Game` |
| `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs` | Nenhuma referencia direta detectada em `.unity`/`.prefab` | Intermediario/legado organizado | Pode virar legado apos confirmar que nenhuma cena oculta ou prefab nao mapeado usa esse tipo |
| `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs` | Referenciado por `Assets/_ProjectAurora/Scenes/MainMenu.unity` | Atual | Canonico recomendado |

Riscos de remover/desativar menu:

- `Assets/Scenes/MainMenu.unity` ainda referencia o `MainMenuController` legado de `Assets/Scripts`.
- `ProjectSettings/EditorBuildSettings.asset` usa `Assets/_ProjectAurora/Scenes/MainMenu.unity`, nao `Assets/Scenes/MainMenu.unity`.
- Remover o script legado antes de arquivar/remover a cena legada pode gerar Missing Script em `Assets/Scenes/MainMenu.unity`.

## 5. Scripts em `Assets/Scripts` ainda usados pela cena canonica sugerida

`Beta03_Principal.unity` ainda usa estes scripts em `Assets/Scripts`:

- `AudioManager.cs`
- `CameraFollow.cs`
- `CelestIAController.cs`
- `FinalCutsceneController.cs`
- `GameManager.cs`
- `InteractableObject.cs`
- `LaserHazard.cs`
- `Obstacle.cs`
- `ObstacleSpawner.cs`
- `PlayerHealth.cs`
- `PlayerInteraction.cs`
- `PlayerRunner.cs`
- `SectorManager.cs`
- `TutorialManager.cs`
- `UIManager.cs`

Esses scripts nao devem ser movidos, renomeados ou apagados agora. A cena canonica recomendada depende deles diretamente.

## 6. Scripts duplicados encontrados

Duplicacao direta por nome:

- `Assets/Scripts/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`

Duplicacao funcional/arquitetural:

- Menu antigo: `Assets/Scripts/MainMenuController.cs`
- Menu intermediario: `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`
- Menu atual por cards: `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

Sobreposicoes conceituais que exigem cuidado:

- `CelestIAHudController.cs` em `Assets/Scripts` e `AuroraGameplayHUDController.cs`/`CelestIACommPanel.cs` em `_ProjectAurora/Scripts/UI` representam geracoes diferentes de HUD/feedback da CelestIA.
- `InteractableObject.cs` em `Assets/Scripts` convive com `DoorInteractable`, `LaserInteractable`, `MovingBlockInteractable` e `InteractableBase` em `_ProjectAurora/Scripts/Interactions`.
- `UIManager.cs` em `Assets/Scripts` continua sendo ponte central entre gameplay e HUD novo, inclusive em prefab `Assets/_ProjectAurora/Prefabs/UI/HUD_Fase01.prefab`.

## 7. Riscos antes de mover/apagar qualquer coisa

- `Beta03_Principal.unity` ainda depende fortemente de `Assets/Scripts`; migrar a pasta inteira de uma vez quebraria referencias de scripts por GUID.
- `Assets/Scenes/Game.unity` e `Assets/Scenes/MainMenu.unity` parecem legados, mas ainda podem servir como fallback, fonte de copia ou historico funcional.
- A cena canonica recomendada mistura sistemas antigos e novos. Isso confirma a necessidade de uma fase de estabilizacao antes de limpeza.
- `GameManager.cs` contem branches especiais de preview/terminal e ainda e usado pela cena canonica; refatorar isso antes de escolher um modelo de cena pode quebrar o fluxo beta.
- `TutorialManager.cs` concentra manager, gate e trigger no mesmo arquivo, mas e usado diretamente pela cena canonica; dividir agora exigiria migracao de GUIDs e validacao no Unity.
- `UIManager.cs` e legado em nome/localizacao, mas nao e descartavel: a cena e o prefab de HUD ainda dependem dele.
- Remover controladores de menu antigos sem limpar cenas antigas causara Missing Script.
- Alterar Build Settings antes de consolidar cenas pode mascarar o problema em vez de resolve-lo.

## 8. Proxima acao recomendada

Proxima acao segura:

1. Abrir `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` no Unity.
2. Rodar Play Mode pelo fluxo atual `Assets/_ProjectAurora/Scenes/MainMenu.unity -> Beta03_Principal`.
3. Validar manualmente: menu, iniciar jogo, movimento, tutorial, HUD, colisao/vida, Game Over, terminal final.
4. So depois criar um plano de migracao incremental para mover scripts ainda vivos de `Assets/Scripts` para uma estrutura canonica em `_ProjectAurora/Scripts`, preservando GUIDs ou usando migracao controlada pelo Unity.

Nao mover, apagar ou renomear nada antes dessa validacao.
