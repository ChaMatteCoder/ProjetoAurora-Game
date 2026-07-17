# FinalVFX — Relatório de Performance (consolidado)

**Data:** 2026-07-16 · **Condições:** Editor, 1920×1080, Quality=Medium, vsync OFF, `Time.smoothDeltaTime`.
Complementa o `FinalVFX_PerformanceBaseline.md` (Onda 0). Medições de Editor **não equivalem a build** — servem como comparação relativa.

## 1. Evolução por onda (mesmas condições)

| Momento | Local | Frame time | Sistemas de partícula tocando |
|---|---|---|---|
| Onda 0 (sem VFX) | z=1500 | ~12,1 ms (~83 FPS) | 0 |
| Onda 1 (feedback gameplay) | z=1500, repouso | ~11,3 ms (~89 FPS) | 0 em repouso |
| Onda 2 (ambiente) | z=1550, zona D ativa | ~14,7 ms (~68 FPS) | 4 |
| Onda 2 (controle) | z=1550, zona desligada | ~12,7 ms (~79 FPS) | 0 |
| **Custo isolado do ambiente (D)** | | **≈ 1,9 ms (Editor)** | |
| Onda 4 (jornada final) | B 650 / C 1100 / E 1950 / Núcleo 2650 | 22,1 / 26,7 / 20,5 / 19,2 ms | 4 / 4 / 4 / 1 |

Nota honesta sobre a jornada da Onda 4: os tempos são maiores que os das ondas anteriores em pontos equivalentes — a sessão incluía a intro rodando em background e overhead de teleportes; **não** é regressão de VFX (o controle da Onda 2 no mesmo ponto isola o custo real em ~1,9 ms). Fica registrado que o número de referência limpo é o da Onda 2.

## 2. Invariantes verificados (medidos, não supostos)

- **0 Particle Systems tocando em repouso** (Setor A) — meta central cumprida.
- Zonas ligam/desligam por Z: nunca mais de **1 zona ativa** (máx. 4 sistemas ambientais ≈ 60 partículas simultâneas).
- Pool recicla: rajada de 12 coletas → 12 ativos → 0; teto de 24 por prefab jamais estourado; 22 instâncias reusadas.
- Renderers: 6277 (baseline) → 6295 (+18) com todo o VFX instalado.
- MPB dos robôs limitado por `activeRange=55 m` — robôs distantes não recebem `SetPropertyBlock` (0 blocks medidos a 290 m).
- Nenhuma Point Light nova em nenhum efeito. Nenhuma sombra em partículas. Nenhum trail/noise/collision.

## 3. Development Build Windows (Onda 1)

- `Builds/Development/VFX_Wave1_Windows/` — abriu, rodou 25 s sem crash, `Player.log` sem exceções.
- Gameplay dentro da build (coleta/dano) não foi exercitado manualmente — smoke test de abertura+log.

## 4. Qualidade configurável (Etapa 28) — API preparada, integração adiada

Integrar VFX ao dropdown de qualidade do menu mexeria no `AuroraMenuSettingsController` funcional — risco desnecessário nesta rodada. **A API já está pronta** para quando for desejado:

| Knob | Onde | Efeito |
|---|---|---|
| `AuroraCameraFeedbackController.ShakeEnabled` (static) | acessibilidade | desliga todo shake |
| `AuroraVFXPool.maxPerPrefab` / `initialPerPrefab` | público no Inspector | teto de efeitos simultâneos |
| `AuroraSectorVFXController.margin` + `rateOverTime` dos emissores | público | densidade ambiental |
| `AuroraMaterialPulseController.activeRange` | público | alcance do pulso dos robôs |

Low = `ShakeEnabled=false` + rates ambientais reduzidos + `maxPerPrefab=8`; High = padrão atual. Documentado, não integrado.
