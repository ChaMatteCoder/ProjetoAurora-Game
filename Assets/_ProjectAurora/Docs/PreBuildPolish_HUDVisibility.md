# Pre-Build Polish — Visibilidade da HUD e Card de Personagem (Round 11)

## Estados de visibilidade

`GameplayHudVisibilityState` (em AuroraGameplayHUDController.cs):
`IntroCinematic · Tutorial · Gameplay · Paused · GameOver · Final`

O `GameManager.SetState` dirige a HUD em toda transição de `GameState` (única fonte):
IntroCutscene→IntroCinematic, Tutorial→Tutorial, Playing→Gameplay, Paused→Paused,
GameOver→GameOver, FinalCutscene/Finished→Final.

| Estado | Setor/Integridade/Distância | Card de fala | Hint "ESC — Pular abertura" |
|---|---|---|---|
| IntroCinematic | OCULTOS (alpha 0, aplicado no frame 0 via Awake) | só com fala ativa | VISÍVEL (canto inferior esquerdo) |
| Tutorial | OCULTOS (decisão: tutorial diegético; não há dano no tutorial) | só com fala ativa | oculto |
| Gameplay | visíveis (fade in 0.45s ao liberar o runner) | só com fala ativa | oculto |
| Paused/GameOver/Final | mantidos (as telas existentes cobrem a HUD) | idem | oculto |

Implementação: CanvasGroups adicionados EM RUNTIME (Awake) aos blocos
"Sector Identification" / "Integrity System" / "Distance System" — zero mudança estrutural
na cena serializada. O hint é criado em runtime (TMP, 17pt, ciano-branco alpha 0.62).
Texto usa "ESC" (não "ESQ") para não confundir com esquerda.

## Card de personagem só durante fala ativa

- O painel "CelestIA Communication" ganhou CanvasGroup (alpha-only — o pool de
  VideoPlayers do retrato vive sob ele e NUNCA pode ser desativado).
- **Mostrar**: `SetVoiceLine` (qualquer falante) e `SetDialogue` com mensagem não-vazia.
- **Esconder** (fade 0.25s após delay 0.35s — nova fala dentro do delay cancela):
  - fim natural de fala: `VoiceLinePlayer.FinishRequest` agora notifica
    `EndVoiceLine` para QUALQUER falante (antes: só Dr. Elias — o subtítulo ficava fixo);
  - cancelamento: `ClearVoiceLine`;
  - fim de sequência do DialogueManager (fila vazia) e `StopAll`;
  - `SetDialogue` com mensagem vazia.
- `ShowPersistent` (usado só no preview do terminal) mantém o card por design.

## Card do Dr. Elias por ID (bug crítico corrigido)

Causas e correções:
1. **Corrida de Start**: `HudCharacterVideoPortraitController.Start()` chamava
   `ShowCelestIANormal()` incondicionalmente e sobrescrevia o Dr. Elias pedido por
   ELI_001 no mesmo frame (a intro toca no `GameManager.Start`). Agora o Start só
   aplica a CelestIA se nenhum falante foi pedido antes (`activeSlot`/`pendingSlot` nulos).
2. **Timer que cortava a fala**: caminho por ID usa `ShowDrEliasForVoiceLine(mood)` —
   segura o retrato até `EndVoiceLine` (sem o hold de 3,5s). O caminho legado por texto
   (`SetSpeakerFromDialogue`) mantém o timer como fallback.
3. **Estado da CelestIA sobrescrevia a identidade**: `SetCelestIAState` (disparado pelo
   SectorManager a cada mudança) escrevia nome/STATUS/accent do painel por cima do
   Dr. Elias. Agora só aplica no painel quando o retrato NÃO está no Dr. Elias.
- Fonte do speaker: `VoiceLineEntry.speaker` (metadata do VoiceLineDatabase) — nunca
  inferência por texto quando há ID. `drEliasMood` do banco controla normal/nervoso e o
  status "BIOSINAL: ESTÁVEL"/"BIOSINAL: ELEVADO".

## Validado em play ([R11V]/[R11W])
- Intro: setor/integridade/distância alpha 0.00; hint visível; ELI_001 →
  portrait DrElias, nome "DR. ELIAS", status "BIOSINAL: ESTÁVEL", card alpha 1.
- t+8s (fala encerrada): retrato voltou à CelestIA.
- Tutorial: HUD segue oculta; hint some.
- Playing: HUD alpha 1 com fade; card alpha 0.00 medido 1.4s após o fim das falas.
