# FinalVFX — Relatório da Onda 1 (Feedback Essencial)

**Cena:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
**Data:** 2026-07-16 (revisão 2 — pós-correções do checklist)
**Status:** ver §10 (checklist final com o que passou e o que resta).

---

## 1. Política de materiais — corrigida e VALIDADA empiricamente

A política oficial (A: partículas com material serializado / B: MPB para repetidos / C: instância para heros / D: fallback `_BaseColor` HDR) foi gravada no `FinalVFX_Audit.md`, substituindo as regras absolutas anteriores.

### Matriz de validação (Etapa 3) — MEDIDA na Beta03

Teste com 5 quads **na câmera real de gameplay** (Setor A), material-base URP/Lit com albedo quase preto e **`_EMISSION` serializada** no asset. Screenshots capturados e inspecionados.

| Método | Propriedade | Material com emissão serializada | Resultado |
|---|---|---|---|
| (referência) | emissão serializada | sim | **VISÍVEL** (ciano) |
| MPB | `_BaseColor` | sim (emissão zerada via MPB p/ revelar albedo) | **VISÍVEL** (verde) |
| MPB | `_EmissionColor` | sim | **VISÍVEL** (magenta — sobrescreveu o ciano) |
| Material instance | `_BaseColor` | sim (emissão zerada) | **VISÍVEL** (laranja) |
| Material instance | `_EmissionColor` | sim | **VISÍVEL** (vermelho) |

**Conclusão: os 4 métodos funcionam.** A crença anterior ("emissão em runtime não renderiza / MPB ignorado") foi **refutada** — o cenário que falhava era `EnableKeyword("_EMISSION")` **somente em runtime**. Pré-condição: keyword serializada num material-asset referenciado (garante a variante na build).

Notas de execução honestas:
- Duas capturas foram necessárias: na primeira, os `_BaseColor` ficaram mascarados pela emissão ciano forte do material-base (falha de desenho do teste, corrigida zerando a emissão dos quads B e D).
- Descoberta de harness: desabilitar `IntroCutsceneController` **não para as corrotinas dele** — a câmera continuou sendo dirigida. Solução: quads parenteados à câmera.
- Objetos de teste (`VFX_MaterialValidation_Temporary_*`) destruídos; material temporário deletado; Console sem erros.

Propriedades verificadas via `Material.HasProperty`: `_BaseColor` ✓, `_EmissionColor` ✓ (URP/Lit e URP/Particles/Unlit usam `_BaseColor`/`_BaseMap`).

---

## 2. Materiais criados (5, compartilhados)

| Material | Shader | Cor HDR | Textura |
|---|---|---|---|
| `MAT_VFX_Additive_Cyan` | URP/Particles/Unlit | (0.35, 2.6, 3.2) | TEX_VFX_SoftCircle |
| `MAT_VFX_Additive_Red` | URP/Particles/Unlit | (3.0, 0.35, 0.18) | TEX_VFX_SoftCircle |
| `MAT_VFX_Spark` | URP/Particles/Unlit | (3.2, 1.9, 0.7) | TEX_VFX_Spark |
| `MAT_VFX_Particle_White` | URP/Particles/Unlit | (2.2, 2.2, 2.2) | TEX_VFX_SoftCircle |
| `MAT_VFX_DigitalScan` | URP/Particles/Unlit | (0.25, 2.2, 2.8) | TEX_VFX_DigitalLine |

Todos: Transparent + Additive, ZWrite off, double-sided, renderQueue 3000.

## 3. Texturas procedurais (Etapa 5) — geradas por script

| Textura | Conteúdo | Formato |
|---|---|---|
| `TEX_VFX_SoftCircle` | gradiente radial suave (smoothstep²) | 128×128 PNG, clamp, alphaIsTransparency |
| `TEX_VFX_Spark` | núcleo intenso + halo de queda rápida | 128×128 PNG |
| `TEX_VFX_DigitalLine` | traço vertical com fade nas pontas | 128×128 PNG |

Sem paths absolutos em runtime (texturas referenciadas pelos materiais).

## 4. Prefabs (5)

Todos: lights/collision/trails/noise/shadows OFF, culling Automatic, faixa 18–30 partículas.

| Prefab | maxParticles | Observação |
|---|---|---|
| `PF_VFX_PlayerDamage` | 28 | faíscas (TEX_Spark) |
| `PF_VFX_SuitRecovery` | 24 | **loop**, emissão contínua 14/s, simSpace Local (bug one-shot corrigido — ver §6 da rev. 1) |
| `PF_VFX_CollectBurst` | 22 | reutilizado por moeda + conclusão do recovery |
| `PF_VFX_DigitalScan` | 30 | **renderMode Stretch** (linhas verticais subindo), material próprio |
| `PF_VFX_InteractionPulse` | 18 | no alvo interagido, não na UI |

## 5. Pooling e fachada

- `AuroraVFXPool`: pool por prefab, pré-cria 4, teto 24, recicla no Update, `ReleaseAll()`.
- `AuroraVFXController`: fachada estática no-op-se-ausente. Host na cena: `Aurora VFX`.

