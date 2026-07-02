# Gameplay Challenge — Auditoria (Round 3)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity` · Console: 0 erros

## Fontes de dano ativas (mapa por z, pós-tutorial)
- **S1 Lab (90–438)**: Curated Pass — padrões bons: singles, 3 duplas (205 L-tall+C-low; 250 L-low+R-laser; 300 R-low+C-tall; 350 L-laser+R-tall), lasers de pulo (y0.8–1.0). Ritmo 35–50u. **Adequado para fase inicial.**
- **S2 Contenção (438–902)**: SÓ singles a cada 58u (Low Barrier L / Containment C ou R / 1 laser de pulo 670 / laser-gate full-width 760). **Setor mais fácil e monótono — 8 vãos de 58u para popular.**
- **S3 Máquinas (902–1350)**: singles 58u + Progressive intercalado a 29u em parte (931/1047/1163/1279/1308-dupla). Vãos: 989, 1105, 1221, 1337.
- **S4 Vermelho (1350–1800)**: densidade maior, 4 duplas naturais (1424 robô-R+low-L; 1540; 1656; 1772). Vãos: 1453, 1511, 1569, 1685, 1743.
- **Ponte (1800–2250)**: denso a 29u, incl. quase-one-true-path (1946 robô-R + tall-L → só C). Vão: 1917.
- **Terminal (2250–2526)**: denso; dupla tall 2410 C+R. Depois de 2526: livre até o terminal (lead-in é visual).

## Interações E existentes
| z | Painel | Ação atual | Problema |
|---|---|---|---|
| 88 | TutorialPanel_Console | Abre porta do tutorial (slab some com collider) | OK (validado R2) |
| 505 | Painel de porta | `OpenDoor` → **Containment Door (z520) inteira some** | Errado: porta deve abrir, não sumir |
| 735 | Painel de lasers | `DisableLaser` → Laser Hazard z760 | **Feixe z760 é invisível** (renderer desabilitado; só 2 postes visuais) — "porta de lasers sem lasers reais" |
| 2660 | Terminal Central Access | FinalTerminal | OK |

Só 2 interações E na corrida inteira (505, 735) → pouco.

## Sistemas
- `PlayerHealth`: 3 vidas, invuln 2s + slow 1.5s no dano, `IntegrityChanged(int,int)`, `OnDeath`. **Sem método de recuperação** (Lives private set).
- HUD `AuroraGameplayHUDController.SetIntegrity(cur,max)`: segmentos Image com cor ativa/vazia. **Sem estado de "carregando".**
- `InteractableObject`: OpenDoor=SetActive(false); DisableLaser=1 laser só (`targetLaser`); sem SFX; mensagem CelestIA ✓.
- `LaserHazard.Deactivate()`: desliga damageCollider + escurece visual ✓ (3 Progressive Cyan Laser com visual=null — cosmético).
- `Obstacle`: dano por trigger, simples e reutilizável ✓.

## Pontos fracos de desafio
1. S2 inteiro é single-slalom fácil (58u de reação = 4–7s).
2. Zero one-true-path deliberado (só o acidental em 1946).
3. Zero interação E entre z735 e z2660 (~1900u sem E).
4. Laser gate 760: invisível → injusto/ilegível; porta 520 some (feio e irreal).
5. Sem recuperação de integridade → dificuldade extra puniria demais (3 hits em 2700u).
