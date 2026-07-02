# Intro Cinematográfica — Jogo de Câmeras (Round 2)

Data: 2026-07-02 · Script: `Assets/Scripts/IntroCutsceneController.cs` (estendido — sem sistema paralelo)

## Antes
Um único shot estático 3/4 do player durante ~15s de diálogo, depois restore de 1s.

## Depois — sequência de 4 shots
| Shot | Conteúdo | Câmera | Duração |
|---|---|---|---|
| 01 Establishing | Corredor do laboratório, vitrines ciano, obstáculos ao fundo | Dolly elevada lateral esquerda (−5, 4.2, 24) → (−3.5, 3.2, 17), olhando pista adiante | 3.6s |
| 02 Close no Dr. Elias | Rosto/corpo (material novo fosco) | Push-in frontal 3/4 (1.8, 1.78, 2.9) → (1.25, 1.62, 2.3) | 4.2s + espera do diálogo |
| 03 Detalhe do perigo | Gabinete elétrico rompido do tutorial (z22) sob **alerta vermelho** + sirene | Push-in baixo (−2.6, 1.7, 16.5) → (−2.05, 1.42, 18.3) | 2.5s + diálogo |
| 04 Retorno | Lerp suave para a câmera de runner; CameraFollow reativado; tutorial inicia | SmoothStep 1s | 1s |

- Diálogos originais preservados, tocando **em paralelo** aos shots (callback `onComplete` em vez de bloqueio).
- Posições relativas ao player; interpolação SmoothStep com drift lento (dolly) em cada shot; câmera nunca cruza paredes (|x| ≤ 5, dentro do corredor de 7u de meia-largura).

## Skip
- **Esc** pula a intro inteira (interrompe shots + `DialogueManager.StopCurrent()` + vai direto ao restore).
- Space/Enter continuam pulando linha a linha (comportamento existente do DialogueManager, preservado).
- Sem `Time.timeScale = 0`; input do player bloqueado durante toda a intro (`SetInputEnabled(false)` + estado `IntroCutscene` do GameManager); tutorial só recebe controle após o restore.

## Bug corrigido (crítico)
`SetAlertLighting()` tingia TODAS as luzes da cena 65% de vermelho e **nunca restaurava as cores** — só o ambient voltava. Depois da intro, a iluminação por setor da rodada 1 (ciano/azul/âmbar/vermelho) ficava permanentemente contaminada. Agora as cores originais são salvas antes do tint e restauradas no fim/skip (`RestoreLighting()`).

## Integração
- Fluxo inalterado: `GameManager.Start()` → `introCutscene.Begin()` → shots+diálogo → `EnterTutorial()` + `tutorial.BeginTutorial()`.
- Campos serializados originais preservados (dialogue/player/tutorial/sirenSource) — nada re-wire na cena.
- Durações expostas no Inspector (`establishingDuration`, `characterDuration`, `dangerMinDuration`).

## Revisão R2b — intro na SALA DO DR. ELIAS (feedback do usuário)
A intro foi re-encenada para começar na sala do Dr. Elias (replicando `Art/Menu/Characters/Dr.Elias_Menu.png`), e ele agora **sai por uma porta deslizante** para o corredor:
- Construída `Gameplay_Round2_Polish/DrElias_Office` (z −34..−15.5): mesa com superfície holográfica ciano (materiais novos `MAT_Aurora_HoloTable/HoloDetail/ScreenDim`), caneca, janela ampla atrás da mesa com skyline noturno da instalação (backdrop `MAT_Aurora_NightSky` + torres com janelas acesas + spire com beacon vermelho), 3 monitores na parede esquerda, armários/prateleira à direita, iluminação própria (glow do holo + fill da janela + warm discreta).
- Player spawn movido de (0,0,0) para **(0, 0.05, −24)** dentro da sala; piso com collider próprio; a moldura de arco existente do corredor em z−15 emoldura a saída.
- **Porta deslizante** `OfficeDoor_Slab` (ContainmentWall + fissura vermelha + faixa hazard + collider sólido): desliza 4.7u para a direita em 1.2s (`SlideOfficeDoorOpen`, campos `officeDoorSlab/doorSlideDistance/doorSlideDuration`; auto-find por nome). Abre após o 2º bloco de diálogo ("Rota definida... você precisa correr"), antes do restore.
- Novos shots: 01 = mesa holográfica com Elias atrás e janela ao fundo (replica do menu); 02 = close no rosto; 03 = over-the-shoulder com Elias em primeiro plano e a porta sob alerta vermelho; 04 = porta desliza → restore → ele corre para fora da sala rumo ao tutorial.
- Validado em play: saída completa sala→porta→corredor→gate do tutorial, câmera atravessa o vão sem clipar, 0 erros.
- Nota: materiais emissivos criados via script exigem `globalIlluminationFlags = RealtimeEmissive` além do keyword `_EMISSION` — sem o flag, a validação do URP limpa o keyword no save (bug corrigido nos 4 materiais novos).
