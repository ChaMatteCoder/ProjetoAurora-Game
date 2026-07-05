# Pre-Build Polish — Tutorial Gating e Setas (Round 11)

## Problema
Depois da correção de interrupções, a ação de cada etapa liberava no MESMO instante da
fala principal — o jogador executava imediatamente, a etapa completava e
`StopTutorialVoice()` cortava a fala. Em cadeia, o tutorial inteiro ficava mudo.

## Máquina de estados por etapa

```
TutorialStepState { WaitingForInstructionVoice, ActionEnabled, Completed }
```

Fluxo por etapa (TutorialManager):
1. **Trigger** → `ActivateStep`: `CurrentAllowedAction = None` (TODOS os inputs de ação
   bloqueados), `SetAutoRun(false)`, prompt/seta ocultos, toca a instrução principal
   (`PlaySequence` com onComplete).
2. **Fala termina** (onComplete natural) → `EnableCurrentStepAction`: libera
   `CurrentAllowedAction = requiredAction`, mostra prompt da HUD, mostra a SETA animada,
   arma o lembrete.
3. **Jogador executa** → `CompleteCurrentStep`: seta some, `Completed`, retoma cruzeiro.

### Segurança escolhida — Opção C (Safe hold zone), já estrutural
`ActivateStep` SEMPRE fez `player.SetAutoRun(false)`: o runner PARA no trigger da etapa.
Logo, bloquear a ação durante a fala não move o jogador — impossível passar do obstáculo,
impossível dano injusto (obstáculos estão à frente do ponto de parada). Não foi preciso
mexer em velocidade nem Time.timeScale. Decisão documentada: a menos invasiva, com
comportamento idêntico ao anterior exceto pelo momento da liberação.

### Nunca prender o jogador (watchdog)
`StepGateRoutine` roda em paralelo ao onComplete:
- fala presente: libera quando `!IsPlayingGroup(Tutorial)` (cobre cancelamento por evento
  crítico sem callback) após mínimo de 0,5s; teto duro `maxInstructionWaitSeconds = 12s`;
- fala ausente (fallback de texto): libera após `fallbackInstructionSeconds = 2,6s`;
- guarda por versão (`tutorialVoiceVersion`) evita liberação de etapa antiga.

### Lembretes
- Só armam em `ActionEnabled` (nunca durante a instrução);
- O `ReminderRoutine` existente já espera o grupo Tutorial silenciar e valida
  etapa/versão — lembretes não bloqueiam nada e não tocam após a conclusão.

## Setas animadas (TutorialArrowIndicator)

Indicador semi-diegético no MUNDO (não UI genérica): chevrons ciano emissivos
(2 lâminas em ">") com pulso de emissão + deslize em loop na direção da ação;
etiquetas de tecla ("ESPAÇO"/"E") em TextMeshPro 3D viradas para a câmera.

| Ação | Visual | Posição |
|---|---|---|
| Direita/Esquerda | 3 chevrons deslizando ±X | faixa-alvo (lane destino), z do trigger +5,5 |
| Pulo | chevrons para cima + "ESPAÇO" | faixa do player, sobre o obstáculo (offset do pulo +0,6) |
| Interação | "E" pulsando | sobre o console do painel (busca filho "Console") |

Regras: aparece SÓ após a instrução terminar; some ao executar/completar/terminar o
tutorial; nunca em cutscene (só existe dentro do fluxo de etapa). Material criado em
runtime com RealtimeEmissive (URP).

## Validado em play ([R11V])
5/5 etapas: em WAITING todas as ações bloqueadas e seta oculta; liberação após a fala
REAL de dublagem (6,4s / 5,3s / 5,3s / 5,0s / 3,8s); seta visível em todas; conclusão
avança sem falas presas; tutorial termina e `StartFullRun` assume.
