# Pre-Build Polish — Auditoria (Round 11)

## Bugs encontrados

### B1 — Card não mostra Dr. Elias na primeira fala (ELI_001) — CRÍTICO
Causa raiz (dupla):
1. **Corrida de inicialização**: a intro começa em `GameManager.Start()` (frame 0) e `VoiceLinePlayer`
   já chama `ui.SetVoiceLine(ELI_001)` → retrato pede Dr. Elias. Mas
   `HudCharacterVideoPortraitController.Start()` (ordem arbitrária no mesmo frame) chama
   `ShowCelestIANormal()` DEPOIS, sobrescrevendo o pedido (o slot pendente do Elias é trocado
   pelo da CelestIA). Resultado: ELI_001 exibe CelestIA.
2. **Timer de retorno**: `EliasReturnRoutine` devolve o retrato à CelestIA após 3,5s fixos,
   mesmo com a fala do Elias ainda em andamento (clipe > 3,5s corta o card no meio).
Nota: `SetVoiceLine` JÁ roteia por `entry.speaker` (metadata do VoiceLineDatabase é a fonte) —
o problema é ser sobrescrito, não inferência. A inferência por texto (`SetSpeakerFromDialogue`)
existe apenas no caminho legado `SetDialogue` (sem ID) e será mantida SÓ como fallback.

### B2 — HUD completa visível durante a abertura — ALTO
`Sector Identification`, `Integrity System` e `Distance System` ficam ativos desde o load.
Não existe mecanismo de visibilidade por estado. `GameManager.Update` ainda chama
`ui.SetDistance` durante a intro.

### B3 — Card de personagem nunca some — ALTO
- `VoiceLinePlayer.FinishRequest` NÃO limpa a HUD em término natural (só `CancelCurrent` limpa)
  → o último subtítulo fica fixo para sempre.
- `DialogueManager.PlayRoutine` também termina sem limpar.
- O painel `CelestIA Communication` não tem CanvasGroup (nem fade).

### B4 — Tutorial permite cortar todas as falas — ALTO
`ActivateStep` libera `CurrentAllowedAction` NO MESMO instante em que toca a fala principal.
Jogador que aperta D/A/Espaço imediatamente completa a etapa → `StopTutorialVoice()` corta a
fala → em cadeia, o tutorial inteiro fica mudo. Não há setas indicativas.
**Fato favorável**: `ActivateStep` já faz `player.SetAutoRun(false)` — o runner PARA no trigger.
Bloquear a ação durante a fala não faz o player passar do obstáculo (ele está parado) →
a "Safe hold zone" (Opção C) já existe estruturalmente; falta apenas o gating por fim de fala.

### B5 — Portas com abertura não convincente — MÉDIO
- `TutorialPanel_Console` (z98) → `Door_Slab` some via `SetActive(false)` (porta desaparece).
- `Painel de porta` (z505) desliza a ESTRUTURA INTEIRA "Containment Door" +4,2 em Y
  (moldura, pilares e header sobem juntos = "bloco atravessando tudo").
- `Interactable_Door_01` (z150, DoorInteractable) sobe o bloco 4m verticalmente.
- Sem trilhos, sem luzes de status, sem ease consistente, sem painéis divididos.

### B6 — Sem portas de transição entre setores / sem overlay — MÉDIO
Fronteiras em z=450/900/1350/1800/2250 não têm portas; mudança de setor só troca o texto
pequeno no topo. `HUD Canvas/Sector Card` (com CanvasGroup) existe mas `ui.sectorCard` está
NULO — o card legado nunca aparece.

### B7 — ESC sem indicação na intro — BAIXO
ESC já pula a intro (`IntroCutsceneController.Update`), mas nada informa o jogador.
`GameManager.TogglePause` já ignora ESC em IntroCutscene/Tutorial (sem conflito).

## Sistemas afetados
VoiceLinePlayer (1 edit mínimo), HudCharacterVideoPortraitController, AuroraGameplayHUDController,
UIManager, DialogueManager, GameManager, TutorialManager, InteractableObject, DoorInteractable,
SectorManager + novos: TutorialArrowIndicator, AuroraDoorController, AuroraSectorDoorTrigger,
SectorTitleOverlayController.

## Riscos e mitigação
- **Dublagem (integração do usuário)**: edits em VoiceLinePlayer limitados a FinishRequest
  (sempre notificar fim de linha à HUD, não só p/ Elias). Fila/prioridades intocadas.
- **Vídeo pool do retrato**: NUNCA desativar GameObjects do painel de comunicação
  (os VP_* vivem lá) — fade só por CanvasGroup.alpha.
- **Tutorial softlock**: gating usa polling de `IsPlayingGroup(Tutorial)` + timeout de segurança
  (fala ausente/cancelada libera a ação após fallback) — nunca prende o jogador.
- **Porta z1800**: LaserGate_Challenge_02 está em z1801 (desafio E). Porta de transição
  Vermelho→Ponte vai para z~1830 (depois do gate) para não sobrepor o desafio.
- **Porta z900**: início da perseguição é z905 (robô em z902). Porta Contenção→Máquinas
  vai para z888, abrindo com antecedência, sem interferir na cutscene.
- **Robôs**: portas de transição abrem para o player e ficam abertas (openOnce) — perseguidores
  (replay visual) não são bloqueados; a porta da perseguição no Terminal (z2566) permanece a única
  que fecha atrás.

## Plano de correção (ordem)
1. Código: portrait fixes + estados de HUD + fade do card + limpeza de fim de fala.
2. Código: tutorial gating por fala (TutorialStepState) + setas + prompts pós-fala.
3. Código: AuroraDoorController/Trigger + SectorTitleOverlay + SectorManager estruturado.
4. Cena: CanvasGroups, hint ESC, overlay, retrofit das 3 portas existentes,
   prefab PF_AuroraSectorDoor + 5 portas de transição (z452/888/1351/1830/2250), setas.
5. QA em play: fluxo completo menu→terminal + regressões (perseguição, recovery, GameOver).

## Posições confirmadas na cena
- Tutorial: steps z14(D)/z38(A)/z62(pulo)/z78(pulo)/z98(E→Door_Slab z106).
- Janelas livres nas fronteiras: 452 (obst. 438/467), 888 (robô 902), 1351 (obst. 1337/1366),
  1830 (gate laser 1801 — verificar 1815-1845 no build), 2250 (barreiras 2236/2265).