## 6. Integrações (sistemas existentes)

| Sistema | Gancho | Validação |
|---|---|---|
| `PlayerHealth.TakeDamage` | faíscas + shake | ✅ play (Lives 3→2, efeito spawnado) |
| `SuitIntegrityRecovery` | start/complete/cancel/OnDisable | ✅ play (efeito vivo preso ao corpo; cancel limpa) |
| `AuroraCoinCollectible` | burst no `else` | ✅ play (rajada 12 → reciclada a 0) |
| `DataFileManager` | DigitalScan | ✅ spawn via API |
| `PlayerInteraction` | pulso no alvo | ✅ spawn via API |
| **HUD contador de moedas** | `AuroraCoinHudController` (JÁ EXISTIA: pulso 1→1.08, 0,22s, unscaled, OnCoinsAdded) | ✅ play: `TryAddCoins(1)` → texto 000→001, pulseRoutine ativa, escala 1,014 medida em pleno pulso |

**Correção do checklist anterior:** a Etapa 12 (pulso da HUD) constava como "não feito" — na verdade **o sistema já existia e estava ligado na cena** (balanceText, pulseTarget, iconGlow). Foi apenas validado, não reimplementado.

## 7. Performance antes/depois (Editor, z=1500, vsync OFF, Medium, 1080p)

| Métrica | Baseline (sem VFX) | Pós-Onda 1 |
|---|---|---|
| Frame time (amostras) | 14,27 / 9,53 / 12,42 | 11,30 / 11,08 / 11,51 / 11,41 / 11,09 |
| **Média** | ~12,1 ms (~83 FPS) | **~11,3 ms (~89 FPS)** |
| Renderers ativos | 6277 | 6295 (+18) |
| ParticleSystems tocando em repouso | 0 | **0** (meta atingida) |
| Rajada de 12 coletas | n/a | reciclada a 0; frame time inalterado (11,09 ms) |

Sem regressão (diferença dentro do ruído do Editor). Instâncias do pool inativas não renderizam.

## 8. Save dev/build (pedido extra do cliente, fora da spec da onda)

`AuroraProgressSaveService` ganhou modo dev: **no Editor com save padrão**, moedas e DataFiles são zerados a cada Play e **nada é escrito em disco**; em build, comportamento normal. Testes de Editor preservados (usam caminho customizado, fora do gate). Validado em play: 12/12 DataFiles visíveis e saldo 0 com o disco contendo 999/17; `TryAddCoins(25)+Save()` não alterou o disco. Save contaminado do usuário (999 moedas) **excluído com autorização** (`aurora_progress.json` + `.bak`).

## 9. Development Build Windows (Etapa 17)

Gerada em `Builds/Development/VFX_Wave1_Windows/` — resultado registrado na conclusão da rodada (ver resposta da sessão; se falhou, está como pendência).

## 10. Checklist final da Onda 1

| Critério | Estado |
|---|---|
| Doc de materiais corrigida | ✅ |
| MPB testado de verdade na Beta03 + matriz | ✅ (4/4 métodos funcionam) |
| Camera shake sem deriva | ✅ (Onda 0) |
| Shake com CameraFollow ativo / pause / morte / cutscene | ⚠️ **pendente** — segue não isolado |
| Dano com partículas + shake, sem spam em i-frames | ✅ (i-frames do PlayerHealth bloqueiam re-dano; VFX só dispara em dano real) |
| Suit Recovery: início/contínuo/conclusão/cancelamento | ✅ |
| AuroraCoin: burst só na coleta + contador real pulsando | ✅ |
| Coleta rápida sem instantiate/destroy contínuo | ✅ (pool, teto 24) |
| DataFile com efeito próprio, sem moedas | ✅ (scan de linhas, sem tocar em saldo) |
| Interação E: confirmação no mundo | ✅ · pulso no ícone E da UI: ⚠️ **não implementado** (prompt já tem show/hide; pulso de UI ficou de fora) |
| Sem Point Light nova / sem idle nas 186 moedas | ✅ |
| VFX em repouso = 0 sistemas ativos | ✅ medido |
| Performance antes/depois registrada | ✅ |
| Console sem erros novos | ✅ |
| Objetos temporários removidos | ✅ (quads + material TEMP deletados) |
| Development Build Windows | ver §9 |
| Onda 2 não iniciada | ✅ |

### Pendências reais para a Onda 2
1. Testes de coexistência do shake (pause/morte/cutscene/CameraFollow ativo) — itens 2–4 da Etapa 18.
2. Pulso no ícone E da UI (parte A da Etapa 14).
3. Confirmação visual **a olho humano** dos 5 efeitos (cores/escala/leitura) — validação de gosto pertence ao cliente.
4. `AuroraVFXController.StopAll()` ainda não ligado a morte/restart.
5. Lasers, portas, robôs, ambiente por setor (escopo da Onda 2) — agora desbloqueados pela matriz de materiais: **MPB com keyword serializada é o caminho aprovado para objetos repetidos**.
