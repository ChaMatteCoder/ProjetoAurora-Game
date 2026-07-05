# Gameplay Tuning — Relatório Final (Round 4)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity` · Console: 0 erros · Missing scripts: 0
Doc de apoio: `GameplayTuning_Round4_Audit.md`

## Status: ✅ APROVADO para build beta

## Ajustes de balanceamento
- Gradiente de densidade confirmado saudável (32u→17u→26u por obstáculo, S0→S4→S5) — **nenhum reespaçamento necessário**; a varredura de timing encontrou **0 sequências de troca de faixa forçada com t<1.5s**.
- **z760 limpo**: removido o laser legado redundante (invisível, pulável) que coexistia com o `LaserGate_Challenge_01` — era a única ambiguidade real de leitura. Movido para `Legacy_TutorialPlaceholders_Disabled` (desativado, não apagado). Agora só os 3 feixes visíveis do gate.
- One-true-paths reconfirmados justos e claros (z873=C, z1685=R, z2555=C, todos com setas de faixa segura).
- Terminal final permanece acessível (validado: interação → FinalCutscene).

## Interações
- **Sistema de prioridade de mensagens** (novo no `DialogueManager`): `PriorityLow=0` (painel/recuperação), `PriorityDamage=1` (dano), `PriorityStory=2` (narrativa/intro/final). Mensagens de prioridade menor **enfileiram** em vez de cortar uma sequência protegida em andamento.
  - Validado em teste síncrono: painel(low) e dano(med) **não interrompem** narrativa; narrativa pode substituir narrativa. ✓
- Painéis de laser (`Painel de lasers` z735) religados aos 3 feixes reais do gate; `targetLaser` legado removido.
- Prompts E: alcance justo (trigger 9×4×6, some fora de alcance) — sem alteração necessária.

## SFX
- **Pendente (sem assets no projeto)**: só existem 3 áudios (Aurora.mp3, GameOver.mp3, Falha de Contenção.mp3), todos música/ambiente. Nenhum SFX de painel/porta/laser.
- Campos serializados **prontos** para receber clips quando existirem:
  - `InteractableObject.interactSfx` (painel/porta/laser/barreira)
  - `SuitIntegrityRecovery.recoveryStartSfx` / `recoveryCompleteSfx`
- A lógica de disparo já está no código (toca se `clip != null`) — basta arrastar clips no Inspector.

## Suit Recovery tuning
- `recoveryDelay` 60s → **45s** (mais perceptível numa run de ~225s sem trivializar); `recoveryDurationPerSegment` mantido em 10s; cancelamento ao tomar dano preservado (revalidado na R3).

## Problemas corrigidos
1. Laser legado invisível/ambíguo em z760 → removido.
2. Mensagens de painel cortando narrativa → sistema de prioridade.
3. Suit Recovery pouco perceptível → 45s.
4. `LaserHazard.SetColor` não apagava emissão (R3) → já corrigido; revalidado.

## Build check
- Build Settings: `[0] MainMenu`, `[1] Beta03_Principal`, ambos ON ✓ (inalterados)
- Cena abre sem erros, 0 missing scripts, `isDirty=false` (salva) ✓
- Build de player NÃO foi gerada (processo longo; não exigido — "se ambiente permitir"). Projeto está **pronto para build manual** via File > Build.

## Pendências para beta
- SFX de interação/recuperação (campos prontos, faltam clips).
- Playtest humano de "feel" em S4/Ponte (drivers validam lógica/timing, não sensação).
- 3 `Progressive Cyan Laser` legados com `visual=null` (cosmético, sem impacto).
- Retrato da CelestIA é sprite estático (variante em vídeo existe, opcional).

## Como testar
1. Play via MainMenu → JOGAR (Esc pula intro).
2. Tutorial → gameplay livre; observar densidade crescente por setor.
3. z760: apertar E — 3 feixes apagam, portal permanece.
4. Deixar uma narrativa de setor tocar e apertar E num painel: a fala da narrativa **não é cortada**.
5. Tomar dano, esperar 45s: "RECALIBRANDO TRAJE" + segmento pulsa → restaura; tomar dano no meio cancela.
6. Perder 3 vidas → Game Over; chegar ao terminal → E → cutscene final.
