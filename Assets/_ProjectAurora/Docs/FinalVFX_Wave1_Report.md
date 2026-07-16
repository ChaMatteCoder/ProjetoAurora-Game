# FinalVFX — Relatório da Onda 1 (Feedback Essencial)

**Cena:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
**Data:** 2026-07-16
**Status:** **PARCIAL** — implementado e integrado; testes parcialmente executados. Ver §7.

---

## 1. Estratégia de materiais — o que foi realmente usado

**Nenhum MaterialPropertyBlock foi usado nesta onda, e nenhuma instância de material foi criada.**

Motivo: os 5 efeitos da Onda 1 são **Particle Systems com material emissivo serializado**. Nenhum deles precisa alterar emissão em runtime — a cor HDR já vive no `.mat` e o Bloom faz o halo. Esse é o caminho que a estratégia oficial marca como sempre-funciona.

MPB só passa a importar na **Onda 2** (pulsar mesh renderers já existentes: robôs, painéis, emissores de laser). **A decisão sobre MPB foi deliberadamente adiada para lá**, onde ela de fato pesa.

### Tentativa de validação de MPB — INCONCLUSIVA

Tentei validar MPB nesta rodada e **falhei por problema de harness, não do produto**:
- 1ª tentativa: caiu no MainMenu (override de Play Mode Start Scene) — quads ficaram atrás do canvas.
- 2ª tentativa: câmera isolada + RenderTexture + `ReadPixels` retornou tudo preto, **incluindo o fundo** — indício de que `Camera.Render()` manual não funciona no URP/SRP e a câmera de teste não renderizou.

**Não há conclusão sobre MPB.** A afirmação da rodada anterior ("MPB é ignorado") **continua não comprovada e não deve ser tratada como fato**. Pendente para a Onda 2, com harness adequado.

---

## 2. Materiais criados (compartilhados, HDR serializado)

| Material | Shader | `_BaseColor` (HDR) | Uso |
|---|---|---|---|
| `MAT_VFX_Additive_Cyan` | URP/Particles/Unlit | (0.35, 2.6, 3.2) | traje, moeda, scan, interação |
| `MAT_VFX_Additive_Red` | URP/Particles/Unlit | (3.0, 0.35, 0.18) | corrupção/alarme (reservado p/ Onda 2) |
| `MAT_VFX_Spark` | URP/Particles/Unlit | (3.2, 1.9, 0.7) | faíscas de dano |

Todos: Transparent + Additive, `_ZWrite=0`, double-sided. Valores >1 alimentam o Bloom.

---

## 3. Prefabs criados (5)

Todos com **lights OFF, collision OFF, trails OFF, noise OFF, shadows OFF** e `cullingMode=Automatic`.

| Prefab | Pasta | maxParticles | Lifetime | Loop |
|---|---|---|---|---|
| `PF_VFX_PlayerDamage` | Gameplay | 28 | 0,35 s | não |
| `PF_VFX_SuitRecovery` | Gameplay | 24 | 1,1 s | **sim** (contínuo) |
| `PF_VFX_CollectBurst` | Collectibles | 22 | 0,45 s | não |
| `PF_VFX_DigitalScan` | Collectibles | 30 | 0,7 s | não |
| `PF_VFX_InteractionPulse` | Interactions | 18 | 0,4 s | não |

Todos dentro da faixa "efeito pequeno" (10–40 partículas). `PF_VFX_CollectBurst` é reutilizado pela moeda **e** pelo flash de conclusão do Suit Recovery — sem duplicar prefab só para trocar contexto.

---

## 4. Scripts criados

| Script | Papel |
|---|---|
| `AuroraCameraFeedbackController` | Shake (validado na Onda 0) |
| `AuroraVFXPool` | Pool por prefab; teto 24/prefab; `Update` varre e recicla; `ReleaseAll()` |
| `AuroraVFXController` | Fachada estática. **No-op silencioso se ausente** — nenhum sistema de gameplay quebra sem VFX |

Host na cena: GameObject **`Aurora VFX`** (pool + controller + 5 prefabs ligados).

---

## 5. Integração (nos sistemas existentes, sem criar paralelos)

