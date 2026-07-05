# Enemy Robots — Substituição de Obstáculos (Round 6)

Data: 2026-07-02

## Primitivos substituídos (visual novo, função intacta)
Método: filhos primitivos movidos para `Gameplay_Round6_Pursuit/Legacy_RobotPrimitiveObstacles_Disabled` (desativados, nada apagado); instância de `PF_AuroraEnemyRobot` como novo visual (rotY=180 — encara o player); **`Obstacle` + BoxCollider trigger do root preservados** (dano idêntico); componente `EnemyRobotObstacle` adicionado; colliders do prefab desligados.

| Objeto | z | Faixa | Setor | Animator speed |
|---|---|---|---|---|
| Security Robot (Real) | 902 | R | Sala de Máquinas | 0.85 |
| Security Robot (Real) | 1424 | R | Corredor Vermelho | 0.85 |
| Security Robot (Real) | 1946 | R | Ponte Técnica | 0.85 |
| Security Robot (Real) | 2468 | R | Terminal (aprox.) | 0.85 |

Nota: os 4 Security Robots são ATIVADOS pela narrativa (`NarrativeEventManager.ActivateRobots` no evento da Sala de Máquinas) — comportamento preservado (busca por nome "Security Robot" continua batendo).

## Convertidos de blocos para robôs (novos robôs-obstáculo)
| Antes | Agora | z | Faixa | Setor |
|---|---|---|---|---|
| TallBlock_C_1105 | RobotObstacle_C_1105 | 1105 | C | Sala de Máquinas (dupla L+C, R livre) |
| TallBlock_L_1685 | RobotObstacle_L_1685 | 1685 | L | Corredor Vermelho — **one-true-path #2** (robô guarda uma das faixas bloqueadas) |

Total: **6 robôs-obstáculo reais** distribuídos por Sala de Máquinas (2), Corredor Vermelho (2), Ponte Técnica (1) e aproximação do Terminal (1) — prioridade do briefing atendida. Dano/telegraph/faixas/one-true-path do balanceamento R3/R4 inalterados (mesmos colliders, mesmas posições).
