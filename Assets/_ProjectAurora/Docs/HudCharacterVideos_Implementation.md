# HUD Character Videos — Implementação (Round 5)

Data: 2026-07-02 · Script: `Assets/_ProjectAurora/Scripts/UI/HUD/HudCharacterVideoPortraitController.cs`

## Arquitetura
- **1 VideoPlayer** (`HUD Canvas/CelestIA Communication/HudPortraitVideoPlayer`): playOnAwake off, **audioOutputMode=None** (nunca toca áudio), renderMode=RenderTexture, waitForFirstFrame, skipOnDrop.
- **RenderTexture 16:9** `RT_HudCharacterPortrait` (1024×576) — preserva o aspecto do vídeo; nada é achatado.
- **RawImage `CharacterVideo`** dentro da máscara circular existente (`Portrait Ring/Portrait Mask`): RectTransform QUADRADO (184×184) + **uvRect (0.21875, 0, 0.5625, 1)** = crop central 1:1 do 16:9 → sem barras pretas, sem distorção. Crop configurável no Inspector (`centerCrop16x9To1x1`). Raycast off. Anel ciano permanece por cima.
- Sprite antigo `CelestIA Portrait` mantido como **fallback** (controller troca sprite conforme o personagem e ativa se o vídeo falhar).

## Máquina de estados
- `CurrentSpeaker` (CelestIA/DrElias), `CelestiaState` (Normal/Transitioning/Corrupted), `TransitionPlayed` (guard permanente).
- **CelestIA**: Normal→Celestia01 loop · `PlayCelestIATransitionOnce()`→Celestia02 SEM loop, uma única vez · fim (evento `loopPointReached`)→**blackout 0.35s** (retrato desliga)→Celestia03 loop permanente. Replays de 02 impossíveis (guard + estado Transitioning não reentra).
- **Dr. Elias**: fala dele → clip normal/nervoso em loop + nome "DR. ELIAS" + status "BIOSINAL: ESTÁVEL/ELEVADO" + accent âmbar; retorno automático à CelestIA (estado correto: 01 ou 03) após 3.5s sem nova fala dele, ou imediatamente quando outra origem fala.
- **Prioridade**: Transitioning > DrElias > CelestIA — fala do Elias durante Celestia02 NÃO interrompe a transformação (validado).

## Identificação do falante
- `AuroraGameplayHUDController.SetDialogue(speaker, message)` já recebia o speaker real das `DialogueLine` → `SetSpeakerFromDialogue`. Zero heurística para identificar o falante.
- **Humor** (sem metadado): fallback textual — termos de tensão (cedendo, energia, desligado, chance, painel, devia/deveria, não, agora, oscilação, "?") → nervoso; padrão normal.
- Transformação: `SetCelestIAState(Transition|Corrupted)` (SectorManager z1350/z1800, NarrativeEventManager, DialogueLines) → `OnCelestIAStateChanged` → transição única.

## Robustez (aprendido em teste)
- `prepareTimeout` 5s (2s estourava no load da cena).
- **Watchdog de resume**: o VideoPlayer pausa sozinho quando o app/editor perde o foco do SO (mesmo comportamento que motivou o `AuroraMenuVideoLoop` do menu); o controller retoma `Play()` a cada 0.5s se deveria estar tocando. Cobre alt-tab em builds.
- Fim do clipe de transição detectado por `loopPointReached` (nunca por polling de `isPlaying`, que dispararia blackout prematuro num alt-tab).
- Falha/timeout → sprite fallback + warning; nunca círculo preto permanente.

## Integração (mínima)
- `AuroraGameplayHUDController`: +campo `characterPortrait`; `SetDialogue` chama o portrait e deixa de prefixar "DR. ELIAS:" no texto (o card já identifica o falante); `SetCelestIAState` notifica o portrait. Nada mais mudou; HUD não duplicada.
