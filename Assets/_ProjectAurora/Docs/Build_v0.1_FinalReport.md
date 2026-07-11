# Build v0.1 — Relatório Final

**Build:** PROJETO:AURORA — Demo Alpha v0.1
**Data de geração:** 06/07/2026
**Autor:** Matheus Fernandes
**Engine:** Unity 6000.4.10f1 (URP)

---

## 1. Identificação

| Campo | Valor |
|---|---|
| Product Name | `PROJETO AURORA - Demo Alpha v0.1` (confirmado no `app.info`) |
| Company Name | `Matheus Fernandes` |
| Version | `0.1.0-alpha` |
| Plataforma | Standalone **Windows x64** |
| Scripting Backend | **Mono2x** (não IL2CPP — decisão de estabilidade) |
| Color Space | Linear |
| Development Build | **false** (sem console de debug) |
| Executável | `ProjetoAurora_DemoAlpha_v0.1.exe` |

**Caminho de saída:**
`C:\ProjetoAurora-Game\Builds\ProjetoAurora_DemoAlpha_v0.1_Windows\`

---

## 2. Cenas empacotadas

| # | Cena | Arquivo na build |
|---|---|---|
| 0 | `Assets/_ProjectAurora/Scenes/MainMenu.unity` | `level0` |
| 1 | `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` | `level1` (+ `level1.resS`) |

Fluxo oficial **MainMenu → Beta03_Principal** preservado. Nenhuma cena legada/beta
incluída na build; nenhuma cena apagada do projeto.

---

## 3. Resultado da build

| Métrica | Valor |
|---|---|
| Resultado | **SUCESSO** |
| Tamanho total | ~1,5 GB |
| Maior asset | `sharedassets1.assets` (~559 MB) + `.resS` (~821 MB) — mídia pesada (vídeos da HUD, dublagem, modelos) |
| Erros de compilação | 0 |
| Runtime | `MonoBleedingEdge/` presente (confirma Mono) |

### Verificação de integridade da saída
- ✅ `ProjetoAurora_DemoAlpha_v0.1.exe` (launcher, ~652 KB)
- ✅ `ProjetoAurora_DemoAlpha_v0.1_Data/` (dados do jogo)
- ✅ `UnityPlayer.dll` (~35 MB)
- ✅ `UnityCrashHandler64.exe`, `dstorage.dll`, `dstoragecore.dll`, `D3D12/`, `MonoBleedingEdge/`
- ✅ `level0` (MainMenu) e `level1` (Beta03) empacotadas
- ✅ `app.info` com product/company corretos

> A build foi disparada via `BuildPipeline.BuildPlayer` diretamente (para não bloquear
> em modal `DisplayDialog`). O mesmo resultado é obtido pelo menu do editor
> **Tools → Projeto Aurora → Build → Demo Alpha v0.1 - Windows**
> (script `AuroraBuildDemoAlphaV01.cs`), que gera adicionalmente `build_result.txt`.

---

## 4. Player Settings aplicados

| Campo | Antes | Aplicado |
|---|---|---|
| Product Name | ProjetoAuroraGame | PROJETO AURORA - Demo Alpha v0.1 |
| Company Name | DefaultCompany | Matheus Fernandes |
| Version | 1.0 | 0.1.0-alpha |
| Default Screen | 1920×1080 | 1920×1080 |
| Fullscreen Mode | FullScreenWindow | FullScreenWindow |
| Scripting Backend | Mono2x | Mono2x (mantido) |
| API Compat | .NET Standard 2.0 | mantido |
| Color Space | Linear | mantido |

---

## 5. Teste manual documentado (roteiro de validação)

Roteiro para validar o executável em uma máquina Windows. Marcar cada passo ao rodar
`ProjetoAurora_DemoAlpha_v0.1.exe`:

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir o `.exe` | Abre em janela cheia 1920×1080, sem tela preta |
| 2 | Menu principal | Vídeo do Dr. Elias em loop, música do menu, botões responsivos |
| 3 | Abrir Configurações | Sliders de volume ajustam em tempo real (%) |
| 4 | Clicar **JOGAR** | Transição para a gameplay (Beta03) sem travar |
| 5 | Abertura (intro) | Laboratório do Dr. Elias, sirene 3D, HUD oculta, hint "ESC — Pular abertura" |
| 6 | Pressionar **ESC** na intro | Pula a abertura e entra direto na gameplay |
| 7 | HUD de gameplay | Setor, integridade, distância e comunicador da CelestIA visíveis |
| 8 | Tutorial | Setas corretas + indicador "E"; ação liberada só após a fala da CelestIA |
| 9 | Mover A/D e pular ESPAÇO | Troca de faixa e pulo respondem |
| 10 | Interagir **E** nos painéis | Marcador "E" visível de longe; abre portas / desativa lasers |
| 11 | Tomar dano em obstáculo | Integridade cai; recupera com o tempo ao parar de colidir |
| 12 | Pausar com **ESC** na gameplay | Menu de pausa abre; retoma corretamente |
| 13 | Perseguição / setores finais | Robôs, colapso do Setor E, sprint final |
| 14 | Terminal Central | Luzes acendem conforme os passos; sem tela preta |
| 15 | Cutscene final | Robôs chegam só no "Não…" (ELI_010); HUD oculta |
| 16 | Áudio e vídeo | Dublagem, música, SFX e retratos em vídeo funcionando |

> Observação: a validação em Play Mode dentro do editor já cobriu os itens 5–16 nas
> rodadas R11–R16c. O teste do executável confirma que o empacotamento preserva esse
> comportamento (assets de mídia, backend Mono, fluxo de cenas).

---

## 6. Problemas conhecidos / pendências

1. `ELI_010` ("Não…") sem áudio dublado — exibido como texto no card (intencional).
2. Avisos cosméticos de *color primaries* ao carregar MP4 (não afetam o jogo).
3. Build em **Mono** (não IL2CPP) — inicialização levemente mais lenta, porém estável.
4. Tamanho de ~1,5 GB devido à mídia pesada (vídeos + dublagem + modelos).
5. Executável sem assinatura digital → aviso do SmartScreen na primeira execução
   (documentado no `README_BUILD.txt`).

---

## 7. Como reconstruir a build

**Opção A — pelo menu do editor (recomendado):**
`Tools → Projeto Aurora → Build → Demo Alpha v0.1 - Windows`
(script `Assets/_ProjectAurora/Scripts/Editor/Build/AuroraBuildDemoAlphaV01.cs`)

**Opção B — manual:**
1. `File → Build Settings…`
2. Confirmar as 2 cenas na ordem: `MainMenu` (0), `Beta03_Principal` (1).
3. Platform: **Windows / Standalone x64**; Development Build **desmarcado**.
4. `Build` → pasta `Builds/ProjetoAurora_DemoAlpha_v0.1_Windows/`.

Não separar o `.exe` da pasta `_Data`, nem mover arquivos de dentro da build.

---

**Veredito:** Demo Alpha v0.1 gerada com sucesso, íntegra e pronta para apresentação.
