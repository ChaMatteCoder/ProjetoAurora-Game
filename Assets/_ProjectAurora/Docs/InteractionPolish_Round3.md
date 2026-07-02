# Interações com E — Polish (Round 3)

Data: 2026-07-02 · Script estendido: `InteractableObject.cs` (retrocompatível — nenhum wiring antigo quebra)

## Extensões no InteractableObject
- `targetLasers[]`: DisableLaser agora desativa múltiplos feixes (gates multi-laser).
- `slideTarget/slideOffset/slideDuration`: OpenDoor com slideTarget **desliza** o alvo (coroutine SmoothStep) em vez de `SetActive(false)` — portas abrem de verdade.
- `statusIndicators[] + statusOffColor`: renderers de luz de status apagam ao interagir (base + emissão).
- `interactSfx`: AudioSource opcional tocado no Interact (nenhum clip de SFX específico existe no projeto; campo pronto para receber).

## Correção no LaserHazard.SetColor
Agora percorre `GetComponentsInChildren<Renderer>` do `visual` e atualiza também `_EmissionColor` — antes, um laser emissivo desativado **continuava brilhando** (collider off + visual aceso = sinalização injusta). Validado: feixes apagam de verdade.

## Interações na corrida (de 2 → 6)
| z painel | Ação | Efeito | Mensagem CelestIA | Fallback sem E |
|---|---|---|---|---|
| 88 | TutorialPanel | Porta do tutorial abre | (tutorial) | gate do tutorial exige E |
| 505 | OpenDoor+slide | **Containment Door (z520) desliza p/ cima e permanece** (antes: sumia) | "Acesso liberado." | porta é cenográfica (sem collider) — decorativa/imersiva |
| 735 | DisableLaser | **LaserGate_Challenge_01 (z760): 3 feixes reais + laser legado desativam; portal fica; luzes de status apagam** | "Emissores desativados." | atravessar custa 1 hit (invuln cobre os 3 feixes) |
| 1196 | DisableLaser | Lasers das faixas L+C (z1221) desativam | "Rota recalculada." | pular a barra baixa na faixa R |
| 1428 | OpenDoor+slide | Barreira C (z1453) é **deslocada** para cima | "Barreira deslocada." | faixa R livre |
| 1776 | DisableLaser | LaserGate_Challenge_02 (z1801, vermelho): 3 feixes desativam | "Emissores desativados." | atravessar custa 1 hit |
| 2660 | FinalTerminal | Cutscene final | — | — |

- Todos os painéis: trigger 9×4×6 (center.y=0.5) — alcance justo em qualquer faixa, prompt some fora do alcance (PlayerInteraction existente).
- Consoles visuais idênticos ao padrão do tutorial (pedestal + tela emissiva).
- Zero softlock: todo gate tem rota alternativa (pulo/faixa livre) ou custo de 1 hit com invulnerabilidade.

## Validado em play (driver)
- Painel 735 → 3 feixes `isActive=false` + damageColliders off + estrutura ativa + luzes de status apagadas + feixes visualmente escuros.
- Painel 505 → porta subiu de y2.2 para y6.8 e **permanece ativa**.
- Painel 1428 → barreira C subiu para y3.4 (faixa liberada).
