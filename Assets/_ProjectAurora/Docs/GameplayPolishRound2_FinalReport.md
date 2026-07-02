# Gameplay Polish Round 2 — Relatório Final

Data: 2026-07-02 · Cena: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` · Console: 0 erros
Relatórios da rodada: `GameplayPolishRound2_Audit.md`, `HUD_Redesign_Round2.md`, `DrEliasMaterialFix_Round2.md`, `IntroCameraSequence_Round2.md`, `TutorialObstacleReplacement_Round2.md`

## 1–2. HUD
- A HUD existente (`HUD Canvas` + AuroraGameplayHUDController, TMP) já implementava a estrutura da referência `Art/References/GameplayHUD_Ref.png`; validada em play 1:1 com o layout: setor (topo esq.) + objetivo com losango, INTEGRIDADE com 3 segmentos (topo centro), DISTÂNCIA com valor + track + flag (topo dir.), card CELESTIA (inf. dir.) com retrato circular `CelestiaNormal`, nome, STATUS, mensagem e waveform de 26 barras.
- Canvas Scaler já em 1920×1080 match 0.5, Screen Space Overlay. Nenhuma HUD duplicada. Integrações dinâmicas confirmadas em play (setor/distância/vidas/mensagens/estado da CelestIA).

## 3. Dr. Elias
- Causa do prateado: smoothness 0.5 uniforme do material embutido no FBX sob a nova iluminação (não era Metallic).
- Criado `MAT_DrElias_Body.mat` (textura basecolor+normal originais, Metallic 0, Smoothness 0.30, reflexos de ambiente off) e remapeado nos 36 renderers. Jaleco agora lê como tecido fosco. Rig/Animator/colliders intactos.

## 4. Intro cinematográfica
- `IntroCutsceneController` estendido (sem sistema paralelo): Shot 1 establishing do corredor (dolly), Shot 2 close no Dr. Elias, Shot 3 detalhe do gabinete elétrico sob alerta vermelho + sirene, Shot 4 retorno suave à câmera runner. Diálogos originais em paralelo aos shots. Esc pula tudo; Space/Enter pula linha (preservado). Sem timeScale=0; input bloqueado até o fim.
- **Bug corrigido:** o alerta vermelho tingia todas as luzes permanentemente; agora cores são salvas e restauradas (validado: luzes por setor voltam exatas após a intro).

## 5. Tutorial — obstáculos reais
- Os cubos brancos eram criados em runtime por `EnsureRuntimeSequence()`; a sequência foi **autorada em cena** (5 triggers z14/38/62/78/88 + porta z96) com obstáculos reais: gabinete elétrico rompido c/ arcos, colapso estrutural, cabos energizados, tubulação baixa, console + porta de contenção (slab com collider sólido desativado junto no E). Materiais `MAT_Aurora_*` compartilhados.
- **Correção estrutural:** `TutorialStepTrigger`/`TutorialActionGate` extraídos de dentro de `TutorialManager.cs` para arquivos próprios — classes sem arquivo homônimo não serializam em cena (viravam Missing Script). API idêntica.
- Legado desativado: `Tutorial Door` (z8, o player atravessava o visual) e `Tutorial Panel` (z2) → `Legacy_TutorialPlaceholders_Disabled`.

## 6. Correções lógicas
- Restauração das cores de luz pós-intro (acima).
- Labels de setor espelhados corrigidos (rodada 1, mantido).
- Porta do tutorial agora bloqueia fisicamente e libera ao abrir (collider vai junto do slab).
- Wiring verificado: Painel de porta→Containment Door, Painel de lasers→LaserHazard z760 (Deactivate desliga collider+visual), Terminal→BeginFinalCutscene, GameOverManager conectado.
- Gating do tutorial preservado (ações fora da etapa bloqueadas por TutorialActionGate; triggers one-shot).
- Nota de design (não alterado): `Containment Door` (z520) não tem collider sólido — a interação é opcional; adicionar collider criaria softlock para quem não está na faixa do painel.

## 7. Preservado
Aurora3DRenderer + post-processing + iluminação/setores da rodada 1; menu intocado; Build Settings intocados; GameManager/PlayerRunner/PlayerHealth/SectorManager/DialogueManager sem refactor (apenas IntroCutsceneController estendido e extração de classes do TutorialManager); fluxo MainMenu→Beta03 validado em play.

## 8. Validação executada (play mode, automação via APIs reais do jogo)
- Driver A: intro→tutorial→**5 passos completados** (D/A/pulo/pulo/E; porta abriu)→Playing→dano 3→2→1→0 com HUD atualizando→**Game Over com canvas ativo**.
- Driver B: **skip da intro por Esc**→tutorial→teleporte z2600→interação no terminal→**FinalCutscene**.
- Cena recarregada do disco com 0 erros e 0 missing scripts.
- Sessão manual do usuário confirmou passos 1–2 por teclado real e visual do Elias/HUD em jogo.

## Adendo R2b (feedback do usuário)
- **Rosto/prata do Elias**: causa raiz final era o normal map importado como *Sprite* (default 2D do projeto) — corrigido para *NormalMap* (+basecolor para *Default*); Smoothness 0.26. Take frontal validado.
- **Intro agora começa na sala do Dr. Elias** (réplica de `Dr.Elias_Menu.png`): sala construída em z−34..−15.5 (mesa holo, janela com skyline, monitores, armários), spawn movido para (0,0.05,−24), e ele **sai por uma porta de contenção deslizante** para o corredor quando CelestIA define a rota. Shots re-encenados (mesa → close → over-the-shoulder na porta sob alerta → saída). Validação em play completa: sala→porta→corredor→tutorial, 0 erros.

## 9. Pendências / riscos
- Retrato da CelestIA é sprite estático; variante com vídeo (CelestIAHudController + Celestia01-03.mp4) existe no projeto e pode ser integrada depois.
- Ritmo do diálogo usa `unscaledDeltaTime` sem clamp — hitches longos do editor podem "comer" linhas; em jogo normal o ritmo é o autorado (pré-existente, não alterado).
- O Console do editor estava com **Error Pause** ativo na máquina do usuário; foi desativado via editor. Se play pausar sozinho de novo, verificar o toggle no Console.
- Obstáculos do tutorial não causam dano (design original preservado).

## Como testar
1. Abrir Beta03_Principal → Play (via MainMenu → JOGAR).
2. Assistir a intro (4 shots; Esc pula; Space avança falas).
3. Tutorial: D → A → Espaço → Espaço → E no console (porta de contenção abre).
4. Correr, tomar dano (integridade cai), perder 3 vidas → Game Over.
5. Chegar ao terminal (z~2660) → E → cutscene final.
