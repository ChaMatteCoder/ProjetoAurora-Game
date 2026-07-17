# FinalVFX — Onda 3: CelestIA, Terminal Central e Cutscene Final

**Cena:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
**Data:** 2026-07-16
**Status:** implementado e validado em Play (ressalvas em §5). **Sem commit** — aguardando validação do cliente.

---

## 1. Corrupção da CelestIA (Etapa 17)

**Descoberta da auditoria: o glitch corrompido JÁ EXISTIA** (`CelestIACommPanel`, Round 13): bursts intermitentes com jitter em status/nome, troca de cor ciano↔vermelho, alpha instável, restauração exata da posição base, supressão quando o Dr. Elias fala. Nada foi recriado.

**Adicionado nesta onda (o único gap da spec): waveform irregular no estado corrompido** — ruído em degraus de ~1/20s sobre a senoide: picos súbitos (25), dropouts (2,5) e alturas erráticas. Não toca em layout nem na mensagem; estados Normal/Transition permanecem com a senoide suave.

**Validado em play:** com `SetState(Corrupted)`, alturas medidas `... 8,0 4,0 25,0 3,7 ...` (salto abrupto + pico) vs. senoide suave no estado normal.

## 2. Terminal Central — energia do núcleo (Etapa 18)

- **`PF_VFX_CoreEnergy`** (Terminal/): loop, cone estreito subindo pelo núcleo, 60 max, material branco tingível, culling Pause.
- Instanciado **dentro** do `CoreTube_Final` (base do núcleo, y=1.9) — só o núcleo interno é afetado; a carcaça permanece intocada (regra do cliente preservada).
- **Sincronizado por construção com o `TubeCorePulse`**: campo novo `coreEnergy` — a cor `startColor` recebe a MESMA cor do pulso ciano↔vermelho, e o `rateOverTime` cresce 10→22 com a instabilidade.
- Ligado como **5ª zona** do `AuroraSectorVFXController` (Núcleo, z 2250–2750) — não roda até o player chegar.

**Validado em play (z=2650):** zona ativa; partículas tocando; cor amostrada no pico vermelho `(1.00, 0.08, 0.05)` rate 22 e no meio do ciclo `(0.36, 0.41, 0.72)` rate 13,5 — sincronização visível entre amostras.

Painel do terminal: o feedback de interação com E já é coberto pelo `PF_VFX_InteractionPulse` da Onda 1 (spawn no alvo). Braços robóticos: **sem efeito novo** — decisão deliberada: partículas em juntas de braços animados exigiriam rastreamento por bone; custo/risco alto para ganho pequeno. Registrado como omissão consciente.

## 3. Volume local do Terminal (Etapa 19)

- Perfil novo `Terminal_Local_Volume.asset`: Bloom 1.15 (threshold 0.85), Vignette 0.26, exposure −0.12, contrast +8, filtro frio ciano leve. **Sem chromatic aberration** (a spec diz "talvez, apenas durante corrupção" — deixado de fora nesta passada).
- Objeto `Terminal Local Volume`: **`isGlobal=false`**, BoxCollider trigger cobrindo a câmara (z 2580–2760, 40×26 m), `blendDistance=18` (transição suave), `priority=20` (> global 10).

**Validado em play nos dois sentidos:**
| Câmera | Vignette efetivo no stack | Bloom efetivo |
|---|---|---|
| Dentro (z=2641) | **0.260** | **1.15** |
| Fora (z=1991) | 0.000 | 0.00 |

O volume influencia **apenas** a câmara do Terminal; menu e setores anteriores intactos.

## 4. Cutscene final (Etapa 22)

`FinalCutsceneController` ganhou a corrotina `DimOnFinalNao()` — **nada da cutscene foi refeito**:
- Observa `VoiceLinePlayer.CurrentLineId` em paralelo (sem alterar o sistema de voz).
- Quando **ELI_010 ("Não...")** começa: luz ambiente decai a 25% em 2,2 s (SmoothStep), o `coreLight` esmorece a 20% e as partículas do núcleo **param de emitir** (as vivas morrem sozinhas). `TubeCorePulse` é congelado para o fade não ser sobrescrito.
- Teto de segurança de 120 s: se a fala nunca vier, a corrotina expira sem travar nada.
- Caminho de fallback (sem banco de voz) não escurece — aceitável, o banco está instalado.

## 5. Testes e ressalvas

| Item | Estado |
|---|---|
| Waveform corrompida irregular | ✅ medido em play |
| Glitch de status/nome | ✅ já existia (Round 13), revalidado por leitura |
| CoreEnergy sincronizado ciano↔vermelho | ✅ 2 amostras em fases distintas do ciclo |
| Zona Núcleo ativa/inativa por Z | ✅ |
| Volume local dentro/fora | ✅ valores exatos do perfil no stack |
| Console sem erros | ✅ |
| **Cutscene final completa em play** (dim no "Não...") | ⚠️ **não executada** — o hook foi validado por código e o gatilho (`CurrentLineId`) é API existente; rodar a cutscene inteira exige a corrida completa |
| Transição da CelestIA (Setor D, vídeo Celestia02/blackout) | ✅ já existia; não alterada |
| Pendências antigas que continuam | shake × pause/morte/cutscene; perseguição completa em play; Setor E em play |

## 6. Fora do escopo (Onda 4)

Qualidade configurável (Low/Medium/High), testes completos da Etapa 30, profiling final, builds Windows/Linux, relatório final consolidado, remoção de excessos apontados pelo cliente.
