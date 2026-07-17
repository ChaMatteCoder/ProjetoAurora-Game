# FinalVFX — Relatório Final da Rodada de VFX

**Projeto:** PROJETO:AURORA — Falha de Contenção · **Cena:** `Beta03_Principal.unity` · **Data:** 2026-07-16
**Ondas executadas:** 0 (baseline) → 1 (feedback) → 2 (obstáculos/ambiente) → 3 (CelestIA/Terminal/cutscene) → 4 (fechamento).
Documentos-irmãos: `FinalVFX_Audit.md` · `_PerformanceBaseline.md` · `_Wave1_Report.md` · `_SectorEffects.md` · `_TerminalAndCutscenes.md` · `_PerformanceReport.md` · `_GameplayFeedback.md`.

---

## 1–4. Efeitos, prefabs, materiais, texturas e shaders criados

**Scripts (7):** `AuroraCameraFeedbackController`, `AuroraVFXPool`, `AuroraVFXController`, `AuroraSectorVFXController`, `AuroraMaterialPulseController`, `AuroraPromptPulse` + extensão do `TubeCorePulse` (`coreEnergy`).

**Prefabs (12):** PlayerDamage, SuitRecovery(loop), CollectBurst, DigitalScan, InteractionPulse, AmbientSparks, Steam, AmbientDust, CorruptionMotes, LaserShutdown, DoorDust, CoreEnergy(loop, tingível). Todos: 9–60 partículas máx., sem luz/colisão/trail/noise/sombra.

**Materiais (6):** Additive_Cyan/Red, Spark, Particle_White, DigitalScan, Smoke_Soft — compartilhados, HDR serializado, URP Particles/Unlit.
**Texturas (3):** SoftCircle, Spark, DigitalLine — 128², procedurais, geradas por script.
**Shaders novos: 0** (regra cumprida — só shaders oficiais URP). **Point Lights novas: 0.**

## 5. Efeitos por setor (ativação por Z, 5 zonas)

A: limpo (tutorial) · B: faíscas+vapor · C: vapor+faíscas · D: corrupção vermelha+faíscas · E: poeira+faíscas · Núcleo: energia do núcleo tingível. Máx. 1 zona ativa por vez (validado).

## 6–13. Sistemas (tabela em `FinalVFX_GameplayFeedback.md`)

Dano ✅ · Suit Recovery (4 fases) ✅ · AuroraCoin + pulso HUD ✅ · DataFile scan próprio ✅ · Interação E (UI pulsando + confirmação no mundo) ✅ · Lasers (shutdown) ✅ · Portas (poeira) ✅ · Robôs (MPB vermelho, obstáculos + perseguidores com Lead mais forte, culling 55 m) ✅.

## 14. CelestIA corrompida

Glitch de status/nome **já existia** (Round 13) — preservado. Adicionada a **waveform irregular** (picos/dropouts por ruído) só no estado corrompido. Leitura das falas intacta; layout intacto.

## 15. Terminal Central

`PF_VFX_CoreEnergy` dentro do núcleo, **sincronizado por construção** ao `TubeCorePulse` (cor + taxa 10→22 com a instabilidade). Só o núcleo interno muda de estado — carcaça intocada (regra do cliente). Braços robóticos: omissão deliberada (rastreamento por bone = custo/risco alto), registrada.

## 16. Cutscenes

- Perseguição: shake na ativação dos robôs; impacto único (shake+poeira+faíscas) quando o gate assenta.
- Final: `DimOnFinalNao()` — no "Não..." (ELI_010), ambiente cai a 25%, núcleo esmorece, partículas param. Observa `CurrentLineId` sem alterar o sistema de voz; teto de segurança de 120 s.
- Intro: **não alterada** (Etapa 20 não executada — ver §21).

## 17. Pooling

Pool por prefab, pré-cria 4, teto 24, reciclagem automática, `ReleaseAll()` na morte. Validado: rajada de 12 → 0; 22 instâncias reusadas; nunca estourou.

## 18–19. Otimizações e performance

Ver `FinalVFX_PerformanceReport.md`. Resumo: 0 sistemas tocando em repouso; +18 Renderers no total; custo ambiental isolado ≈ 1,9 ms (Editor); sem regressão no feedback de gameplay (~11,3 ms vs baseline ~12,1 ms).

**Bugs reais encontrados pelos testes (e corrigidos):**
1. `PF_VFX_SuitRecovery` one-shot vs recuperação contínua → referência pendurada no pool (Onda 1).
2. `VolumeProfile` criado sem `AddObjectToAsset` → overrides sumiam no reload de domínio (Onda 4). *Só apareceu porque a revalidação pós-reload foi feita.*

## 20. Compatibilidade Windows/Linux

- **Windows**: build final regenerada nesta onda (ver resultado na resposta da sessão); Dev Build da Onda 1 abriu sem exceções.
- **Linux**: build final regenerada nesta onda. **Não executada em máquina Linux** — compatibilidade verificada por build bem-sucedida + shaders exclusivamente URP oficiais (sem risco de rosa por shader custom).
- Sem `AssetDatabase`/APIs de Editor em runtime; sem paths absolutos; materiais/keywords serializados em assets referenciados.

## 21. Pendências (honestas)

1. **Cutscene final e perseguição completas em play** — hooks validados por código; a sequência inteira precisa de uma corrida real (validação do cliente).
2. **Shake × pause/morte/cutscene** — coexistência nunca isolada em teste dedicado (design é defensivo: remove offset todo Update).
3. **Etapa 20 (polish da cutscene inicial)** — não executada; intro intacta.
4. **Pulso contínuo dos feixes de laser ativos** — feixes seguem com emissivo estático; melhoria opcional.
5. **Qualidade configurável** — API pronta e documentada; integração ao menu adiada por risco.
6. **Gameplay dentro das builds** — smoke test de abertura+log apenas.
7. **Ajuste artístico fino** — cores/escalas aprovadas pelo cliente nas validações das ondas 1–3; refinements ficam a critério dele.

## Critérios de aceite (32 da spec original)

Cumpridos: 1–11, 13–19 (16: só núcleo muda ✅; 17: volume local ✅), 21–29 (25 materiais compartilhados ✅; 26 MPB validado e usado ✅; 27 pooling ✅), 31 (console limpo), 32 (relatórios).
Parciais: 12 (Setor D tem corrupção/alarme herdado + motes novos; "bem trabalhados" é juízo do cliente), 20 (cutscene inicial funciona, mas polish da Etapa 20 não foi feito), 30 (builds regeneradas; Linux não executada em SO real).
