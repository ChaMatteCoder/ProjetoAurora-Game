# FinalVFX — Baseline de Performance e Auditoria de Luzes (Onda 0)

**Cena:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
**Data:** 2026-07-16
**Método:** medição em Play Mode real na Beta03 (não estimada). Todos os números foram observados.

---

## 1. CORREÇÃO DA AUDITORIA ANTERIOR

O `FinalVFX_Audit.md` classificou as **124 luzes realtime como "CRÍTICO"**. **Essa avaliação estava errada** e é corrigida aqui com dados medidos.

| Afirmação anterior | Realidade medida |
|---|---|
| "124 luzes realtime — CRÍTICO" | URP capa em **8 luzes adicionais** (`maxAdditionalLightsCount=8`). As 124 nunca renderizam juntas. |
| "risco de performance" | Com o player em gameplay (z=1500), apenas **5 luzes** estão perto o suficiente da câmera para importar. |
| "121 point lights pressionam o frame" | `supportsAdditionalLightShadows=**False**` — **nenhuma** point light projeta sombra. Só a directional principal projeta. |

**Causa do erro:** contei luzes no arquivo da cena sem verificar o cap do URP nem a distribuição espacial. As luzes estão espalhadas por **~3000 m de pista** com range médio de **11,2 m** — a densidade local é baixa por construção.

**Conclusão:** as luzes **não são o gargalo**. A regra "nenhum Point Light novo por VFX" continua válida como boa prática (o cap de 8 é um recurso escasso e compartilhado), mas **não** por causa de uma crise inexistente.

---

## 2. Configuração do pipeline (medida)

| Setting | Valor |
|---|---|
| URP asset | `UniversalRP` |
| `maxAdditionalLightsCount` | **8** |
| `additionalLightsRenderingMode` | PerPixel |
| `supportsAdditionalLightShadows` | **False** |
| `supportsMainLightShadows` | True |
| `useSRPBatcher` | **True** |
| MSAA | 1 (off) |
| renderScale | 1 |
| Quality level | Medium (2) |
| `pixelLightCount` | 4 |

`useSRPBatcher=True` é o motivo de MPB importar: um Renderer com MPB sai do caminho do SRP Batcher (mas **continua renderizando** — a formulação "ignorado" da rodada anterior estava incorreta, conforme já apontado).

---

## 3. Auditoria das luzes

| Métrica | Valor medido |
|---|---|
| Total na cena | 124 (110 ativas, 12 inativas, 3 directional) |
| Com sombra | **3** (apenas as directional) |
| Point lights | 121 — **0 com sombra** |
| Range médio (point) | 11,2 m |
| Maior range | 24 m |
| **Perto da câmera em gameplay (z=1500)** | **5** |

### Distribuição por Z (bucket de 250 m)

| Faixa Z | Luzes |
|---|---|
| -250..0 | 3 |
| 0..250 | 12 |
| 250..500 | 8 |
| 500..750 | 12 |
| 750..1000 | 4 |
| 1000..1250 | 7 |
| 1250..1500 | 10 |
| 1500..1750 | 12 |
| 1750..2000 | 11 |
| 2000..2250 | 12 |
| 2250..2500 | 18 |
| 2500..2750 | 15 |

Distribuição uniforme ao longo da pista — **auto-culling natural por distância**.

### Grupos repetidos

| Nome | Qtd |
|---|---|
| BeamGlow (lasers) | 26 |
| Terminal Route Light | 16 |
| Holo_Light | 12 |
| Laboratory Ceiling Light | 10 |
| Panel_Light | 8 |
| Integrated Chamber Fill | 3 |

### Otimizações aplicadas nesta onda

**Nenhuma.** Justificativa honesta: os dados mostram que não há problema a corrigir.
- Desligar sombras de luzes decorativas → **já não há** (0 point lights com sombra).
- Desativar luzes de setores distantes → **o URP já faz** por distância/cap de 8.
- Reduzir range exagerado → range médio 11,2 m e máximo 24 m já são conservadores.
- Remover duplicatas → os nomes repetidos são instâncias legítimas espalhadas pela pista, não duplicatas sobrepostas.

