# Relatório final — interrupções de voz

## Problema encontrado

A fila global não conhecia o estado que originou cada fala. O tutorial avançava, mas instruções e lembretes anteriores continuavam válidos. Na intro, `allowSkip` permitia que o `Enter` residual do menu cortasse `ELI_001` e avançasse para CelestIA.

## Arquivos alterados

- Sistema: `VoiceLinePlayer.cs`, `VoicePlaybackOptions.cs`, `DialogueManager.cs`.
- Tutorial: `TutorialManager.cs`.
- Fluxos: `IntroCutsceneController.cs`, `NarrativeEventManager.cs`, `FinalCutsceneController.cs`, `GameOverManager.cs`.
- Contexto: `PlayerHealth.cs`, `SuitIntegrityRecovery.cs`, `InteractableBase.cs`, `InteractableObject.cs`, `CelestIAController.cs`.
- HUD: `UIManager.cs`, `AuroraGameplayHUDController.cs`.

## Resultado

- Falas do tutorial são canceladas ao concluir ou trocar etapa.
- Lembretes antigos não sobrevivem à versão da etapa.
- Input e avanço do tutorial não aguardam áudio.
- Fila Tutorial fica vazia ao sair do tutorial.
- HUD e retrato não recebem conclusão tardia de playback cancelado.
- Contexto não interrompe tutorial/cutscene/final.
- Game Over e final possuem interrupção crítica.
- `ELI_001` usa sua duração real no relógio DSP e não é mais pulada por `Enter/Espaço`; `Esc` continua pulando a intro.

## Testes executados

- Compilação pelo Unity 6000.4.10f1: sem erros de C#.
- Duração lida pelo Unity: `ELI_001 = 3,1608 s`; `CEL_002 = 2,0376 s`.
- Play Mode na cena canônica carregada a partir do MainMenu.
- Sequência observada no histórico runtime: `ELI_001` antes de `CEL_002`.
- Repetição após relógio DSP: início de `CEL_002` ocorreu 5,2649 s após `ELI_001`, acima do mínimo calculado de 3,3108 s; não houve corte de Elias.
- Teste rápido de direita: fala Tutorial ativa antes da ação; após `NotifyMoveRight`, grupo parado e fila Tutorial igual a zero.
- Teste rápido de esquerda, dois pulos e painel: tutorial terminou com `IsComplete = true`, estado `Playing` e nenhuma fala do grupo Tutorial ativa.
- Game Over em runtime: estado mudou para `GameOver` e `CEL_056` tornou-se a linha atual.
- Validação transitória da cena `Beta03_Principal`: 6.504 objetos inspecionados e 0 Missing Scripts.

## Pendências e riscos

- O teste manual completo com teclado, dano real e percurso até o terminal ainda é recomendado para avaliação audiovisual.
- O Console apresenta mensagens preexistentes do Windows Media Foundation sobre primárias de cor/timestamps dos vídeos H.264; não são erros de compilação nem foram causadas por esta alteração.
- `RobotPursuitDirector` ainda usa mensagens visuais hardcoded sem IDs de dublagem; o grupo `RobotChase` ficou disponível para migração futura quando houver IDs oficiais.
