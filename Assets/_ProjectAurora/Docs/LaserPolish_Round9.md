# Lasers — SFX + Feixes Triplos + Cortinas Completas (Round 9)

Data: 2026-07-03 · Cena: `Beta03_Principal.unity` (salva) · Console: 0 erros

## SFX (12 WAVs, sorteio aleatório)
- `fx/lazer01-12.wav` movidos da raiz para `Assets/_ProjectAurora/Audio/SFX/Lasers/` (raiz limpa).
- `LaserHazard` estendido: `impactClips[]` (toca ao atravessar feixe ATIVO — inclusive na janela de invulnerabilidade, feedback físico) e `deactivateClips[]` (toca no `Deactivate()` — cobre todos os painéis com E automaticamente). AudioSource 3D criado sob demanda (spatialBlend 1, alcance 3–30, rolloff linear), `PlayOneShot` com clip sorteado do pool a cada evento. Pool completo (12) atribuído aos **22 LaserHazards** da cena para ambos os eventos.

## Feixes triplos nos lasers "quebrados" (11 corrigidos)
Erro de modelagem: os lasers modelados tinham 1 linha só (embaixo) — e 3 Progressive não tinham visual NENHUM. Correção via Unity (controle de brilho, como planejado): grupo `LaserBeams` com **3 linhas emissivas** (0.06 de espessura, espaçadas ±0.15 em torno do centro do collider de dano), linha antiga preservada por baixo, `LaserHazard.visual` apontado para o grupo (o dim do E apaga tudo junto):
- 5× `Laser Hazard` (z670/1076/1482/1888/2294, pulo): 3 linhas vermelhas em ~y0.4/0.55/0.7
- 3× `Laser Obstacle` (Curated z160/250/350, modelados): 3 linhas vermelhas em ~y0.75/0.9/1.05
- 3× `Progressive Cyan Laser` (z2033/2207/2381): 3 linhas CIANO em ~y0.55/0.7/0.85 (tinham visual=null)
- Visual maior que o collider de dano (dano continua justo); LaserLanes (1221×2/1569/815) já tinham 3 feixes — intocadas.

## Cortinas completas nos LaserGates (portas)
`LaserGate_Challenge_01` (z760) e `02` (z1801): feixes iam só até y1.5 num vão de 4.1. Adicionados **5 feixes visuais extras** (y1.85→3.65) em `CurtainVisual` — portal agora tem **cortina de 8 feixes até o topo**. O grupo é o `visual` do feixe de dano superior: o E no painel desativa e **a cortina inteira escurece junto** (sem colliders novos — dano continua nos 3 feixes baixos, justo).

## Validação em play (driver)
- Atravessar o gate: **lives 3→2 + SFX de impacto tocando** (AudioSource do gate isPlaying=true) ✓
- E no painel z735: **SFX de desativação + 0 feixes ativos + cortina inteira em (0.12,0.12,0.12)** (dim total, incluindo os feixes novos do topo) ✓

## Revisão R9b — feedback do usuário (cores misturadas + feixes só embaixo)
O primeiro passe deixou os lasers modelados com o feixe grosso AZUL do modelo + linhas VERMELHAS aglomeradas embaixo, e os 2 traços quebrados dos postes (2.8 de altura) continuavam vazios. Correção definitiva:
- **Cor única (vermelho)**: o feixe grosso original (`Laser Damage Beam`, material ciano) foi recolorido para `MAT_Aurora_EmissionRed` — todo o obstáculo numa cor só.
- **Sobreposição dos traços quebrados**: os 3 feixes novos agora ficam nas alturas dos traços do modelo — **y0.9 / y1.62 / y2.3** (até o topo dos postes de 2.8) nos 3 Curated; nos 5 primitivos com postes de dressing, feixes em **y0.5 / y0.85 / y1.6** (alinhados com a lente emissora do poste).
- **Dano = visual (justiça)**: colliders esticados para cobrir os feixes (Curated 0.3–2.5; primitivos 0.3–1.7) — esses 8 lasers deixam de ser puláveis e viram obstáculos de **desvio** (todos têm ao menos 1 faixa livre; z350 vira o primeiro momento de faixa única do jogo — legível: parede de laser à esquerda + caixa alta à direita, centro livre).
- **Bug encontrado**: os 5 primitivos tinham escala achatada (0.16 em Y) da barra original, que encolhia feixes e collider — grupos de feixes agora neutralizam a escala do pai (localScale inverso) e o collider compensa a divisão.
- Progressive Cyan (z2033/2207/2381) permanecem ciano (cor única por obstáculo, como combinado "azul ou vermelho").

## Pendências
- Balanço fino de volume (sfxVolume 0.85 por laser, ajustável no Inspector) merece ouvido humano.
- Os 12 clips servem os dois eventos (pedido: uso aleatório); se quiser pools separados (impacto vs desativação), é só repartir os arrays no Inspector.