Aplicar "otimizações" aqui seria mexer em sistema funcional sem necessidade, contrariando as regras do projeto. **A identidade visual foi preservada por omissão deliberada.**

---

## 4. Baseline de performance (Editor)

**Condições:** Play Mode na Beta03, 1920x1080, Quality=Medium, **vsync desligado** (para medir custo real, não o teto do monitor), player teleportado para z=1500 (Setor D).

| Amostra | Frame time | FPS aprox. |
|---|---|---|
| 1 | 14,27 ms | ~70 |
| 2 | 9,53 ms | ~105 |
| 3 | 12,42 ms | ~81 |
| **Média** | **~12,1 ms** | **~83** |

**Contexto do custo real:**
- **6277 Renderers ativos na cena.** Este é o principal driver de custo (draw calls / culling), não as luzes.
- 0 Particle Systems (baseline sem qualquer VFX).

### Ressalvas honestas sobre esta medição

1. **É medição de Editor** — inclui overhead do Editor e **não equivale a uma build**. Serve como referência relativa (antes/depois), não como número absoluto de shipping.
2. **Amostragem pequena** (3 amostras, variação de 9,5–14,3 ms). A variância é alta; não é um profiling estatístico.
3. **Não foi feito Development Build Windows** para baseline — pendente.
4. Medido em um único ponto da pista (z=1500). Outros setores podem ter custo diferente.

---

## 5. Camera Feedback Controller — VALIDADO

`AuroraCameraFeedbackController` anexado à `Main Camera` (que também tem `CameraFollow`, target = `Dr. Elias - Player`).

### Descoberta relevante

**Vários sistemas dirigem a câmera**, não só o `CameraFollow`:
- `IntroCutsceneController`
- `FinalCutsceneController`
- `DeathCinematics`

O design escolhido (remover offset no `Update`, aplicar no `LateUpdate` com `[DefaultExecutionOrder(100)]`) é robusto a todos eles: o offset é sempre removido **antes** de qualquer sistema ler/escrever a posição, e reaplicado **depois** deles.

### Resultados medidos (drivers de câmera desligados para isolar)

| Teste | Resultado |
|---|---|
| Câmera parada, sem shake | desvio **0,00000 m** |
| Shake em voo (elapsed 15,08/60) | offset aplicado `(-0,041, -0,041, 0)` → magnitude **0,058 m** (>0 e ≤ amplitude 0,10 ✓) |
| Amortecimento | a 25% do tempo, amplitude efetiva ~0,056 — bate com a curva `amplitude · (1-t)²` ✓ |
| **Após shake de 10 s completo** | `appliedOffset=(0,0,0)`, posição **exatamente** `(0, 3, 0)` — **deriva zero** ✓ |

### Ainda NÃO testado (pendente)

- Pause durante shake
- Morte / troca de estado durante shake
- Cutscene durante shake
- Desativação da câmera durante shake
- Comportamento com `CameraFollow` **ativo** (o teste isolou desligando-o)

---

## 6. Estado da Onda 0

| Item | Estado |
|---|---|
| Validar camera controller em Play | ✅ (com ressalvas do item 5) |
| Anexar à câmera sem quebrar CameraFollow | ✅ anexado; coexistência testada apenas com follow desligado |
| Confirmar retorno do transform | ✅ deriva zero medida |
| Medir FPS/frame time sem VFX | ✅ ~12,1 ms / ~83 FPS (Editor) |
| Auditar 124 luzes | ✅ — e **corrigiu erro da auditoria anterior** |
| Otimizações seguras | ✅ nenhuma necessária (justificado) |
| Baseline Development Build Windows | ❌ **pendente** |
| Documento | ✅ este arquivo |
