# Menu Feature — Reestruturação de Código (Round 10)

## Princípio adotado

O menu era o "problema histórico" do projeto por acúmulo de controllers paralelos e
lógica duplicada. A reestruturação seguiu uma regra única: **um dono por responsabilidade**,
e o pause **não cria sistema novo** — pluga no fluxo que já existia
(`GameManager` → `UIManager.SetPause` → `hud.pausePanel`).

## Scripts novos

| Script | Namespace | Responsabilidade |
|---|---|---|
| `UI/Shared/AuroraSettingsService.cs` | (global) | Serviço **estático** de configurações. Persiste em `PlayerPrefs` (chaves `Aurora_*`), aplica em runtime: master → `AudioListener.volume`; música → `AudioManager.SetUserVolume`; voz → AudioSources do `VoiceLinePlayer`; efeitos → multiplicador estático `EffectsVolume` (consumido por `LaserHazard`); tela cheia/vsync/qualidade → `Screen`/`QualitySettings`. `ResetToDefaults()` restaura tudo. |
| `UI/Shared/AuroraSettingsApplier.cs` | (global) | MonoBehaviour mínimo: chama `ApplyAll()` no `Start`. Presente no MainMenu e na gameplay — garante que preferências valem em qualquer cena de entrada. |
| `UI/Menu/AuroraMenuSettingsController.cs` | `ProjectAurora.UI.Menu` | Liga widgets (4 sliders, 2 toggles, dropdown de qualidade, reset) ao serviço. `OnEnable` sincroniza do serviço; `OnDisable` salva. **Reutilizado sem alteração** pelo painel de settings do pause — mesma classe, duas instâncias. |
| `UI/Menu/AuroraMenuExtraController.cs` | `ProjectAurora.UI.Menu` | Painel EXTRA: hub → subpainéis SKIN/LORE (placeholders estruturados). `IsInSubpanel`/`BackToHub()` para navegação. |
| `UI/Pause/AuroraPauseMenuController.cs` | `ProjectAurora.UI.Pause` | Botões do pause. NÃO toca em ESC nem `Time.timeScale` — delega para APIs existentes do `GameManager` (`Resume`/`Restart`/`ReturnToMenu`). Confirmações via padrão `AskConfirm(msg, action)`. |

## Scripts alterados (mínimo necessário)

| Script | Mudança |
|---|---|
| `AuroraMainMenuController.cs` | `StartGame()` reescrito: guarda anti-duplo-clique → desativa cards → ativa `loadingOverlay` (CARREGANDO...) → `SceneManager.LoadSceneAsync`. Antes era load síncrono que congelava a UI ("botão travado"). Campo novo: `loadingOverlay`. |
| `AudioManager.cs` | Método novo `SetUserVolume(float)`: define o volume-base do usuário **preservando a proporção atual** (ex.: se a redução narrativa de 0.15 está ativa, ela continua proporcional). |
| `LaserHazard.cs` | `PlayRandom` agora multiplica por `AuroraSettingsService.EffectsVolume` (slider Efeitos funcional). |
| `AuroraGameplayHUDController.cs` | Nenhuma mudança neste round — o campo `pausePanel` já existia e virou o ponto de acoplamento do pause novo. |

## O que NÃO foi alterado (de propósito)

- `GameManager` — continua o único dono de ESC, `Time.timeScale` e pausa de áudio (`TogglePause`).
- `UIManager` — fluxo `SetPause` intacto.
- Qualquer código de gameplay, tutorial, inimigos, dublagem, GameOver, terminal.

## Controllers duplicados / código morto

- `AuroraMainMenuController` segue sendo o ÚNICO controller do menu (verificado: nenhum outro script conectado na cena).
- O placeholder antigo "Pause Panel" da HUD foi renomeado para `Pause Panel (Legacy)` e desativado — não deletado (política do projeto), não referenciado por nada.
- 5 `Text` legados desativados ("Label") removidos da hierarquia do MainMenu (eram filhos mortos sem referência).

## Limitação conhecida e aceita

ESC enquanto um subpainel do pause está aberto (Settings/Confirm) **retoma o jogo diretamente**
(o `GameManager` processa ESC antes do painel). O `OnDisable` do `AuroraPauseMenuController`
fecha os subpainéis, então nunca fica estado órfão. Comportamento documentado e testado.
