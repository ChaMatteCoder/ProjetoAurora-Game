# Menu Feature — Relatório Final (Round 10)

**Veredito: APROVADO.** Menu reestruturado, JOGAR instantâneo (async + overlay),
configurações funcionais e persistentes compartilhadas com o pause da gameplay,
EXTRA/CRÉDITOS reais, e nenhum caminho deixa `Time.timeScale` preso em 0.

## Entregas por etapa do briefing

1. **JOGAR sem atraso artificial** — load síncrono substituído por
   `LoadSceneAsync` + overlay "CARREGANDO..." + cards desativados + guarda de duplo clique.
   Medido em play: overlay visível no mesmo frame do clique; Beta03 pronta em ~3.6 s
   (tempo real de load da cena, não delay artificial).
2. **Configurações funcionais** — 4 volumes (geral/música/efeitos/voz), tela cheia, VSync,
   qualidade, exibição de controles, restaurar padrão. Persistem via PlayerPrefs e aplicam
   em ambas as cenas (`AuroraSettingsApplier`). Detalhes: `MenuFeature_SettingsAndPause.md`.
3. **EXTRA** — hub com SKIN e LORE (subpainéis placeholder estruturados, com voltar próprio
   `Button_Retornar_*` que não colide com o bind global de VOLTAR do controller principal).
4. **CRÉDITOS** — texto real: Matheus Fernandes, disciplina de Computação Gráfica,
   ferramentas (Unity/C#/ElevenLabs etc.).
5. **SAIR** — funcional (EditorApplication no editor / Application.Quit em build).
6. **Escala e peso do menu** — RT do vídeo de fundo 1920×1080 → 1280×720;
   42 imagens decorativas com raycast desligado; 5 textos mortos removidos;
   visual legado agrupado inativo em `Legacy_MenuVisuals`.
7. **Pause na gameplay** — ESC abre `PauseMenu_Root` com Continuar / Configurações /
   Reiniciar (confirma) / Menu (confirma) / Sair (confirma), plugado no fluxo existente
   (`hud.pausePanel`); zero mudança em código de gameplay.
8. **Sem duplicação** — `AuroraMainMenuController` é o único controller do menu;
   `AuroraMenuSettingsController` é a única lógica de settings (2 instâncias, 1 classe);
   placeholder antigo "Pause Panel (Legacy)" desativado, não referenciado.

## Validação em play (driver automatizado, logs [R10V])

| Teste | Resultado |
|---|---|
| Painel Settings abre, 4 sliders + 3 toggles presentes | ✅ |
| Slider Geral 1.0 → 0.5 aplica em `AudioListener.volume` | ✅ (0.5 medido) |
| EXTRA abre, botão SKIN entra no subpainel (`IsInSubpanel=true`) | ✅ |
| CRÉDITOS abre | ✅ |
| JOGAR: overlay CARREGANDO ativo + load async | ✅ (Beta03 em 3.6 s) |
| ESC na gameplay: painel abre, `state=Paused`, `timeScale=0` | ✅ |
| Settings no pause: abre, master 0.7 aplicado | ✅ |
| CONTINUAR: `state=Playing`, `timeScale=1`, painel fechado | ✅ |
| REINICIAR: confirmação com texto correto → SIM → cena recarrega, `timeScale=1` | ✅ |
| VOLTAR AO MENU: chega no MainMenu com `timeScale=1` | ✅ |
| Console sem erros novos (só avisos pré-existentes de color primaries dos vídeos) | ✅ |

Intocados conforme o briefing: gameplay, tutorial, inimigos/perseguição, dublagem
(VoiceLinePlayer), GameOver, terminal/cutscene final.

## Pendências / observações

- ESC com subpainel do pause aberto retoma o jogo direto (limitação aceita e documentada;
  subpainéis fecham sozinhos via `OnDisable` — sem estado órfão).
- SKIN e LORE são placeholders estruturados aguardando conteúdo futuro.
- Slider Efeitos hoje alcança os SFX dos lasers (`LaserHazard`); novos sistemas de SFX
  devem multiplicar por `AuroraSettingsService.EffectsVolume`.

## Hotfix R10b — interatividade + redesign (pós-feedback do playtest)

**Bug raiz encontrado:** os painéis (Settings/Extra/Créditos) estavam ANTES do
`MenuButtonsPanel` na hierarquia — em Unity UI, irmãos posteriores desenham por cima
e recebem o raycast primeiro. Resultado: os cards do menu ficavam visualmente na frente
do painel aberto E roubavam todos os cliques ("não consigo ajustar nada").

**Correções:**
1. **Reordenação**: `Video_Background(0) → MenuButtonsPanel(1) → Panel_Settings(2) →
   Panel_Extra(3) → Panel_Credits(4) → Legacy(5) → Loading_Overlay(último)`.
   Painéis agora desenham acima dos cards.
2. **Scrim modal**: cada painel virou full-screen com fundo escuro translúcido que
   bloqueia raycast — o menu atrás escurece (modal de verdade) e não recebe cliques.
3. **Redesign completo** (identidade Aurora: ciano #0DE0FF, cantos acentuados,
   moldura fina, watermark "AURORA // SISTEMA"):
   - Settings: colunas ÁUDIO/VÍDEO, sliders com trilho fino + fill com glow + handle
     losango + hit-area invisível de linha inteira, % ao vivo (`AuroraSliderValueLabel`,
     script novo), toggles com moldura, dropdown escuro, chips de teclas nos CONTROLES.
   - Extra: hub com 2 cartões grandes (SKIN/LORE) com ícone, descrição e tag "EM BREVE";
     subpainéis com texto de lore.
   - Créditos: layout tipográfico (título, autor, disciplina, ferramentas).
   - Pause (Beta03): mesmos padrões — Main_Panel, Settings_Panel (idêntico ao do menu)
     e Confirm_Panel reconstruídos e re-conectados ao `AuroraPauseMenuController`.

**Validação (drivers [R10B]/[R10BP], raycast simulado por EventSystem.RaycastAll):**
- Todos os widgets do settings do menu recebem o raycast no topo (handle, hit-area,
  toggle, dropdown, voltar, reset) ✅
- Card JOGAR corretamente BLOQUEADO pelo scrim com painel aberto; liberado ao fechar ✅
- Clique real (ExecuteEvents) em toggle alterna; slider 0.6 → AudioListener 0.6 + label 60% ✅
- Extra: clique em SKIN abre subpainel, RETORNAR volta ao hub ✅
- Pause com timeScale=0: raycasts OK, slider 0.45 aplica + label 45% ✅
- CONTINUAR: Playing, timeScale=1 ✅
- Screenshots: `Assets/Screenshots/r10b_*` (settings/extra/créditos/pause)

**Infra:** watchdog `Assets/Editor/McpBridgeWatchdog.cs` religa a bridge MCP após
domain reload (a bridge não subia sozinha após reiniciar o editor).

## Documentos do round

1. `MenuFeature_Audit.md` — auditoria inicial (causa do delay do Jogar, mapa da cena).
2. `MenuFeature_CodeRestructure.md` — scripts novos/alterados e o que ficou intocado.
3. `MenuFeature_SettingsAndPause.md` — especificação de settings + pause + garantias de timeScale.
4. `MenuFeature_FinalReport.md` — este relatório.
