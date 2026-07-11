# Aurora — Plano de Otimização (Preparação)

**Data:** 2026-07-08  ·  **Assets preparados:** 12  ·  **Status:** todos `Prepared_Not_Optimized` (NADA otimizado ainda)

> ⚠️ **NÃO substituir os originais ainda.** Esta etapa só criou cópias de trabalho.

## Contagem por categoria
- **CRITICAL**: 9
- **VERY_HEAVY**: 0
- **HEAVY**: 3

## Top assets por triângulos
| # | Asset | Tris | Cat | Alvo | Preset sugerido | Em cena | Em prefab |
|---|---|---:|---|---:|---|:--:|:--:|
| 1 | `Aurora_Lazer_02` | 1,973,495 | CRITICAL | 5,000 | MEDIUM_PROP | ✅ | — |
| 2 | `Painel_Lazer` | 1,963,325 | CRITICAL | 8,000 | MEDIUM_PROP | ✅ | — |
| 3 | `Aurora_Box_02` | 1,960,620 | CRITICAL | 5,000 | MEDIUM_PROP | ✅ | — |
| 4 | `Enemy_Robot_roboot` | 1,911,345 | CRITICAL | 15,000 | LARGE_PROP | — | ✅ |
| 5 | `Aurora_Lazer_01` | 1,903,455 | CRITICAL | 5,000 | MEDIUM_PROP | ✅ | — |
| 6 | `Aurora_Box_01` | 1,897,220 | CRITICAL | 5,000 | MEDIUM_PROP | ✅ | — |
| 7 | `Aurora_Door_01` | 1,866,867 | CRITICAL | 5,000 | MEDIUM_PROP | ✅ | — |
| 8 | `DrElias_Animation_tripo` | 985,940 | CRITICAL | 15,000 | LARGE_PROP | — | ✅ |
| 9 | `DrElias_Animation_modelo` | 985,940 | CRITICAL | 15,000 | LARGE_PROP | — | — |
| 10 | `DrElias_Jump` | 49,112 | HEAVY | 8,000 | MEDIUM_PROP | — | — |
| 11 | `DrElias_NervouslyLookAround` | 49,112 | HEAVY | 8,000 | MEDIUM_PROP | — | — |
| 12 | `DrElias_Running` | 49,112 | HEAVY | 8,000 | MEDIUM_PROP | — | — |

## Ordem recomendada de otimização

### Prioridade 1 — CRITICAL usados em cenas (6)
- `Aurora_Lazer_02` — 1,973,495 tris → alvo 5,000 (MEDIUM_PROP)  ·  modelo.glb
- `Painel_Lazer` — 1,963,325 tris → alvo 8,000 (MEDIUM_PROP)  ·  Painel_Lazer.fbx
- `Aurora_Box_02` — 1,960,620 tris → alvo 5,000 (MEDIUM_PROP)  ·  modelo.glb
- `Aurora_Lazer_01` — 1,903,455 tris → alvo 5,000 (MEDIUM_PROP)  ·  modelo.glb
- `Aurora_Box_01` — 1,897,220 tris → alvo 5,000 (MEDIUM_PROP)  ·  modelo.glb
- `Aurora_Door_01` — 1,866,867 tris → alvo 5,000 (MEDIUM_PROP)  ·  modelo.glb

### Prioridade 2 — VERY_HEAVY usados em cenas (0)
_(nenhum)_

### Prioridade 3 — HEAVY usados em cenas (0)
_(nenhum)_

### Prioridade 4 — pesados usados apenas em prefabs (2)
- `Enemy_Robot_roboot` — 1,911,345 tris → alvo 15,000 (LARGE_PROP)  ·  roboot.fbx
- `DrElias_Animation_tripo` — 985,940 tris → alvo 15,000 (LARGE_PROP)  ·  tripo_convert_1c0ed329-50ef-45bd-8891-8f1d62783e9c.fbx

### Prioridade 5 — pesados não utilizados (4)
- `DrElias_Animation_modelo` — 985,940 tris → alvo 15,000 (LARGE_PROP)  ·  modelo.fbx
- `DrElias_Jump` — 49,112 tris → alvo 8,000 (MEDIUM_PROP)  ·  Jump.fbx
- `DrElias_NervouslyLookAround` — 49,112 tris → alvo 8,000 (MEDIUM_PROP)  ·  Nervously Look Around.fbx
- `DrElias_Running` — 49,112 tris → alvo 8,000 (MEDIUM_PROP)  ·  Running.fbx

## Recomendações de target tris (presets)
- **SMALL_PROP** = 3.000 tris / tex 1024 — obstáculos simples (caixas, portas, lasers).
- **MEDIUM_PROP** = 8.000 tris / tex 1024–2048 — props com mais detalhe (Painel_Lazer).
- **LARGE_PROP** = 15.000 tris / tex 2048 — modelos grandes/personagens (robô, Dr. Elias).
- **HERO_PROP** = 30.000 tris / tex 2048–4096 — apenas hero props muito importantes.
- Texturas: 1024/2048 na maioria; 4096 só em hero; **nunca 8192** sem confirmação manual.

## Alerta
- Originais **não** foram alterados, movidos ou apagados.
- `.meta` originais foram copiados como `*.meta.backup.txt` (inertes) — sem colisão de GUID.
- Otimização em lote **não** foi executada. Rodar `aurora_hp_to_lp_bake.py` 1 asset por vez.
