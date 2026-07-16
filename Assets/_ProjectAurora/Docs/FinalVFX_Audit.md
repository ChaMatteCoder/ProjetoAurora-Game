# FinalVFX — Auditoria (Etapa 1)

**Projeto:** PROJETO:AURORA — Falha de Contenção
**Cena auditada:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
**Data:** 2026-07-16
**Método:** varredura via Unity (reflection/API em edit mode) + leitura de scripts. Todos os números abaixo foram **medidos**, não estimados.

---

## 1. Sumário executivo

| Item | Estado medido | Implicação |
|---|---|---|
| Particle Systems na cena | **0** | Todo o VFX de partículas é greenfield |
| Volumes | **2**, ambos **globais**, sem collider | Não existe infraestrutura de Volume local |
| Luzes | **124 — todas realtime** (121 point, 3 directional) | ⚠️ **CORRIGIDO** — ver `FinalVFX_PerformanceBaseline.md`: o URP capa em 8 luzes adicionais e apenas ~5 ficam perto da câmera em gameplay. **Não é gargalo.** |
| Luzes com sombra | 3 | OK |
| Materiais em `_ProjectAurora` | 82 (28 com `_EMISSION`) | Base emissiva já existe e é reaproveitável |
| Shader Graphs próprios | **0** (só os do TMP) | Shader Graph disponível, mas nada customizado |
| VFX Graph | **não instalado** | Usar Particle System (Shuriken) — alinhado às regras |
| URP / Shader Graph | 17.4.0 / 17.4.0 | Shader Graph disponível se necessário |
| Renderer de gameplay | `Aurora3DRenderer.asset` (Main Camera, rendererIndex=1, postProcessing=ON) | Preservar |

**Conclusão principal:** o projeto tem uma base sólida de sistemas e materiais emissivos, mas **nenhum efeito de partículas**. O maior risco não é adicionar VFX — é o **orçamento de luzes realtime já consumido** (124/124 realtime).

---

## 2. Efeitos existentes (reaproveitáveis)

Sistemas já implementados que servem de **gancho** para VFX, sem precisar de refatoração:

| Script | Gancho disponível |
|---|---|
| `PlayerHealth` | `TakeDamage()`, eventos `IntegrityChanged(int,int)` e `OnDeath` |
| `SuitIntegrityRecovery` | start (`startSfxPlayed`), progresso (`chargeProgress`), conclusão (`TryRestoreSegment()`), cancelamento (`OnIntegrityChanged` ao tomar dano) |
| `TubeCorePulse` | pulso do núcleo do Terminal (ciano/vermelho) |
| `TerminalLightsAwakening` | ignição progressiva por bancos (emissivos + lâmpadas) |
| `TerminalFinalePresentation` | apresentação da cutscene final |
| `PanelScreenPulse`, `ProximityScreen` | pulso/brilho de telas |
| `AuroraCoinVisualController` | rotação/flutuação/pulso/coleta da moeda |
| `LaserHazard`, `LaserInteractable` | estado ativo/desligado do laser |
| `AuroraDoorController`, `AuroraSectorDoorTrigger` | abertura de portas |
| `RobotPursuitDirector`, `EnemyRobotProceduralAnimator` | perseguição e animação dos robôs |
| `CelestIACommPanel` | HUD da CelestIA (normal/corrompida) |
| `SectorManager` | base para ativação de VFX por setor (Etapa 26) |
| `AuroraSfx` | serviço central de SFX já existente — **reutilizar, não duplicar** |

**Efeitos visuais que já existem (não recriar):**
- Blink de renderer ao tomar dano (`PlayerHealth.DamageFeedback`, ~2s).
- Pulso do núcleo do tubo (`TubeCorePulse`).
- Ignição progressiva das luzes do Núcleo (`TerminalLightsAwakening`).
- Flutuação + brilho por proximidade das telas (`ProximityScreen`).
- Rotação/flutuação/coleta da AuroraCoin.
- Emissão dos materiais (28 materiais com `_EMISSION`).

---

## 3. Efeitos ausentes (a implementar)

Nenhum dos itens abaixo existe hoje:

- Partículas de qualquer tipo (dano, faíscas, vapor, poeira, energia, corrupção, destroços).
- Camera shake (nenhum controlador de feedback de câmera).
- Volume local (Terminal / Setor D) — só existem 2 volumes globais.
- Efeito próprio de coleta de AuroraCoin / DataFile (partículas, rastro).
- Efeito visual de interação "E" (anel, onda, flash).
- Estados visuais de laser (ativando/desligando) além do on/off.
- Efeitos de abertura de porta.
- Emissão pulsante / corrupção nos robôs via MaterialPropertyBlock.
- Glitch da CelestIA corrompida.
- Pooling de VFX.
- Ativação de VFX por setor.

---

## 4. Efeitos duplicados / conflitos

