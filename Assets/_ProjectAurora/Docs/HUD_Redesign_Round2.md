# HUD da Gameplay — Fidelidade à Referência (Round 2)

Data: 2026-07-02 · Referência: `Assets/_ProjectAurora/Art/UI/../References/GameplayHUD_Ref.png` (imagem anexada pelo usuário, presente no projeto em `Assets/_ProjectAurora/Art/References/GameplayHUD_Ref.png`)

## Conclusão da auditoria
A HUD existente (`HUD Canvas` + `AuroraGameplayHUDController`, TextMeshPro) **já implementa fielmente a estrutura da referência** — foi construída a partir dela em iteração anterior e a rodada 1 a preservou. Não foi criada HUD paralela (regra "não duplicar HUD"); esta rodada validou e ajustou.

## Mapeamento referência → cena (verificado em play)
| Referência | Implementação existente | Status |
|---|---|---|
| SETOR A: Laboratório Limpo (topo esq., painel chanfrado ciano) | `Sector Identification` (Top Glow, cantos TL/BR, Sector Name TMP) via `SectorManager.UpdateSector` → `UIManager.SetSector` | ✓ dinâmico (6 setores) |
| ◆ Escape do setor (abaixo) | `Objective` + `Objective Diamond` no mesmo painel | ✓ |
| INTEGRIDADE + 3 escudos (topo centro) | `Integrity System` (label + 3 segments) via `PlayerHealth` → `UIManager.SetLives` → `SetIntegrity(v,3)` | ✓ atualiza ao dano |
| DISTÂNCIA 1.248 m + barra + flag (topo dir.) | `Distance System` (label, value, track com marcador, finish flag) via `GameManager.Update` → `SetDistance(dist, 2700)` | ✓ dinâmico |
| Card CELESTIA (inf. dir.): retrato circular, nome, STATUS: NORMAL, mensagem, waveform | `CelestIA Communication`: Portrait Ring/Mask com sprite **CelestiaNormal** (mesma personagem da referência), nome, status, signal bars, divider, mensagem TMP, 26 barras `Transmission` | ✓ estados Normal/Transition/Corrupted via `SetCelestIAState` |
| Painéis escuros translúcidos, bordas ciano, chanfros | Padrão visual de todos os painéis do canvas | ✓ |

## Configuração validada
- Canvas: Screen Space Overlay, sortingOrder 50
- Canvas Scaler: Scale With Screen Size, **1920×1080, match 0.5** (exatamente o pedido)
- TextMeshPro em todos os textos; UIManager mantém fachada de compatibilidade (campos Text legados nulos)

## Observações
- O retrato é sprite estático (`CelestiaNormal.png`). Existe uma variante com vídeo (`CelestIAHudController` + Celestia01-03.mp4) não usada nesta cena; `SectorManager.celestIAHud=null` é tratado com null-safety. Documentado como possível evolução futura, não pendência.
- Nenhuma HUD antiga duplicada ativa; painéis Pause/GameOver/Final/Intro íntegros.
