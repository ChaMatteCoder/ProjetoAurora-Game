# Menu Feature — Configurações e Pause (Round 10)

## Configurações (compartilhadas entre menu e gameplay)

### Onde vivem
- **Estado/persistência**: `AuroraSettingsService` (estático, `PlayerPrefs`, chaves `Aurora_*`).
- **UI do menu**: `Panel_Settings` no MainMenu, com `AuroraMenuSettingsController`.
- **UI do pause**: `Settings_Panel` dentro do `PauseMenu_Root` na Beta03, com **outra instância
  da mesma classe** `AuroraMenuSettingsController` — zero duplicação de lógica.
- **Aplicação na entrada da cena**: `AuroraSettingsApplier` (MainMenu: Canvas_MainMenu;
  Gameplay: HUD Canvas) chama `ApplyAll()` no Start.

### Itens

| Item | Widget | Efeito em runtime | Persistência |
|---|---|---|---|
| Volume Geral | Slider 0–1 | `AudioListener.volume` | `Aurora_MasterVolume` |
| Volume Música | Slider 0–1 | `AudioManager.SetUserVolume` (gameplay) e AudioSource `Audio_MenuMusic` (menu, razão ×0.69 preservada) | `Aurora_MusicVolume` |
| Volume Efeitos | Slider 0–1 | `AuroraSettingsService.EffectsVolume` — multiplicador lido pelos SFX (`LaserHazard`) | `Aurora_SfxVolume` |
| Volume Voz | Slider 0–1 | AudioSources filhos do `VoiceLinePlayer` (dublagem) | `Aurora_VoiceVolume` |
| Tela cheia | Toggle | `Screen.fullScreenMode` | `Aurora_Fullscreen` |
| VSync | Toggle | `QualitySettings.vSyncCount` | `Aurora_VSync` |
| Qualidade | Dropdown (nomes reais de `QualitySettings.names`) | `QualitySettings.SetQualityLevel` | `Aurora_Quality` |
| Controles | Texto informativo (A/←, D/→, Espaço, E, ESC) | — | — |
| Restaurar padrão | Botão | `ResetToDefaults()` + re-sync dos widgets | limpa para defaults |

`OnEnable` do controller sincroniza widgets ← serviço (flag `syncing` evita eco);
`OnDisable` chama `Save()`. Mudar um slider aplica **imediatamente** (ouvível com o jogo pausado
no caso do master, pois `AudioListener.volume` independe de `timeScale`).

## Pause (gameplay)

### Arquitetura — plugado, não paralelo

```
ESC → GameManager.TogglePause()          (código EXISTENTE, dono do timeScale/áudio)
      └→ UIManager.SetPause(true)
          └→ AuroraGameplayHUDController.SetPause
              └→ hud.pausePanel.SetActive(true)   ← agora aponta para PauseMenu_Root
```

Nenhuma linha de código de gameplay mudou para o pause funcionar.
`Time.timeScale` é restaurado pelos caminhos já validados do `GameManager`
(`Resume`, `Restart`, `ReturnToMenu` — todos setam 1). O botão SAIR seta
`Time.timeScale = 1f` explicitamente antes de sair (higiene para domain reload no editor).

### Hierarquia (Beta03_Principal → HUD Canvas → PauseMenu_Root, inativo por padrão)

- **PauseMenu_Root** — Image escura full-screen (0.88 alpha, bloqueia raycast p/ HUD)
  + `AuroraPauseMenuController`
  - **Main_Panel** — título PAUSA + botões: CONTINUAR · CONFIGURAÇÕES · REINICIAR CORRIDA ·
    VOLTAR AO MENU PRINCIPAL · SAIR DO JOGO
  - **Settings_Panel** (inativo) — 4 sliders + 2 toggles + dropdown qualidade + controles +
    RESTAURAR PADRÃO + VOLTAR (`Button_RetornarPause` → `ShowMain`)
  - **Confirm_Panel** (inativo) — texto dinâmico + SIM / NÃO

REINICIAR, VOLTAR AO MENU e SAIR pedem confirmação (`AskConfirm`); NÃO volta ao painel principal.

### Garantias de timeScale (testadas em play)

| Fluxo | timeScale ao final |
|---|---|
| ESC → CONTINUAR | 1 (log: `CONTINUAR: state=Playing timeScale=1`) |
| ESC → REINICIAR → SIM | 1 após reload (log: `RESTART OK ... timeScale=1`) |
| ESC → MENU → SIM | 1 no MainMenu (log: `VOLTAR AO MENU OK ... timeScale=1`) |
| ESC → ESC (toggle) | 1 (caminho existente do GameManager, inalterado) |
| SAIR | 1 setado antes do quit |

Não existe caminho que deixe `Time.timeScale = 0` preso.
