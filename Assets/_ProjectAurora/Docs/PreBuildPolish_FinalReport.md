# Pre-Build Polish — Relatório Final (Round 11)

**Veredito: PRONTO PARA PRIMEIRA BUILD** (com as pendências menores listadas ao fim).

## 1. Bugs corrigidos
- B1 Card errado na primeira fala (ELI_001 mostrava CelestIA) — corrida de Start +
  timer de retorno + sobrescrita por SetCelestIAState. CORRIGIDO (3 causas).
- B2 HUD completa visível na abertura — estados de visibilidade criados. CORRIGIDO.
- B3 Card de personagem nunca sumia (FinishRequest não limpava; DialogueManager idem).
  CORRIGIDO com fade 0,35s+0,25s.
- B4 Tutorial cortável em cadeia — gating por fim de fala + setas. CORRIGIDO.
- B5 Portas abrindo como bloco/atravessando cenário — retrofit + padrão Aurora. CORRIGIDO.
- B6 Sem portas de transição/overlay de setor — criados. CORRIGIDO.
- B7 ESC sem indicação — hint discreto na intro. CORRIGIDO.

## 2. Card do Dr. Elias
Fonte primária: `VoiceLineEntry.speaker`/`drEliasMood` do VoiceLineDatabase (por ID).
ELI_XXX → retrato/vídeo do Dr. Elias + "DR. ELIAS" + "BIOSINAL: ESTÁVEL|ELEVADO" +
accent âmbar; CEL_XXX → CelestIA no estado visual atual. Retrato segura até o fim real
da linha (EndVoiceLine); inferência por texto só sobrevive como fallback sem ID.
Validado: ELI_001 exibiu DR. ELIAS/BIOSINAL na intro; retorno à CelestIA após a fala.

## 3. Visibilidade da HUD
`GameplayHudVisibilityState` dirigido pelo GameManager. Intro: setor/integridade/
distância ocultos + "ESC — Pular abertura" no canto inferior esquerdo. Tutorial:
diegético (HUD oculta — sem dano no tutorial). Gameplay: HUD completa com fade.
Detalhes: PreBuildPolish_HUDVisibility.md.

## 4. Tutorial (liberação pós-fala)
`TutorialStepState` Waiting→ActionEnabled→Completed; ação bloqueada durante a instrução;
runner PARADO no trigger (Opção C — safe hold estrutural, sem timeScale); watchdog
(12s teto, 2,6s fallback sem áudio) garante que o player nunca fica preso; lembretes só
após liberar. Detalhes: PreBuildPolish_TutorialGating.md.

## 5. Setas animadas
`TutorialArrowIndicator`: chevrons ciano emissivos no mundo com pulso+deslize direcional,
"ESPAÇO"/"E" em TMP 3D billboard; aparecem SÓ na liberação, somem na execução.
Validado nas 5 etapas.

## 6. Portas melhoradas
`AuroraDoorController` (painéis deslizam na moldura, curve/ease, collider desativa a 45%,
luzes vermelho→verde). Retrofit: porta do tutorial (z106), Containment Door (z520 — a
estrutura não sobe mais; folhas novas fecham o vão) e Interactable_Door_01 (z150).
Integração por detecção nos scripts existentes (fallback preservado).

## 7. Portas de transição
PF_AuroraSectorDoor + 5 instâncias: z452 / z888 / z1351 / z1817 / z2250 (posições
escolhidas pelas janelas livres reais; 1817 fica após o desafio de laser z1801; 888 antes
da perseguição z905). Abrem 30m antes, bloqueiam até abrir, ficam abertas (robôs nunca
presos). Detalhes: PreBuildPolish_DoorsAndSectors.md.

## 8. Overlay de setor
`SectorTitleOverlayController`: SETOR A..E/NÚCLEO + subtítulo, ciano/vermelho (corrompido),
0,35/2,0/0,45s, 1× por setor, nunca na intro, sem bloquear input.

## 9. Testes executados (drivers [R11V] run1 + [R11W] run2)
1-2. MainMenu→Jogar→intro na sala do Dr. Elias ✅
3. ELI_001 com card do Dr. Elias (nome+BIOSINAL+retrato) ✅
4. Setor/integridade/distância alpha 0,00 na intro ✅
5-6. Hint ESC visível; skip pela mesma flag do ESC funcionou ✅
7. Card alpha 0,00 medido 1,4s após o fim das falas ✅
8-10. 5/5 etapas: ações bloqueadas na fala (4-6s reais de dublagem), seta na liberação ✅
11. Player parado durante bloqueio (safe hold) — nenhum atravessamento ✅
12. Tutorial completou e liberou a corrida ✅
13. Porta do tutorial abriu deslizando pelo painel ✅
14. 5/5 portas de transição abriram na aproximação e foram atravessadas ✅
    (+ porta fechada retém o player de verdade — prova de bloqueio ✅)
15. Overlays de setor dispararam (índices 1→5 + SETOR A no início) ✅
16-17. Gameplay contínua até o Núcleo; perseguição com 3 robôs ativa ✅
18. Suit Recovery: fluxo de dano/integridade inalterado (caminhos não tocados) ✅
19. Game Over disparou e apresentou tela ✅
20. Terminal final → FinalCutscene iniciada (CEL_044 tocando) ✅
21. Console sem erros novos (só avisos pré-existentes de color primaries dos vídeos) ✅
22. Cena salva, 0 missing scripts ✅

## 10. Pendências para primeira build
- SFX de porta: campos prontos no AuroraDoorController; nenhum clipe de porta no projeto
  (adicionar asset e arrastar em openClip).
- Avisos "Color primaries" dos MP4 (WMF): cosméticos; re-exportar vídeos com BT.709
  resolve quando conveniente.
- ESC durante subpainel do pause retoma direto (limitação aceita do Round 10).
- Build settings: conferir apenas MainMenu + Beta03_Principal na lista final de cenas.
