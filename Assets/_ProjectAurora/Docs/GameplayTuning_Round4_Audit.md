# Gameplay Tuning — Auditoria de Feel (Round 4)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity` · Console: 0 erros · Método: análise por dados (colliders reais + curva de velocidade 8→16 u/s) + drivers em play

## Densidade por setor (gradiente confirmado bom)
| Setor | z | bloqueios | espaçamento médio | leitura |
|---|---|---|---|---|
| 0 Lab | 0–450 | 14 | 32u | fácil ✓ |
| 1 Contenção | 450–900 | 20 | 23u | médio ✓ |
| 2 Máquinas | 900–1350 | 20 | 23u | médio ✓ |
| 3 Vermelho | 1350–1800 | 21 | 21u | médio/difícil ✓ |
| 4 Ponte | 1800–2250 | 26 | 17u | difícil (mais denso) ✓ |
| 5 Terminal | 2250–2700 | 17 | 26u | afrouxa p/ acesso ao terminal ✓ |

Curva de dificuldade sobe monotonicamente e relaxa no fim — exatamente o pretendido.

## Timing / colisões injustas
- **0 sequências de troca de faixa forçada com t<1.5s** (varredura de todas as 83 linhas de obstáculo com faixa-segura comum): mecanicamente justo, sem combos impossíveis.
- One-true-paths corretos: z873 livre=C, z1685 livre=R, z2555 livre=C — todos com setas de faixa segura.
- Gate de decisão z1221: lasers L+C (E desativa) OU pulo na barra R — fallback válido ✓.
- Gates z760/z1801: fallback = E ou 1 hit com invulnerabilidade ✓.

## Problemas encontrados
1. **z760 — laser legado redundante**: `Gameplay Objects/Laser Hazard` (w8.5, invisível, renderer off, y1.0–1.2 pulável) coexiste com `LaserGate_Challenge_01` (3 feixes visíveis não-puláveis). Ambíguo: parece pulável mas o gate não é. → desativar o legado (o gate o substitui; painel z735 já aponta para os 3 feixes reais).
2. **Mensagens se atropelam (grave)**: painéis/dano/recovery chamam `ShowTemporary` → `Play(interrupt:true)`, que **corta sequências narrativas** da `NarrativeEventManager` (multi-linha, disparadas a cada setor) no meio. → sistema de prioridade.
3. **Suit Recovery 60s**: run completa ~225s (2700u @ ~12u/s). 60s é perceptível mas o jogador dificilmente vê 2 recargas. → 45s (mais legível sem trivializar).
4. **SFX**: só 3 áudios no projeto (Aurora.mp3, GameOver.mp3, Falha de Contenção.mp3) — todos música. Sem SFX de painel/porta/laser → pendência (campos serializados prontos).

## OK (sem ação)
- Build Settings: MainMenu=[0], Beta03_Principal=[1], ambos ON ✓
- Prompts E: trigger 9×4×6 center.y=0.5, `PlayerInteraction.RefreshPrompt` some fora de alcance ✓
- LaserGate desativa lasers (não a estrutura) ✓ — validado R3
- Telegraph: warning amarelo + emissivos + setas nos OTP presentes ✓
