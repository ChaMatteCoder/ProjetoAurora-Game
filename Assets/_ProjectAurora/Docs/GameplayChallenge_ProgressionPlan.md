# Gameplay Challenge — Plano de Progressão (Round 3)

Velocidade do player: 8→16 u/s (ramp por z). Reação mínima projetada: ≥25u (~1.8–3s). Todos os padrões novos ficam nos vãos de 58u do mapa auditado (midpoints ~29u dos vizinhos), com telegraph visual (warning amarelo no piso + emissivos).

## S1 — Lab (90–450) · FÁCIL — quase intocado
Curated Pass já cumpre a fase inicial. Adição única:
- z419 JUMP CHECK leve (tubo baixo) — reforça pulo antes do slalom S2.

## S2 — Contenção (450–900) · MÉDIO — setor mais reforçado
- z467 SINGLE R (slalom L→R→C com 438/496)
- z525 JUMP CHECK full-width
- z583 DOUBLE C+R → só L livre
- z641 LANE+JUMP: barra baixa sobre L+C (R livre ou pulo)
- z699 SINGLE C alto
- **z760 LASERGATE_CHALLENGE_01** (correção): portal físico + 3 feixes vermelhos REAIS conectados ao Painel z735; E desativa feixes (portal fica); sem E = atravessar custa 1 hit (invuln cobre os 3 feixes)
- z815 LASER LANE: laser visível na faixa L
- z873 ONE TRUE PATH #1 (didático): L+R altos, C livre com setas ciano

## S3 — Máquinas (900–1350) · MÉDIO/DIFÍCIL
- z989 LANE+JUMP: barra sobre C+R (L livre)
- z1105 DOUBLE L+C → só R (após laser R em 1076: troca em 29u, tensão justa)
- z1221 INTERACTION GATE #2 (decisão): lasers sobre L+C + barra de pulo na R; painel em z1196 (lado R) desativa os lasers → escolha: E ou pulo
- z1337 SINGLE C alto (respiro pré-S4)

## S4 — Vermelho (1350–1800) · DIFÍCIL
- z1453 DOUBLE C+L → só R
- z1511 JUMP full-width (entre laser 1482 e dupla 1540)
- z1569 LASER LANE R (vermelho)
- z1685 ONE TRUE PATH #2: L+C altos, R livre com setas vermelhas (flui da dupla 1656)
- z1743 LANE+JUMP: barra sobre L+C (R livre)
- **z1801 LASERGATE_CHALLENGE_02** (vermelho): painel z1776; sem E = 1 hit

## Ponte (1800–2250) · DIFÍCIL+
- z1917 DOUBLE L+R altos → só C (encadeia com 1888 laser-C+low-R e 1946 robô-R+tall-L: weaving em C sob pressão)
(setor já denso a 29u — uma adição basta)

## Terminal (2250–2700) · TENSÃO FINAL
- z2555 FINAL PRESSURE / ONE TRUE PATH #3: L+R altos vermelhos, C livre → funil dramático para o terminal
- z2570+ LIVRE até o terminal (acesso garantido, regra de aceite)

## Compensação — Suit Recovery
- 60s sem dano → segmento recarrega em 10s (1 por vez, delay reinicia por segmento)
- Cancela ao tomar dano; só em GameState.Playing
- HUD: segmento em recarga pulsa ciano com preenchimento gradual + label "RECALIBRANDO TRAJE"

## Regras de justiça aplicadas
- Nenhum padrão novo a <25u de vizinho conflitante; duplas sempre com faixa livre coerente com o padrão anterior/seguinte
- One-true-path: 3 usos no total (873 didático, 1685, 2555), sempre sinalizado com setas emissivas no chão
- Lasers novos SEMPRE visíveis (feixes emissivos) + postes
- Gates com E têm fallback: dano único (invuln) ou rota de pulo — zero softlock
