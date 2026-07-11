# Build v0.1 — Auditoria Pré-Build

**Build:** PROJETO:AURORA — Demo Alpha v0.1 · **Data:** 06/07/2026 ·
**Plataforma alvo:** Standalone Windows x64

## Cenas incluídas no Build Settings
| # | Cena | Enabled |
|---|---|---|
| 0 | `Assets/_ProjectAurora/Scenes/MainMenu.unity` | ✅ |
| 1 | `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` | ✅ |

Fluxo oficial `MainMenu → Beta03_Principal` — correto e já configurado.

## Cenas removidas do Build Settings
Nenhuma. O Build Settings **já continha apenas as 2 cenas canônicas** — nenhuma cena
legada/beta estava incluída, portanto nada foi removido. Todas as cenas antigas/betas
permanecem intactas no projeto (não versionadas para a build, não apagadas).

## Player Settings (antes → alvo)
| Campo | Antes | Alvo (Etapa 2) |
|---|---|---|
| Product Name | ProjetoAuroraGame | PROJETO AURORA - Demo Alpha v0.1 |
| Company Name | DefaultCompany | Matheus Fernandes |
| Version | 1.0 | 0.1.0-alpha |
| Default Screen | 1920×1080 | 1920×1080 (mantém) |
| Fullscreen Mode | FullScreenWindow | FullScreenWindow (seguro, mantém) |
| Run In Background | False | False (mantém) |
| Scripting Backend | Mono2x | Mono2x (NÃO alterar — estável) |
| API Compat | .NET Standard 2.0 | mantém |
| Color Space | Linear | mantém |
| Build Target | StandaloneWindows64 | mantém |

## Erros encontrados
Nenhum erro de compilação, nenhum Missing Script, nenhuma referência quebrada.

## Warnings relevantes
- `Color primaries 0 is unknown or unsupported by WindowsMediaFoundation` — informativo,
  emitido ao carregar os MP4 (CelestIA 01/02/03, DrElias normal/nervous). Não afeta a
  build; é metadado de cor dos vídeos. Sem impacto funcional.

## Assets críticos verificados
| Área | Item | Status |
|---|---|---|
| Menu | AuroraMainMenuController | ✅ presente |
| Menu | Audio_MenuMusic (clip) | ✅ |
| Menu | VideoPlayer_DrEliasLoop (clip `Dr.Elias_Loop`) | ✅ |
| Gameplay | Missing scripts (MainMenu / Beta03) | ✅ 0 / 0 |
| Gameplay | VoiceLineDatabase | ✅ 67 falas |
| Gameplay | Clips de dublagem em `Audio/Voice/Dublagem` | ✅ 66 clips |
| Gameplay | Retratos em vídeo (CelestIA/Dr. Elias) | ✅ referenciados |
| Gameplay | GameManager: player/ui/tutorial/intro/final/gameOver | ✅ todos setados |

## Referências externas / paths absolutos
Varredura em todos os `.cs` (runtime): **nenhuma** referência a `C:\ProjetoAurora-Game`,
raiz temporária ou pasta `Dublagem` fora de `Assets/`. Todo asset usado está dentro de
`Assets/`. (Os avisos de color-primaries citam o caminho do MP4 apenas na mensagem de log,
não é dependência de código.)

## Riscos antes da build
1. **Vídeo do menu (`Dr.Elias_Loop`)** e alguns MP4 de WIP estão fora do versionamento
   (gitignore de assets pesados). Isso **não afeta esta build local** (os arquivos existem
   no disco e serão incluídos), mas um clone limpo do repositório precisaria deles para
   reconstruir o menu com vídeo.
2. `ELI_010` ("Não...") não tem áudio dublado — aparece como texto no card (esperado).
3. Avisos de color-primaries reaparecem em runtime ao tocar vídeos (cosmético).
4. Build em Mono (não IL2CPP) — decisão de segurança para build rápida/estável de demo.

**Veredito:** projeto apto para gerar a Demo Alpha v0.1 sem riscos bloqueantes.