- **Nenhuma duplicação de partículas** (não há partículas).
- **Alerta histórico:** já houve um conflito real de dois sistemas controlando a mesma luz (`ProgressiveLight` × `TerminalLightsAwakening`), onde a ordem `Awake`/`Start` zerava a `baseIntensity`. **Lição:** ao adicionar VFX que altere emissão/intensidade, verificar se outro sistema já controla o mesmo alvo. Hoje o Núcleo é controlado exclusivamente por `TerminalLightsAwakening` (+2 fills com `ProgressiveLight`).
- `Fase01 Global Volume` está **desativado** na cena, mas presente — candidato a limpeza (não remover sem confirmar).

---

## 5. Pontos críticos de performance

Ordenados por gravidade:

1. ~~**124 luzes realtime (121 point lights) — CRÍTICO e PRÉ-EXISTENTE.**~~
   ⚠️ **ESTA AVALIAÇÃO ESTAVA ERRADA.** Medição posterior (ver `FinalVFX_PerformanceBaseline.md`) mostrou: URP capa em **8 luzes adicionais**, **nenhuma point light projeta sombra**, e apenas **5 ficam perto da câmera** em gameplay real (range médio 11,2 m espalhado por 3000 m de pista). **As luzes não são o gargalo.** O custo real está nos **6277 Renderers ativos**.
   A regra "nenhum Point Light novo por VFX" **permanece válida** — mas como boa prática (o cap de 8 é recurso escasso e compartilhado), não por uma crise inexistente.
2. **Overdraw de partículas transparentes.** Em runner, a câmera vê a pista inteira; partículas grandes na tela custam caro. Limitar tamanho e `maxParticles`.
3. **Ausência de culling por setor.** Hoje não há ativação por setor para VFX; sem isso, todos os sistemas rodariam desde o início.
4. **Instanciar/destruir efeitos por coleta.** Com 186 AuroraCoins na cena, coleta rápida exige **pooling** (Etapa 25).
5. **`Renderer.material`** cria cópia de material por instância — proibido; usar `MaterialPropertyBlock` (Etapa 24).

---

## 6. Materiais que podem ser compartilhados

- Já existem **28 materiais com `_EMISSION`** — reaproveitar a paleta (ciano/vermelho/âmbar) em vez de criar novos.
- VFX novos devem usar **poucos materiais compartilhados** em `Assets/_ProjectAurora/VFX/Materials/`:
  - `MAT_VFX_Additive_Cyan` (energia, moeda, interação, traje, núcleo normal)
  - `MAT_VFX_Additive_Red` (corrupção, alarme, robôs, núcleo corrompido)
  - `MAT_VFX_Smoke_Soft` (vapor, poeira, colapso)
  - `MAT_VFX_Spark` (faíscas)
- Um material por *família* de efeito, não por prefab.

---

## 7. Plano de implementação (ondas priorizadas)

Ordenado por **valor percebido ÷ risco**. As ondas 1–2 entregam a maior parte do impacto visual.

### Onda 1 — Núcleo técnico + feedback de gameplay (maior impacto)
- `AuroraCameraFeedbackController` (shake leve, não acumulativo, configurável) — Etapa 23.
- `AuroraMaterialPulseController` (MaterialPropertyBlock, IDs cacheados) — Etapa 24.
- `AuroraVFXPool` (pooling simples por prefab) — Etapa 25.
- `AuroraVFXController` (fachada: `PlayDamage()`, `PlayCoinCollect()`, …).
- **Feedback de dano** (Etapa 4) — partículas + pulso emissivo + shake leve.
- **Coleta de AuroraCoin** (Etapa 6) + **DataFile** (Etapa 7) com pooling.

### Onda 2 — Interação e obstáculos
- Interação "E" (Etapa 8), Lasers (Etapa 9), Portas (Etapa 10), Robôs (Etapa 11).
- Suit Recovery (Etapa 5) integrado aos ganchos existentes.

### Onda 3 — Ambiente por setor
- `AuroraSectorVFXController` + ativação por setor (Etapa 26).
- Setores A→E (Etapas 12–16).

### Onda 4 — Clímax e cinema
- CelestIA corrompida (17), Terminal (18), Volume local (19), Cutscenes (20–22).

### Onda 5 — Fechamento
- Qualidade configurável (28), compatibilidade (29), testes (30), builds, relatórios (31).

---

## 8. Regras derivadas da auditoria (obrigatórias)

1. **Zero Point Light nova por efeito** — o orçamento já está em 124 realtime.
2. Emissão + Bloom no lugar de luz.
3. `MaterialPropertyBlock` sempre; nunca `Renderer.material`.
4. Pooling para efeitos recorrentes (moeda, faísca, impacto).
5. Antes de controlar emissão/intensidade de um alvo, verificar se outro sistema já o controla (lição do conflito `ProgressiveLight`×`TerminalLightsAwakening`).
6. VFX ativado por setor/distância; nada rodando desde o frame 0.
7. Nenhum efeito pode reduzir a legibilidade de obstáculos ou HUD.

---

## 9. Estado desta rodada

- **Etapa 1 (auditoria):** concluída — este documento.
- **Etapa 2 (estrutura de pastas):** concluída (`VFX/` + `Scripts/VFX/` criadas).
- **Etapas 3–31:** pendentes, a executar nas ondas acima.
