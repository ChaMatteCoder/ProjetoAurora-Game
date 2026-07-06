# HUD Polish — Round 13

**Veredito: APROVADO.** Card de diálogo sem sobreposição nos 4 falantes, glitch leve no
status corrompido, HUD de distância refeita e overlay de setor com fundo temático animado.

## Tarefa 1 — Card de diálogo (sobreposição corrigida)

Causa: `CelestIA Status` (largura 230, borda direita em x-38) invadia o cluster de sinal
(ícone + 2 barras em x-51..-25) — as barras cobriam os últimos glifos.

Correções:
- status movido para x-58 com largura 220 → **gap de 7px** para o cluster;
- cluster de sinal alinhado ao topo do texto (y-30);
- **auto-size no TMP do status (12–18)** — "STATUS: CORROMPIDA" (224px) e
  "BIOSINAL: ELEVADO" passam a caber sem clipar (bug pego no QA visual);
- nada de LayoutGroup novo: RectTransforms organizados com padding seguro (estrutura
  existente preservada — retrato/vídeos intocados).

Validação nos 4 falantes (screenshots r13_card_*): CelestIA normal (ciano),
CelestIA corrompida (vermelho + glitch), Dr. Elias normal/nervoso (dourado + BIOSINAL).

## Tarefa 2 — Glitch no status corrompido

`CelestIACommPanel.UpdateCorruptedGlitch()`:
- ativo APENAS com estado Corrupted E status começando com "STATUS" (suprime
  automaticamente quando o card mostra o Dr. Elias — "BIOSINAL...");
- bursts de 0,05–0,14s a cada 0,3–0,95s: offsets ±1,7px trocando a ~30Hz
  (aspecto scanline), cor alternando vermelho↔ciano, alpha instável;
- nome recebe eco leve do offset; a MENSAGEM nunca é afetada;
- retorno exato à posição base após cada burst; zero alocação por frame
  (PerlinNoise + aritmética).
Medido em play: desvio máx 1,2px; suprimido com Elias (0px).

## Tarefa 3 — HUD de distância

- trilha realinhada: 300×6, eixo em y44, pivô (1, 0.5);
- ticks de início/fim (3×14) nas extremidades;
- marcador do player em losango 13×13 (rot 45°) — movimento verificado:
  50% → x=0, 100% → x=150, fill 0,50/1,00;
- **bandeira redesenhada**: o grid quadriculado torto virou um pennant
  (mastro + banner + ponta losango + base), centralizado no eixo da linha (y44);
- integração preservada: mesmos GOs/refs (`distanceProgressFill`, `distanceMarker`,
  `distanceTrack`, `distanceValueText`) — zero mudança no código de distância.

## Tarefa 4 — Overlay de setor com fundo temático

`SectorTitleOverlayController` (Round 13):
- `Theme_Background` translúcido (alpha 0,82; tom azul-escuro normal / vinho corrompido);
- 5 linhas de acento (topo, base, divisória central, 2 laterais) ciano ou vermelho
  conforme o setor;
- **entrada animada**: card desliza 22px de cima + linhas fazem varredura horizontal
  (scale.x 0→1) durante o fade-in; fade-out mantido;
- sem bloqueio de input, 1× por setor (guarda por índice preservada).
Screenshots: SETOR E vermelho (r13_sector_red2) e SETOR B ciano (r13_sector_cyan).

## Bônus — bug real corrigido no retrato

Watchdog de vídeo só retomava slots em loop: se o app perdesse o foco durante o clipe
único de transição (Celestia02), ele nunca terminava e o retrato ficava **preso em
Transitioning** (bloqueando até a identidade do Dr. Elias). Agora slots não-loop também
retomam, exceto quando já estão no último frame (fim natural via loopPointReached).

## Preservados
Dublagem por ID, pool de vídeos dos retratos, tutorial, menu — intocados.
Console sem erros. Cena salva.
