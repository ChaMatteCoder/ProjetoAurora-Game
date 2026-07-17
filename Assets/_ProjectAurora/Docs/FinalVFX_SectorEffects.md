# FinalVFX — Onda 2: Obstáculos e Ambiente (relatório)

**Cena:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
**Data:** 2026-07-16
**Status:** implementado e validado em Play Mode (detalhes e ressalvas em §7). **Sem commit** — aguardando validação do cliente.

---

## 1. Pendências da Onda 1 fechadas nesta onda

| Item | Como foi resolvido | Validação |
|---|---|---|
| `StopAll()` não ligado a morte/restart | `AuroraVFXController.Start()` assina `PlayerHealth.OnDeath` → `StopAll()`. Restart recarrega a cena (pool morre junto) | ✅ 6 efeitos vivos → morte → **0** |
| Pulso no ícone E da UI (Etapa 14-A) | `AuroraPromptPulse` no objeto do prompt: pulsa **só cor/alpha** de Glow+cantos (5 targets), zero layout, unscaled time, restaura no OnDisable | ✅ cor base (0.040, 0.860, 1.000) → pulsando (0.048, 1.036, 1.205, a=0.835) |

Descoberta no teste do prompt: `PlayerInteraction.RefreshPrompt` desliga o prompt todo frame sem alvo em alcance — o teste manual precisou desabilitar o componente; **no jogo real o prompt fica ativo em alcance e o pulso roda normalmente**.

## 2. Ativação por setor (Etapa 26)

`AuroraSectorVFXController` — zonas por faixa de Z (scan 5 Hz, margem 60 m), `Play()` ao entrar, `StopEmitting` ao sair (partículas vivas morrem sozinhas).

Objeto de cena **`Aurora Sector VFX`**: 4 zonas, 16 emissores nas laterais da pista (x=±3.5–3.8):

| Zona | Faixa Z | Emissores |
|---|---|---|
| Setor B — Contenção | 450–900 | 2× faíscas + 2× vapor |
| Setor C — Sala de Máquinas | 900–1350 | 2× vapor + 2× faíscas |
| Setor D — Corredor Vermelho | 1350–1800 | 3× corrupção vermelha + 1× faíscas |
| Setor E — Ponte Técnica | 1800–2250 | 3× poeira + 1× faíscas |

Setor A deliberadamente **sem** emissores (legibilidade do tutorial, Etapa 12 da spec original).

**Validado em play:** Setor A → 0 zonas/0 sistemas; z=650 → só B ativa (4 sistemas); z=1550 → B desligou, só D ativa (3 corrupção + 1 faísca). Meta "nada rodando desde o frame 0" cumprida.

## 3. Prefabs e material novos

| Asset | Config |
|---|---|
| `MAT_VFX_Smoke_Soft` | Particles/Unlit, **alpha blend** (não aditivo), cinza a=0.35, SoftCircle |
| `PF_VFX_AmbientSparks` | loop, rate 3,5/s, max 9, caem |
| `PF_VFX_Steam` | loop, rate 6/s, max 20, sobem |
| `PF_VFX_AmbientDust` | loop, rate 2,5/s, max 16, deriva lateral |
| `PF_VFX_CorruptionMotes` | loop, rate 7/s, max 18, aditivo vermelho |
| `PF_VFX_LaserShutdown` | one-shot, 20 faíscas |
| `PF_VFX_DoorDust` | one-shot, 16 puffs cinza |

Todos: sem luz/colisão/trail/sombra; loops com `cullingMode=Pause`. Total ambiente por zona ≈ 60 partículas máx.

## 4. Lasers e portas (Etapas 9–10, escopo mínimo viável)

- **`LaserInteractable.HandleInteraction`**: dispara `LaserShutdown` na posição de cada feixe **antes** de desativá-lo (teto de 3 bursts por painel). ✅ validado: Interact → +1 efeito, feixes off. *(Nota de teste: o gate one-shot `HasBeenUsed` precisou de reset via reflection porque uma chamada anterior do próprio teste o consumiu — no jogo real o `PlayerInteraction` só chama com painel ativo/em alcance.)*
- **`AuroraDoorController.Open()`**: poeira no vão central. ✅ validado: Open → +1 efeito, `IsOpen=True`.
- **Não implementado (registrado):** pulso contínuo do feixe ativo e partículas no emissor enquanto ligado — o feixe já é emissivo serializado; o pulso via MPB fica como melhoria opcional.

## 5. Robôs (Etapa 11)

- `MAT_EnemyRobot_DarkMetal`: keyword **`_EMISSION` serializada** no asset (emissão preta = visual padrão inalterado) — pré-condição da matriz validada na Onda 1.
- `AuroraMaterialPulseController` (novo): pulso de `_EmissionColor` via **MPB** com IDs cacheados, dessincronizado por instância, `activeRange` (culling por distância do player), restaura via `SetPropertyBlock(null)` no OnDisable. Custo aceito: Renderer com MPB sai do SRP Batcher (6 robôs próximos por vez, no máx.).
- **6 robôs-obstáculo** do Setor C: pulso vermelho (range 55 m). ✅ validado: perto → 18/18 renderers com MPB, emissão (1.61, 0.16, 0.07); robô a 290 m → **0 blocks** (culling OK).
- **Perseguidores** (spawnados em runtime pelo `RobotPursuitDirector`): pulso adicionado no spawn — **Lead Pursuer mais forte** (0.5–1.3 @1.1 Hz) que os demais (0.3–0.85 @0.75 Hz), conforme spec.
- **Cutscene da perseguição**: shake `RobotActivation()` no início; **impacto único** quando a porta de contenção assenta (`DoorImpact` + poeira + faíscas na base do slab). ⚠️ *Validado por código; a cutscene completa não foi executada em play nesta rodada.*

## 6. Performance (Editor, z=1550, Setor D ativo, vsync OFF)

| Condição | Frame time |
|---|---|
| Com 4 emissores do D ativos | 14,77 / 14,54 / 14,67 ms (~68 FPS) |
| Mesmo ponto, zona desligada | 12,73 / 12,61 ms (~79 FPS) |
| **Custo isolado do ambiente** | **≈ 1,9 ms no Editor** |

Contexto: o baseline da Onda 1 (11,3 ms) foi em **z=1500**, ponto diferente — a comparação válida é a do mesmo ponto acima. O custo real em build tende a ser menor (overhead de Editor na simulação de partículas). Meta "impacto pequeno" atendida com ressalva; se o cliente achar alto, reduzir rate dos emissores do D é o primeiro botão.

## 7. Testes executados / ressalvas

| Teste | Resultado |
|---|---|
| Zonas ligam/desligam por Z (A→B→D) | ✅ |
| 0 sistemas tocando em repouso (Setor A) | ✅ |
| Robô perto pulsa / longe não atualiza | ✅ |
| Laser shutdown spawna e feixes desligam | ✅ |
| Porta spawna poeira e abre | ✅ |
| Prompt pulsa e restaura cor | ✅ |
| StopAll na morte (6→0) | ✅ |
| Console sem erros | ✅ |
| Perf medida com/sem ambiente | ✅ |
| **Não testado:** cutscene da perseguição completa (spawn+gate) em play; visual dos perseguidores; Setor E em play (zona validada só por simetria com B/D); coexistência do shake com pause/morte/cutscene (pendência antiga) | ⚠️ |

## 8. Fora do escopo desta onda (fica para Onda 3)

CelestIA corrompida (glitch), Terminal Central (partículas do núcleo), Volume local, cutscenes inicial/final, qualidade configurável, builds.
