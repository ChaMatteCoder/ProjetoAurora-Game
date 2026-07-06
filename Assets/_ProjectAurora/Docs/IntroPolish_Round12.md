# Intro Polish — Round 12

**Veredito: APROVADO.** Laboratório inicial retrabalhado, Dr. Elias revelado só após
CEL_003 com sirene 3D, e transição de áudio intro→gameplay sem estouro.

## Tarefa 1 — Laboratório inicial (Gameplay_Round_Polish/Intro_Lab_Rework)

Rework 100% com primitivas + materiais compartilhados `MAT_Aurora_*` (nenhum material
novo), sombras desligadas nos decorativos, sem colliders. ~90 renderers novos.

| Elemento | Composição |
|---|---|
| Arcos de teto (3) | 8 segmentos angulados cada (curva real), segmento central em EmissionCyan |
| Colunas curvas de parede (8) | cilindros com núcleo alternado ciano/escuro + capitel esférico |
| Arco luminoso da janela | 7 segmentos EmissionCyan acompanhando o topo do vão |
| Mesa tech | bordas em cilindro (CleanWhite), pé central cilíndrico, glow de base, **anel holográfico de dados** (10 chips), 2 telas holo flutuantes inclinadas |
| Interface da CelestIA | pilar com base, feixe ciano, 3 anéis de vidro/holo inclinados e "avatar" esférico emissivo — novo ponto focal dos planos de abertura |
| Cabos organizados | canaletas RubberBlack na base das paredes + conduítes verticais com cotovelo |
| Monitores da parede | 3 linhas de conteúdo emissivo por tela (ciano/âmbar) |

Peças da mesa ficam sob `Desk_Cluster` (afundam junto quando a porta abre — rig da intro
preservado). Nada obstrui os caminhos de câmera dos shots.

## Tarefa 2 — Reenquadramento do Dr. Elias

Nova montagem (IntroCutsceneController):
1. **Abertura** (ELI_001+CEL_002): establishing da porta (plano aberto, Elias pequeno/de
   costas), plongée na mesa holográfica (rosto fora de quadro), fillers: mesa em arco,
   janela/skyline e **interface da CelestIA** (substituiu o antigo perfil do Elias).
   O close antigo (Shot02) foi removido da abertura.
2. **CEL_003 isolada** ("Atenção. Oscilação detectada..."): alerta vermelho liga e a
   câmera faz travelling nos monitores + mergulho no núcleo — Elias segue fora de quadro.
3. **Revelação**: CEL_003 termina → `PlaySiren()` (hook do SFX) → corte seco para o
   close da reação assustada → resto do diálogo (ELI_002…CEL_007) com over-shoulder
   e fillers de alerta (dutch close agora permitido).

**Sirene**: `sirene.mp3` movida da raiz para
`Assets/_ProjectAurora/Audio/SFX/Alarm/sirene.mp3`. AudioSource 3D no teto do
laboratório (`OfficeSiren_3D`, com beacon vermelho): loop, spatialBlend 1, rolloff
Linear 5→70m, volume respeita o slider Efeitos. **Não é parada no fim da intro** — o
som se afasta naturalmente conforme o Dr. Elias corre (inaudível após ~70m). No skip
por ESC a sirene também dispara (ambiente coerente ao entrar no tutorial).

Preservados: falas por ID, card de diálogo, skip com ESC, início do tutorial.

## Tarefa 3 — Fades de áudio

`AudioManager`:
- `FadeNarrativeVolume(multiplier, duration)` — anima o volume com SmoothStep;
- `BeginGameplayMusic()` agora entra com **fade de 2,0s** (era salto 0.15→1.0);
- fades cancelam corretamente entre si (StopMusic/FadeOutMusic/SetNarrativeVolume).
Parâmetros dentro do sugerido: fadeInGameplay = 2,0s; a "música da intro" é a própria
trilha em 15% — não há corte, apenas a subida suave.

## Validação em play ([R12V]/[R12S])
- Abertura: câmera a 4,6m do ponto de close do Elias, sirene OFF;
- CEL_003: câmera nos monitores (-4.4, 2.1, -26.2), alerta vermelho ON, sirene OFF;
- Revelação: sirene ON (3D, maxDist 70) e câmera a **0,1m** do close — corte exato;
- Rampa de música: 0,12 → 0,24 → 0,48 → 0,71 → **0,80 (alvo)** em 2s, curva suave;
- Sirene pós-intro: tocando a 13m no início do tutorial; a 77m (fora do alcance) segue
  em loop inaudível — atenuação por distância comprovada;
- Skip por ESC: tutorial em 1,2s, sirene ligada, gating do tutorial intacto
  (step1 em WaitingForInstructionVoice);
- Console sem erros novos (só os avisos pré-existentes de color primaries).
Screenshot: `Assets/Screenshots/screenshot-20260704-215700.png` (pós-revelação).
