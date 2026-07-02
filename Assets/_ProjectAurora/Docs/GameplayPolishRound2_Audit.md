# Gameplay Polish Round 2 — Auditoria Pós-Remake

Data: 2026-07-01 · Cena: `Beta03_Principal.unity` · Console: 0 erros / 0 warnings

## HUD atual
- `HUD Canvas` (ScreenSpaceOverlay, Scaler 1920×1080 match 0.5, sortOrder 50) com `AuroraGameplayHUDController` (TextMeshPro) + `UIManager` (fachada; campos Text legados = null, delega tudo ao auroraHud).
- Estrutura **já espelha a referência** `Assets/_ProjectAurora/Art/References/GameplayHUD_Ref.png`:
  - Sector Identification (nome + objetivo + losango) — topo esquerdo
  - Integrity System (label + 3 segmentos) — topo central
  - Distance System (label + valor + track de progresso + finish flag) — topo direito
  - CelestIA Communication (Portrait Ring com sprite `CelestiaNormal`, nome, status, signal bars, divider, mensagem, 26 barras de waveform) — inferior direito
  - Interaction Prompt / Sector Card / Pause / Intro / Failure / Final panels
- `CelestIAHudController` (versão com VideoPlayer) **não está na cena** — o retrato é sprite estático; `SectorManager.celestIAHud=null` é tolerado com `?.`. Vídeos Celestia01-03.mp4 existem em `Assets/Videos/CelestIA/` mas não são usados nesta cena.
- Conclusão: HUD não precisa de rebuild — apenas verificação visual fina em play 1920×1080.

## Dr. Elias
- `Dr. Elias - Player/DrElias Visual/DrElias Model` — 36 SkinnedMesh/MeshRenderers (`tripo_part_0..35`), todos com material **embutido no FBX** (`tripo_convert_...fbx`), URP/Lit, metallic=0, **smoothness=0.5**, base branca pura + textura `scientistcharacter3dmodel_basecolor`.
- Causa do prateado: smoothness 0.5 uniforme (specular forte em toda a roupa) sob a iluminação nova + ACES; material embutido não é editável no asset.
- Correção: material único novo compartilhado com a mesma textura e smoothness fosco, remapeado nos 36 slots (sem tocar rig/Animator/colliders).

## Intro atual (`IntroCutsceneController` em Game Systems)
- Um único shot estático 3/4 (`player + (4.5, 2.8, -5)`), dois blocos de diálogo (~15s), alerta vermelho, restore suave de 1s, então `EnterTutorial()`+`BeginTutorial()`.
- **BUG**: `SetAlertLighting()` faz `Lerp(color, red, 0.65)` em TODAS as luzes da cena e **nunca restaura** (só o ambient volta). As cores por setor da rodada 1 (ciano/azul/âmbar/vermelho) são permanentemente contaminadas de vermelho após a intro.
- Diálogo já é pulável por linha (Space/Enter, `DialogueManager.AllowSkip`).

## Tutorial
- **Os obstáculos brancos são primitivas criadas em RUNTIME** por `TutorialManager.EnsureRuntimeSequence()` (cubos coloridos flat: vermelho z22, laranja z46, amarelo z64.8, laranja z80.8, porta azul z96, console ciano z88).
- O método só cria se `FindObjectsByType<TutorialStepTrigger>()` == 0 → **autorar a sequência na cena com visuais reais desativa os placeholders automaticamente, sem alterar script**.
- Legado no spawn: `Gameplay Objects/Tutorial Door` (z8, slab 7.8×2.75 SEM collider — o player atravessa o visual ao correr) e `Tutorial Panel` (z2, inativo). Arranjo antigo de tutorial substituído pela sequência z14–96; devem ser desativados como legado.
- Fluxo/gating OK: TutorialActionGate bloqueia ações fora da etapa; triggers one-shot; painel exige `InteractableAction.TutorialPanel`.

## Interações pós-tutorial (verificar wiring na cena)
- `Painel de porta` (z505) → deve apontar `targetObject=Containment Door` (z520)
- `Painel de lasers` (z735) → deve apontar `targetLaser` (Laser Hazard z760)
- `LaserHazard.Deactivate()` já desabilita `damageCollider` + escurece visual ✓ (script)
- Terminal final: `InteractableAction.FinalTerminal` → `BeginFinalCutscene()` ✓
- Game Over: `PlayerHealth.OnDeath` → `GameOverManager.TriggerGameOver()` ✓

## Ordem de execução da rodada
1. Material do Dr. Elias (cena)
2. Sequência de tutorial autorada + legado desativado (cena) + verificação de wiring
3. Salvar cena
4. Rescrever IntroCutsceneController (script, recompila)
5. Validação em play + polish de HUD
6. Relatórios
