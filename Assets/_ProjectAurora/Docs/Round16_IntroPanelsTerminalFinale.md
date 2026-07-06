# Round 16 — Cena 01, Painéis, Terminal Atmosférico e Cutscene Final

**Veredito: APROVADO** nas quatro frentes, validado em play.

## 1. Cena 01 (laboratório) — render corrigido
Causas encontradas e corrigidas:
- **Materiais sobrepostos**: os arcos de teto ATRAVESSAVAM o teto (topo 6,55 vs base do
  teto 6,05) → interseção visível. Arcos abaixados 0,55 (topo agora 6,00).
- **Objetos flutuando sem nexo**: (a) pontas dos arcos terminavam no ar → ganharam
  **colunas de suporte** com glow e base (viraram pórticos); (b) cotovelos/stubs de
  conduíte flutuando → removidos, verticais estendidos até o teto; (c) vidro da janela
  coplanar com a parede (seam z-fight) → recuado 8cm; (d) asas da mesa assentadas no chão.
- **Partes flutuando ao abaixar a mesa**: as telas holo (y1,62) afundavam só 1,4 e
  ficavam 0,2 acima do piso. `deskStowDepth` 1,4 → **2,1** (maior topo da mesa = 1,84 —
  tudo afunda). 
- **Painel do Dr. Elias simples**: mesa ganhou asas laterais curvas com tampo de tela,
  deck de teclado inclinado com teclas, segundo disco holográfico e luzes de status —
  tudo sob `Desk_Cluster` (afunda junto).
- Incidente de automação: uma chamada com timeout aplicou o batch 6× (108 suportes,
  90 duplicatas na mesa, arcos a y-3,3) — detectado e limpo (contagens verificadas:
  6 suportes, mesa 1×, arco topo 6,00).

## 2. Painéis manuais — asset modelado + indicador E
- `Painel_Lazer` (FBX Tripo 49MB + 23 pares de textura) movido da raiz para
  `Assets/_ProjectAurora/Models/Props/Painel_Lazer/`; **52 texturas** corrigidas
  (basecolor→Default, normal→NormalMap, max 1024).
- **8 painéis** substituídos pelo modelo (tutorial z98, lasers z735, 1196, 1428, 1776,
  porta z505, e os 2 consoles R15 z2075/z2340): visual antigo com renderers desligados
  (transforms preservados p/ âncoras), modelo a 1,9× voltado para a pista, colliders off,
  sombras off.
- **Brilho de tela**: quad emissivo ciano na face + point light (range 3,2) por painel;
  `statusIndicators` religados ao novo glow (desativar laser apaga a tela).
- **Indicador E**: âncora subiu de +1,6 para **+2,7 a partir do chão** — o hexágono
  flutua claramente ACIMA do painel (não parece mais "F"). Validado por screenshot.

## 3. Terminal Central — entrada atmosférica
`TerminalLightsAwakening` (no grupo Fase05): coleta emissivos (Emission/Holo/Glass/
Screen) e Lights dos grupos do Terminal e os APAGA no início via
**MaterialPropertyBlock** (materiais compartilhados nunca são alterados; obstáculos
ficam visíveis — não pertencem aos grupos). Conforme o Dr. Elias avança
(z2596→z2678), **6 bancos** acendem em sequência com flicker de ignição (~0,55s,
PerlinNoise em degraus). Validado: núcleo com emissão 0 na chegada (screenshot escuro)
→ tudo aceso ao alcançar o console.

## 4. Cutscene final — redirigida
Problemas: porta fechada às costas dos robôs; chegavam rápido demais e ficavam
"andando no lugar"; enquadrava o Dr. Elias.
Correções (`TerminalFinalePresentation`):
- **A porta da perseguição ABRE** (Gate_Slab sobe 5,4 em 1,2s) antes de os robôs
  entrarem — são os mesmos 3 perseguidores barrados no Setor, agora entrando por ela.
  Validado: slabY=5,4 com robôs em cena.
- **Aproximação sincronizada com o diálogo**: FASE RONDA — avanço lento (0,55 m/s,
  animator a 0,55; se atingem o teto de ronda param DE VERDADE, sem andar no lugar)
  enquanto as 13 falas rolam; FASE CLÍMAX — quando **ELI_010 ("Não...") começa**,
  investida com ease-in até z2668. Telemetria da validação: ronda 2615→2631 ao longo
  de todas as falas; ELI_010 inicia com robôs em 2631; chegada em 2667 durante a fala;
  param a ~5m do Dr. Elias.
- **Censura**: no clímax a câmera vai para o PONTO DE VISTA do Dr. Elias (POV no
  console) — os robôs avançam PARA A LENTE; ele nunca aparece em quadro quando chegam.
  Plano 4 da ronda agora mostra o corredor com a porta aberta ao fundo.
- Luzes vermelhas do clímax começam desligadas (o despertar do terminal as ignora) e
  são ligadas/pulsadas só na aproximação.

## Testes
| Item | Resultado |
|---|---|
| Arcos sem interseção com o teto | ✅ topo 6,00 |
| Mesa afunda inteira (stow 2,1) | ✅ maior topo 1,84 |
| Painéis modelados + tela com glow | ✅ 8/8 |
| E acima do painel | ✅ y2,7, legível |
| Terminal escuro na entrada / aceso no núcleo | ✅ MPB emissão 0 → plena |
| Porta da perseguição aberta na cutscene | ✅ slabY 5,4 |
| Robôs chegam só no ELI_010 | ✅ 2631→2667 no "Não..." |
| Sem "andar no lugar" | ✅ animator 0 quando parados |
| Censura (POV, sem mostrar o Dr. Elias) | ✅ |
| Finished/Game Over | ✅ |
| Console | ✅ sem erros novos |

## Screenshots
`r16_lab_intro`, `r16_painel_e`, `r16_terminal_dark`, `r16_terminal_igniting`
(em Assets/Screenshots, nomes screenshot-20260706-*).

## Pendências
- Áudio de ELI_010 continua não dublado (texto no card).
- Orientação do modelo do painel presumida (frente para a pista); se o modelo tiver
  frente invertida em algum ponto específico, é um ajuste de rotY por instância.
