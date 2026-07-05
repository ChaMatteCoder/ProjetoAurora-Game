# Menu Feature — Auditoria (Round 10)

Data: 2026-07-03 · Cena: `MainMenu.unity`

## Estado real (melhor que o histórico sugeria)
Limpezas anteriores deixaram a base sã: **1 Canvas** (`Canvas_MainMenu`), **1 EventSystem**, **1 VideoPlayer**, **0 missing scripts**, **1 controller ativo** (`AuroraMainMenuController` no Canvas, wiring completo: cardsContainer + 3 painéis), legado em `Legacy_MenuVisuals` (desativado). Controllers legados (`MainMenuController` ×2 em pastas antigas) existem como ARQUIVO mas **não estão na cena**.

## Problemas encontrados
1. **JOGAR trava**: `StartGame()` usa `SceneManager.LoadScene` **síncrono** — a Beta03 é pesada e o load congela a UI por segundos (o "delay" percebido). Não há delay artificial em código; é o load bloqueante sem feedback. → LoadSceneAsync + overlay "CARREGANDO" + botões desabilitados.
2. **Painéis são cascas**: Settings/Extra/Credits têm apenas Title/Body (Text legado, não TMP) + Voltar. Nada funcional.
3. **Sem persistência**: nenhum sistema de configurações (não há AudioMixer no projeto; música = AudioSource do AudioManager `volume=0.5`; voz = VoiceLinePlayer; SFX = AudioSources dispersos ex. LaserHazard).
4. **Sem pause real na gameplay**: GameManager.TogglePause existe (ESC, timeScale, `ui.SetPause`) mas o "Pause Panel" da HUD é placeholder (1 Message). Sem reiniciar/menu/sair/settings.
5. **Escala/peso**: ícones 42×42 (pequenos p/ cards de 76 de altura), filhos `Label`(Text) mortos desativados em cada card, RT do vídeo em **1920×1080** (pode ser 1280×720), 22 Images com raycast ligado (maioria decorativa).
6. Cards com hover ok (AuroraMenuCard: ColorTint + swap de sprite, sem animações caras) ✓.

## Scripts do menu
- **Oficial**: `UI/Menu/AuroraMainMenuController.cs` (autoridade, limpo), `AuroraMenuCard.cs` (ok), `AuroraMenuVideoLoop.cs` (watchdog do vídeo — manter).
- **Legados (arquivo apenas, fora da cena)**: `UI/MainMenuController.cs` (166L), `Assets/Scripts/MainMenuController.cs` (58L) — não referenciados por cena; manter no disco, documentados.

## Plano
- Código: `Shared/AuroraSettingsService` (PlayerPrefs; master=AudioListener, música=AudioManager, voz=VoiceLinePlayer, SFX=multiplicador estático consumido pelo LaserHazard; fullscreen/vsync/qualidade), `Menu/AuroraMenuSettingsController` (reutilizado no pause), `Menu/AuroraMenuExtraController`, `Pause/AuroraPauseMenuController`; JOGAR async no controller oficial.
- Cena menu: reconstruir conteúdo dos 3 painéis (TMP), subpainéis Skin/Lore, créditos reais, ícones 56×56, raycast off em decorativos, RT 1280×720, overlay de loading.
- Gameplay: `Canvas_PauseMenu` completo plugado no fluxo EXISTENTE (`hud.pausePanel` → novo painel; GameManager segue dono de ESC/timeScale — zero mudança em código de gameplay).
- Risco controlado: ESC dentro de subpainel do pause retoma o jogo (GameManager unpausa direto) — subpainéis fecham junto; documentado como comportamento aceito.
