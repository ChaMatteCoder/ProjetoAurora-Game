# Enemy Robots — Performance Polish (Round 7)

Data: 2026-07-03

## Aplicado
| Medida | Antes | Depois |
|---|---|---|
| Materiais do corpo | 18 distintos por robô (state changes) | **1 compartilhado** (`MAT_EnemyRobot_DarkMetal`) — SRP batching pleno |
| Texturas | 18 × até 2K, tipo Sprite | **Default, max 1024** |
| Sombras | SMRs projetando | **Off em todos os SMRs do prefab** (emissivos idem) |
| updateWhenOffscreen | default | **false** (skinning só quando visível) |
| Animator (obstáculos) | sempre ativo | **CullCompletely** + **desligado pelo mover** fora da janela do player (ativa a −70, desliga a +20) |
| Animator (perseguidores) | AlwaysAnimate | lead **AlwaysAnimate** (visível), secundários **CullUpdateTransforms** |
| Perseguidores ativos | 3 | 3 (lead + 2 no fog) |
| Física | — | segue zero Rigidbody/NavMesh; colliders só de trigger nos obstáculos |
| Luzes | — | zero luz realtime por robô (emissivos fazem o papel) |

## Medição em play
- Durante a perseguição na Sala de Máquinas: **4 Animators ativos no total** (3 perseguidores + 1 obstáculo dentro da janela) — antes, todos os 6 obstáculos + perseguidores animavam permanentemente.
- Movimento dos perseguidores permanece centralizado no `RobotPursuitDirector` (1 Update); movers dos obstáculos são O(1) por frame e dormem fora da janela.

## Não aplicado (deliberado)
- LOD: o modelo não possui LODs; gerar exigiria decimação de malha (fora de escopo).
- Pooling: spawn é único por run (3 instâncias) — desnecessário.