| Sistema | Ponto de integração |
|---|---|
| `PlayerHealth.TakeDamage()` | `AuroraVFXController.PlayerDamage()` → faíscas + shake |
| `SuitIntegrityRecovery` | start (`startSfxPlayed`), complete (`TryRestoreSegment`), cancel (`OnIntegrityChanged`), `OnDisable` |
| `AuroraCoinCollectible` | ramo `else` do `collectionBurst` (as 186 moedas não têm burst próprio) |
| `DataFileManager.ShowCollectedFeedback()` | `DataFileCollect()` na posição do player |
| `PlayerInteraction` | `InteractionConfirm()` na posição do **alvo** (não na UI → não cobre texto nem bloqueia raycast) |

**Blink de dano preservado.** O blink de 2 s **é** o indicador de invulnerabilidade (roda por `invulnerabilityDuration`). Não foi encurtado: mexer nele removeria a leitura de i-frames. As faíscas (0,35 s) e o shake (0,22 s) foram somados **por cima**, dentro da janela 0,25–0,50 s pedida.

---

## 6. Bug encontrado e corrigido pelos testes

**`PF_VFX_SuitRecovery` foi criado como one-shot (0,9 s), mas a recuperação dura ~10 s e é contínua.**

Consequência medida: o efeito terminava, o pool o reciclava e reparentava para `Aurora VFX`, mas o campo `recoveryVfx` **continuava apontando para a instância reciclada** — uma referência pendurada a um objeto que o pool poderia reusar para outro efeito. `StopRecoveryVfx()` mataria o efeito errado.

**Correção:** prefab virou `loop=true`, emissão contínua (rate 14/s), `simulationSpace=Local`, velocity +Y (sobe pelo traje). Sistema em loop fica sempre "alive" → o pool não recicla até `StopRecoveryVfx()` parar.

**Validado após o fix:** `isPlaying=True`, `loop=True`, 15 partículas, `parent='Dr. Elias - Player'`.

Este bug passou pela compilação limpa. Só apareceu em teste real.

---

## 7. Testes — o que passou e o que NÃO foi feito

| # | Teste | Status |
|---|---|---|
| 1 | Receber dano | ✅ Lives 3→2, 1 efeito spawnado |
| 2 | Vários danos sem efeitos presos | ⚠️ **parcial** — testado via rajada de 12 bursts (todos reciclados), não via danos reais consecutivos (i-frames impedem) |
| 3 | Iniciar recuperação | ✅ efeito vivo, preso ao corpo |
| 4 | Cancelar recuperação | ⚠️ **parcial** — referência limpa e `chargeProgress=0`, mas o efeito já havia sido encerrado pelo *complete* antes do dano; a transição exata não foi isolada |
| 5 | Completar recuperação | ✅ (implícito no teste 4) |
| 6 | Coletar várias moedas em sequência | ✅ 12 simultâneas → 12 ativos → reciclados para 0 |
| 7 | Coletar DataFile | ✅ spawn confirmado |
| 8 | Usar interação E | ✅ spawn confirmado (via API) |
| 9 | Pausar durante efeito | ❌ **não testado** |
| 10 | Reiniciar corrida | ❌ **não testado** |
| 11 | Verificar objetos do pool | ✅ 22 instâncias reusadas, teto 24 respeitado |
| 12 | Câmera não deriva | ✅ (Onda 0: deriva zero após 10 s de shake) |
| 13 | Console sem erros | ✅ limpo |
| 14 | Comparar performance com baseline | ❌ **não medido depois** |

**Ressalva importante:** os testes foram feitos via **API (chamadas diretas)**, não jogando de fato. Nenhum efeito foi **confirmado visualmente na tela** — sei que os Particle Systems spawnam, emitem partículas e reciclam, mas **não vi os efeitos renderizando**. Isso é uma lacuna real.

---

## 8. Performance

**Não medida após os VFX.** O baseline da Onda 0 (~12,1 ms / ~83 FPS no Editor) não tem contraparte pós-VFX. Pendente.

Mitigações já embutidas: partículas pequenas (18–30), sem luz/colisão/trail/noise/sombra, culling automático, pooling com teto.

---

## 9. Pendências

**Da Onda 1:**
- Confirmação **visual** dos 5 efeitos renderizando na tela (a maior lacuna).
- Testes 9, 10 e 14 (pause, restart, performance pós-VFX).
- Isolar o cancelamento da recuperação (teste 4).
- Ajuste artístico: cores/escala/quantidade nunca foram avaliados visualmente.
- `AuroraVFXController.StopAll()` existe mas **não está ligado** a morte/restart/troca de estado.

**Para a Onda 2:**
- **Validar MPB com harness adequado** — segue em aberto e gate-eia robôs/painéis/lasers.
- Lasers, portas, robôs, ambiente por setor.
