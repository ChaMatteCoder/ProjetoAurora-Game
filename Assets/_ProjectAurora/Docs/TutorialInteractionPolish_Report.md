# Tutorial & Interaction Polish — Report (Round 14)

**Veredito: APROVADO.** Setas na direção certa, "ESPAÇO" legível, "E" cinematográfico,
card com padding, fala de conclusão do tutorial não cortada, e os três bugs de fluxo
(porta travando, terminal fora do mapa) corrigidos.

## Tarefa 1 — Direção das setas (INVERTIDAS → corrigidas)
Causa raiz: no `TutorialArrowIndicator`, as duas lâminas do chevron convergiam em **-X**
(a ponta do ">" apontava para a esquerda), mas `ShowLane` usava rotação identidade para
"direita". Resultado: direita apontava esquerda e vice-versa.
Correção: inverti os sinais de tilt das lâminas (a ponta agora fica em +X). Assim
`identidade = direita (+X)` e `180° = esquerda (-X)`, alinhados ao deslize.
Validado (medindo a ponta vs base em viewport):
- MoveRight → `pontaVp.x - baseVp.x = +0.035` → **aponta DIREITA**
- MoveLeft → `-0.037` → **aponta ESQUERDA**
(A seta de pulo também passou a apontar corretamente para cima.)

## Tarefa 2 — "ESPAÇO" legível
Antes o texto ficava atrás/misturado às setas. Agora a etiqueta é uma placa própria:
moldura ciano + placa escura opaca + texto branco-ciano com **contorno**, posicionada
**acima** dos chevrons de pulo (localY 2.35). Billboard para a câmera.
Validado: label ativo, y=2.4, legível na captura `r14_space_clean.png`.

## Tarefa 3 — "E" cinematográfico
Novo indicador de interação: **moldura hexagonal** de 6 barras ciano emissivas + placa
escura ao fundo + "E" grande com contorno. Pulso de **escala** (0.92↔1.10) e de emissão
(chamada), billboard para a câmera. Posicionado no console do painel (fora do Dr. Elias).
Validado visualmente: `r14_e_clean.png`.

## Tarefa 4 — Card "PRESSIONE E — ACIONAR PAINEL"
- padding seguro via `margin` (26 lateral / 8 vertical) — nenhuma letra encosta na borda;
- largura 380→**460**, altura 62;
- **auto-size 15–24** com no-wrap: textos longos (ACIONAR PAINEL) cabem sem cortar.
- Typo "Precione": **não existe no projeto** — todos os prompts já dizem "Pressione"
  (verificado por busca global; provavelmente um estado/build antigo). Runtime confirmado:
  card mostra "Pressione E".

## Tarefa 5 — Bugs de fluxo

### 5a. Fala de conclusão do tutorial cortada
CEL_019 ("Acesso liberado") era interrompida por CEL_020 ("Setor A comprometido").
Correção: `PlayTutorialCompletion` agora toca CEL_019 com **prioridade Cutscene**. O
VoiceLinePlayer não interrompe algo com prioridade ≥ Cutscene, então a SectorNarrative
(CEL_020/021) **enfileira** e só toca após CEL_019 terminar. GameOver/Final ainda podem
interromper; não bloqueia gameplay.
Validado (teste determinístico): tocando CEL_019@Cutscene e disparando CEL_020@Narrative
com `interruptCurrent=true`, a fala atual permaneceu **CEL_019** (não cortada); CEL_020
ficou na fila. CEL_019 = 2.5s; CEL_020 toca em seguida.

### 5b. Porta travando o jogo (softlock)
Se o jogador não interagia (E), portas de bloqueio total (ex.: Containment Door z520)
prendiam o jogo para sempre. Novo `SoftlockDoorGuard` (no Game Systems): durante Playing,
se o jogador para de avançar em Z por 2.5s (autorun ligado) com uma `AuroraDoorController`
fechada logo à frente, a porta é **arrombada** — leva 1 de dano e abre, liberando o
caminho. Rearma por 3s para não repetir dano na mesma porta.
Validado: ao completar o tutorial sem usar E, o guard arrombou em sequência
TutorialDoor_Containment, Interactable_Door_01 e Containment Door — jogador nunca ficou
preso (chegou a z212).

### 5c. Terminal fora do mapa
O trigger de acesso ao Terminal cobria apenas **x −2.5..+2.5**, mas as lanes ficam em
x=±3 — um jogador em lane lateral nunca acessava e corria para a parede do fundo.
Correção dupla:
- **Funil** (`TerminalApproachFunnel` @ z2635): ao entrar, trava o jogador na lane central
  (`PlayerRunner.SetLaneLockedCenter`), impedindo sair pelas laterais;
- **trigger alargado** para x ±5 (cobre as 3 lanes) como rede de segurança.
Validado: jogador iniciando na lane esquerda (x=-3) saiu do funil em **x=0.00** e acessou
o terminal (E → FinalCutscene), inclusive com a tela "FIM DA CONTENÇÃO" ao final.

## Tarefa 6 — Testes (todos em Play Mode)
| Item | Resultado |
|---|---|
| Tutorial direita (seta) | ✅ aponta direita |
| Tutorial esquerda (seta) | ✅ aponta esquerda |
| Pulo (ESPAÇO acima da seta) | ✅ legível |
| Interação E (hexágono animado) | ✅ cinematográfico |
| Fim do tutorial (CEL_019) | ✅ não cortada (determinístico) |
| Início do Setor A (CEL_020) | ✅ toca depois, enfileirada |
| Porta sem interagir | ✅ arromba (dano + abre), sem softlock |
| Terminal de lane lateral | ✅ funil ao centro + acesso |
| Console | ✅ sem erros novos (só avisos de color primaries dos vídeos) |

## Arquivos
Scripts: `TutorialArrowIndicator.cs` (reescrito), `TutorialManager.cs` (CEL_019 Cutscene),
`PlayerRunner.cs` (lane lock), `SoftlockDoorGuard.cs` (novo), `TerminalApproachFunnel.cs`
(novo). Cena: card de interação (padding/autosize), Terminal Approach Funnel @ z2635,
trigger do terminal alargado, SoftlockDoorGuard no Game Systems.
Capturas: `r14_e_clean.png`, `r14_space_clean.png`.

## Preservado
Sistema de interrupções de dublagem (só a prioridade de CEL_019 mudou), HUD, tutorial e
gameplay livre — intocados.
