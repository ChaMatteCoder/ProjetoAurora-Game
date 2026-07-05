# HUD Character Videos — Relatório Final (Round 5)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity` (salva) · Console: 0 erros (só warnings benignos de "Color primaries" do WindowsMediaFoundation)
Docs: `HudCharacterVideos_Audit.md`, `HudCharacterVideos_Implementation.md`

## 1. Assets movidos (raiz limpa ✓)
- `IconDrElias_normal/nervous.png` → `Assets/_ProjectAurora/Art/HUD/Portraits/DrElias/` (Sprite 2D/UI, alphaIsTransparency, sem mips, uncompressed)
- `VideoDrElias_normal/nervous.mp4` → `Assets/_ProjectAurora/Videos/DrElias/` (VideoClips 1920×1080, 8s, H.264)

## 2. Vídeos configurados
Todos H.264 16:9; áudio nunca tocado (audioOutputMode=None). CelestIA: Celestia01 (10s, loop), Celestia02 (8s, ÚNICO), Celestia03 (10s, loop) em `Assets/Videos/CelestIA/`.

## 3–4. Crop 1:1 e máscara circular
- RT 16:9 1024×576 (`RT_HudCharacterPortrait`) + RawImage QUADRADO com `uvRect (0.21875, 0, 0.5625, 1)` = crop central exato do quadrado útil; zero esticamento/achatamento/barras pretas. Crop configurável no Inspector.
- Máscara circular existente reutilizada (`Portrait Ring/Portrait Mask` com Mask) — RawImage por dentro, anel ciano por cima, sprite antigo preservado como fallback.

## 5. Celestia01/02/03 — validado em play (driver de estados)
Sequência comprovada por logs: 01 loop no início → `SetCelestIAState(Transition)` (SectorManager z1350 / narrativa) dispara 02 SEM loop, uma única vez → fala do Elias durante 02 é IGNORADA (prioridade da transformação) → fim de 02 (`loopPointReached`) → blackout 0.35s → 03 em loop, `TransitionPlayed=true`, STATUS: CORROMPIDA → novas mudanças de estado NUNCA tocam 02 de novo → falas seguintes da CelestIA usam 03.

## 6. Dr. Elias — validado em play
- Fala dele (speaker real "DR. ELIAS" das DialogueLines) → card troca: nome DR. ELIAS, accent âmbar, "Isso não deveria ser possível." → nervoso (clip nervous + BIOSINAL: ELEVADO); "Entendido." → normal (clip normal + BIOSINAL: ESTÁVEL).
- Retorno automático à CelestIA no estado correto: antes da corrupção → Celestia01/STATUS: NORMAL; depois → Celestia03/STATUS: CORROMPIDA (ambos validados).

## 7. Disparo da transformação
`AuroraGameplayHUDController.SetCelestIAState` — funil único por onde passam SectorManager (setor 3 = z1350 Transition; setor 4+ = Corrupted), NarrativeEventManager e DialogueLines com changeCelestIAState.

## 8. Como testar
1. Play via MainMenu → JOGAR (com o EDITOR EM FOCO — ver riscos).
2. Intro: card com CelestIA em vídeo; falas do Dr. Elias trocam o retrato para vídeo dele (sem achatamento/barras).
3. Card volta à CelestIA após as falas dele.
4. Correr até z1350 (setor Corredor Vermelho): Celestia02 toca uma vez → blackout → Celestia03 em loop até o fim.
5. Falas seguintes: CelestIA sempre 03; Elias pode aparecer e voltar sem resetar 03.

## Correção pós-entrega — delay de início do vídeo (bug de imersão)
**Sintoma:** ao trocar de personagem/estado, o retrato ficava com imagem parada por um instante e só depois o vídeo começava.
**Causa:** o desenho anterior usava UM VideoPlayer e chamava `Stop()`+`Prepare()` no momento da troca — `Prepare()` leva tempo para decodificar o primeiro frame, e era esse tempo que congelava a imagem a cada troca.
**Correção:** reescrito para um **pool de VideoPlayers pré-preparados** — um player + RenderTexture por clip (Celestia01/02/03 + Elias normal/nervoso), todos com `Prepare()` chamado no `Start()`. Trocar de retrato virou apenas: swap da `RawImage.texture` para a RT do slot + `Play()` (resume) do player que **já está preparado** → troca **instantânea**, validada em play (no mesmo frame da fala, o player alvo já está `prepared=true` e `playing=true`, com a textura trocada e o fallback desligado).
**Perf:** só o clip ativo decoda; os demais ficam pausados segurando o último frame (resume instantâneo). `prepareCompleted` pausa cada player não-ativo assim que prepara. Custo: 5 RenderTextures 1024×576 (~12 MB VRAM) — aceitável em desktop. Retorno à CelestIA resume o loop de onde parou (não reinicia).

## 9. Pendências / riscos
- **Playback pausa quando o app perde o foco** (comportamento do VideoPlayer no Windows; o menu tem o mesmo problema — vide `AuroraMenuVideoLoop`). Mitigado com watchdog de resume (0.5s) no controller; em teste automatizado sem foco os frames não avançam, mas estados/clips/UI foram 100% validados e o playback confirma-se com o editor focado.
- Warnings "Color primaries 0" do WMF nos 5 mp4 — cosmético (possível leve desvio de cor); re-exportar com primaries BT.709 elimina.
- Humor do Dr. Elias é inferido por texto (fallback) — metadado explícito nas DialogueLines seria o upgrade futuro.
- `CelestIAHudController` legado (não usado na cena) permanece intacto no projeto.
