# Enemy Robots / Chase — Auditoria (Round 6)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity`

## Assets do robô
- **Modelo** `Characters/Enemy/Model/roboot.fbx`: rig **Generic**, **18 MeshRenderers estáticos** (`tripo_part_0..17`, Tripo AI), **0 SkinnedMeshRenderer**, sem Animator/Avatar. Bounds 0.21×0.98×0.98 (altura 0.98 → escalar ~1.9× para ~1.85m, coerente com Dr. Elias). Texturas basecolor por parte já no fbx.
- **Animação** `Characters/Enemy/Animation/Walking.fbx`: rig **Generic**, clip `mixamo.com` 1.6s, 66 transforms (esqueleto Mixamo humanoide), **0 skinned mesh** (animação pura, sem malha visível).

### ⚠ Incompatibilidade técnica (decisão de arquitetura)
O modelo é um mesh **estático não-riggado**; a Walking.fbx é uma animação **esquelética Mixamo humanoide**. Não é possível aplicar a animação Mixamo ao robô (sem ossos correspondentes, sem skinned mesh). Forçar retarget falharia.
**Solução:** animação **mecânica procedural** no transform (passada/bob vertical, balanço de pitch/yaw, núcleo emissivo pulsante) via `EnemyRobotProceduralAnimator`. Reads perfeitamente para inimigos de runner vistos de trás em fog. O `EnemyRobot_Animator.controller` é criado com o clip Walking em loop (cumpre "configurado/looping" literalmente), mas a motion visível é procedural. Documentado.

## Robôs primitivos na cena (a substituir)
4× `Gameplay Objects/Security Robot` — cada um com `Obstacle` + BoxCollider (trigger), 3 filhos primitivos, faixa R (x=3):
- z=902 (Sala de Máquinas), z=1424 (Corredor Vermelho), z=1946 (Ponte Técnica), z=2468 (perto do Terminal).
Preservar: posição, faixa, Z, `Obstacle`, collider, função de dano. Trocar só o visual pelo modelo real. Antigos → `Legacy_RobotPrimitiveObstacles_Disabled`.

## Setores (SectorManager sectorLength=450, finishDistance=2700)
- **Sala de Máquinas** = setor 2 = z900–1350 → **início da perseguição** em z~905.
- **Corredor do Terminal** → **fim da perseguição** em z~2560 (antes do Terminal Central Access em z~2660); porta fecha atrás.

## Sistemas reutilizáveis
- `PlayerRunner` (lane/jump — sem API pública de lane, mas posição X/Y refletem tudo), `PlayerHealth`, `GameManager.Distance/State`.
- Câmera: `Main Camera` + `CameraFollow` (disable/lerp/restore, padrão já usado em `IntroCutsceneController`).
- Mensagens: `GameManager.celestIA.ShowTemporary(msg, dur, priority)` com prioridade (Round 4) — perseguição usa PriorityStory para não ser cortada por painel.
- `Obstacle`/`LaserHazard` para dano dos robôs-obstáculo.

## Estratégia da perseguição (visual, sem NavMesh/física)
- `RobotPursuitDirector` grava histórico (tempo, posição mundial) do player num ring buffer durante a perseguição.
- Cada perseguidor amostra a posição do player **D segundos atrás** (delay 0.45–1.1s) + offset Z para trás + offset X/Y de formação. Como replica o caminho que o player já percorreu (livre de colisão), reproduz mudança de faixa e pulo com atraso, sem colliders (não colide, não bloqueia, não dá dano).
- Para no corredor do terminal; porta fecha atrás; cutscene de bloqueio; robôs desativam.

## Riscos
- Animação apenas procedural (sem deformação esquelética) — aceitável para inimigos distantes/fog; documentado.
- Perseguidores entre câmera e player podem obstruir visão → formação nas laterais + 1 ao fundo, distância controlada.
- Performance: 18 meshes × N robôs; limitar a 3–4 perseguidores + sombras off; sem Rigidbody/NavMesh; motion centralizada.
