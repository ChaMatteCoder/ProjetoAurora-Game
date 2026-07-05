# Enemy Robots / Chase — Implementação (Round 6)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity`

## Rig do robô (atualização pós-auditoria)
O usuário **riggou o modelo** (`roboot.fbx` agora tem 18 SkinnedMeshRenderers + esqueleto Waist/Spine/braços/pernas). Nomes de ossos ≠ Mixamo, então a ponte é o **retarget Humanoid**: ambos os imports foram convertidos para Humanoid com Avatar automático — `robootAvatar: valid=True, human=True` e `WalkingAvatar: valid=True, human=True`. **Animação esquelética real funciona sem retrabalho.** Clip `mixamo.com` (1.6s) com loopTime, lockRootRotation/HeightY, keepOriginalPositionXZ. Root Motion off.
(`EnemyRobotProceduralAnimator` foi escrito como fallback pré-rig e permanece no projeto, não usado no prefab.)

## Assets criados
- `Prefabs/Enemies/PF_AuroraEnemyRobot.prefab`: root + `RobotModel` (escala **1.83** → ~1.85m como o Dr. Elias, base no chão) + Animator (avatar robootAvatar, controller, cullingMode CullUpdateTransforms) + BoxCollider trigger 1.3×1.85×0.7 **desativado por padrão** (robô-obstáculo usa o collider do root existente).
- `Animations/Enemies/EnemyRobot_Animator.controller`: estado único Walking (clip Mixamo em loop). Velocidade por `Animator.speed`: perseguidor 1.7 ("corrida"), obstáculo 0.7–0.85, parado 0.5.

## Scripts (`Scripts/Enemies/`)
- **EnemyPursuitRobot**: perseguidor 100% visual — colliders desligados no Awake, `ApplyTarget()` com suavização exp; campos delay/backOffset/lateralOffset.
- **EnemyRobotObstacle**: marcador do robô-obstáculo (requer `Obstacle` — dano pelo sistema existente), cadência, patrulha lateral opcional (off).
- **RobotPursuitDirector**: o cérebro. Ring buffer de histórico do player (x,y,z,t); cada perseguidor amostra a posição de `delay` s atrás + recuo Z + offset lateral → **replay atrasado**: imita troca de faixa e pulo reproduzindo um caminho comprovadamente livre — jamais colide com obstáculos, sem NavMesh/física. Movimento centralizado (1 Update).

## Sequência de início (Sala de Máquinas, z≥905)
Invulnerabilidade de cutscene (`PlayerHealth.SetExternalInvulnerability`, método novo) → spawn de 3 robôs atrás (formação C/E/D, delays 0.55/0.8/1.05s, recuos 5.5/7.5/7.5) → CelestIA: "Unidades autônomas ativadas. Dr. Elias, corra." → câmera desacopla e olha para trás mostrando os robôs (2.4s) → retorno suave → "Elas estão atrás de você." → perseguição ativa. Sem timeScale=0; auto-run mantido.

## Sequência de fim (corredor do Terminal, z≥2560)
`TerminalContainmentGate` (z2566, criado na cena; slab 8.4×5.2 começa ERGUIDO): slab desce em 1.1s **depois** que o player passa (player ~z2573 no fechamento — sem softlock; slab sem collider ativo) → robôs convergem para trás da porta e desaceleram (animator 1.7→0.5) → câmera mostra porta+robôs → CelestIA: "Acesso ao núcleo isolado." (ou "Contenção restabelecida..." se corrompida) → retorno → `PursuitFinished` → robôs somem após 4s de silhueta.

## Bug encontrado e corrigido na validação
`EndSequence` seta `PursuitActive=false` na 1ª linha, o que **reabilitava o gate de início** → `StartSequence` re-disparava no meio do encerramento, duplicava os robôs e mutava as listas durante o while do fim → `ArgumentOutOfRangeException` → porta nunca fechava. Corrigido com `!endSequenceRunning` no guard de início + loop defensivo (`Mathf.Min` dos Counts).

## Validação final (driver em play)
Perseguição ativa com **3 robôs** · formação toda atrás do player · **porta fechada (slabY=0.00)** · robôs contidos atrás (maxZ=2563.8 < 2566) · terminal → FinalCutscene → tela "FIM DA CONTENÇÃO" (fluxo de vitória completo) · console 0 erros.
