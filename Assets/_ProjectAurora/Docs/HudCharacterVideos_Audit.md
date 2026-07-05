# HUD Character Videos — Auditoria (Round 5)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity`

## Retrato atual
- `HUD Canvas/CelestIA Communication/Portrait Ring/Portrait Mask` — Image + **Mask** (sprite circular, Show Mask Graphic) contendo `CelestIA Portrait` (Image, sprite estático `CelestiaNormal.png`).
- A máscara circular JÁ EXISTE e funciona → reutilizar; o RawImage do vídeo entra dentro dela; o sprite atual vira fallback.
- Moldura/anel ciano é o `Portrait Ring` (fica acima — preservado).

## Fluxo de mensagens (speaker metadata EXISTE)
`DialogueLine(speaker, message, duration)` → `DialogueManager.PlayRoutine` → `UIManager.SetDialogue(speaker, message)` → `AuroraGameplayHUDController.SetDialogue(speaker, message)` → `CelestIACommPanel.SetMessage(...)`.
- **O speaker real ("CELESTIA" / "DR. ELIAS") chega até o HUD controller** — nenhuma inferência por prefixo é necessária para identificar o falante. O texto de falas de terceiros é prefixado no content ("DR. ELIAS: ..."), comportamento mantido.
- Falas da CelestIA que começam com "Doutor Elias," continuam sendo CelestIA (speaker correto na origem) — risco de falso positivo eliminado por design.
- Humor do Dr. Elias NÃO tem metadado → inferência textual como fallback (termos de tensão), padrão = normal.

## Mudança de estado da CelestIA (Normal → Corrompida)
- `SectorManager.UpdateSector`: setor ≤2 Normal, setor 3 **Transition** (z1350), setor ≥4 **Corrupted** (z1800) → `ui.SetCelestIAState(state)` → `AuroraGameplayHUDController.SetCelestIAState` → `CelestIACommPanel.SetState` (status OSCILANDO/CORROMPIDA + accent).
- `DialogueLine.changeCelestIAState` e `NarrativeEventManager.Queue(state, ...)` também setam estado pelo mesmo funil.
- **Ponto único de integração**: `AuroraGameplayHUDController.SetCelestIAState` — dispara `PlayCelestIATransitionOnce()` na primeira Transition/Corrupted.

## CelestIACommPanel
- `nameText`, `statusText`, `messageText`, `portraitImage`, `signalIcon`, `waveformBars[]` públicos; `SetState/SetStatus/SetAccent` prontos. Waveform anima em Update (intocado).

## Vídeos
- CelestIA: `Assets/Videos/CelestIA/Celestia01/02/03.mp4` (existentes).
- Dr. Elias: movidos da raiz → `Assets/_ProjectAurora/Videos/DrElias/VideoDrElias_normal|nervous.mp4`; ícones → `Assets/_ProjectAurora/Art/HUD/Portraits/DrElias/`.
- Todos 16:9 com conteúdo 1:1 central → crop via `RawImage.uvRect = (0.21875, 0, 0.5625, 1)`, RT 16:9 (1024×576) para não achatar.
- `CelestIAHudController` legado (VideoPlayer 3 clips) existe como script mas NÃO está na cena — o novo controller o substitui em escopo; não será usado.

## Alterações planejadas
1. Importers: PNGs→Sprite (sem mip, alpha), vídeos com player em audioOutputMode=None.
2. `RT_HudCharacterPortrait.renderTexture` (1024×576).
3. Novo `HudCharacterVideoPortraitController` (Scripts/UI/HUD): 1 VideoPlayer, estados CelestIA (01 loop → 02 uma vez → blackout → 03 loop, guard contra replay), Dr. Elias normal/nervoso com retorno automático, prioridade Transição > Elias > CelestIA, fallback por sprite com timeout.
4. `AuroraGameplayHUDController`: +campo `characterPortrait` + 2 chamadas (SetDialogue, SetCelestIAState). Nada mais muda.
5. Cena: RawImage dentro do Portrait Mask + VideoPlayer + wiring de clips/sprites/refs.
