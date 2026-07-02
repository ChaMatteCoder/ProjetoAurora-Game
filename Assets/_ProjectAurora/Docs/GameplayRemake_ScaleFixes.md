# Gameplay Remake — Correções de Escala e Física

Data: 2026-07-01 · Cena: `Beta03_Principal.unity` · Referência humana: Dr. Elias (`CharacterController` ~2,05u)

Este relatório complementa `ScaleAudit_Beta03.md` (auditoria original com a lista completa de problemas). Resumo do que foi feito no remake:

## Aplicado nesta sessão
| Item | Problema | Correção |
|---|---|---|
| Tutorial Door | 9,0u de largura (excessiva) e flutuando após resize | Visuais → 7,8u de largura; altura 2,75u (> player); raiz apoiada por bounds em Y=0 |
| Sector Labels (×5) | Texto espelhado para o jogador (rotY=180) | rotY=0 — legível na aproximação |
| Luzes do Fase05 (×20) | Intensidades 850–2200 (autoradas p/ renderer 2D que as ignorava) estouravam no renderer 3D | Normalizadas para 4–20 |
| Câmera | farClip alto sem necessidade (fog termina em 285) | farClipPlane = 500 |

## Verificado por bounds (já corrigido em sessão anterior, confirmado OK)
- Terminal Entry Gate: apoiado em Y=0
- Curated Obstacle Pass: Low Cargo ×5, Tall Containment ×4, Laser ×3 — visuais apoiados
- Fase01 - Detailed Obstacles: 46 visuais — nenhum enterrado
- Terminal Set Dressing: 4 props apoiados
- Painéis interativos (lasers/porta/tutorial): trigger center.y=0,5, size 4×3×5 (alcance generoso da tecla E preservado)
- Containment Door: 9×4,5 transversal às 3 faixas (intencional), apoiada em Y=0

## Diretrizes respeitadas
- Escala global do root e do player intocadas (tudo em (1,1,1))
- Colliders funcionais dos obstáculos não alterados (bounds já corretos em Y=0)
- Novos props decorativos do remake: sem colliders (zero risco de colisão injusta), fora das 3 faixas (|x| ≥ 4,8), pórticos com vão livre ≥ 4,4u de altura e ≥ 5,2u de meia-largura
