# Terminal Central — Correção do Core Tube (Round de ajuste)

Cena: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
Referências: `Assets/_ProjectAurora/Scenes/FASE 05 - Terminal Central/Inspiração (1..3).png`

## 1. O que foi corrigido da implementação anterior
A implementação anterior (round passado) cometeu três erros centrais:

1. **Console de interação escondido.** As malhas do console do Dr. Elias
   (`Console Base`, `Console Shoulder`, `Main Screen`, `Panel Red Fault`, filhos de
   `Terminal Central Access`) haviam sido **desativadas**, dando a impressão de que o
   painel jogável tinha sido "substituído pelo tubo". → **Reativadas.**
2. **Tubo pequeno e no lugar errado.** O `PF_PainelPrincipal_Finale` foi colocado
   em z=2678 (em cima do console), escala 1.5 → apenas **2,35 m** de altura. → **Movido
   para o dais (z=2696) e escalado para ~8,6 m.**
3. **Tubo inteiro mudando de cor.** O `TubeCorePulse` tingia o `_BaseColor` do mesh
   inteiro do tubo (carcaça + estrutura). → **Reescrito para afetar só o núcleo interno.**

## 2. Como o painel de interação foi preservado
- O GameObject `Terminal Central Access` (z=2675) **não foi movido nem alterado** na
  lógica: mantém `InteractableObject (action=FinalTerminal)` + `BoxCollider` trigger.
- As 4 malhas visuais foram reativadas — o console volta a ser um elemento **próprio,
  separado e visível**, em frente ao tubo (posição lógica de chegada do jogador).
- Teste em Play Mode: `Interact()` executa, consome o `oneShot`, chama
  `GameManager.BeginFinalCutscene()` sem erros. Encenação da cutscene
  (`Focus - Main Panel` z=2678 / `Focus - Corrupted Core` z=2696) permanece coerente.

## 3. Como o tubo primitivo foi substituído
- O núcleo primitivo antigo (`Aurora Core`, `Core Glass` 8 m, `Core Base`,
  `Core Pedestal`, `Core_EnergyColumn`, `Core_EnergyInner`, `Core_BaseHalo`,
  `Ring_Under_Glow`) foi **desativado e movido** para
  `TerminalCentral_Rework/Legacy_PrimitiveTube_Disabled` (8 objetos).
- O modelo final `PF_PainelPrincipal_Finale` (tubo + 2 braços) ocupa o mesmo eixo
  central (x=0, z=2696), sem duplicatas competindo.

## 4. Como a escala foi ajustada
- Bounds de referência do primitivo: `Core Glass` = 8,0 m de altura.
- Novo conjunto em escala **5.5**: tubo ≈ **8,6 m** de altura (topo Y≈8,6; teto da
  câmara Y=11) — iguala/supera o primitivo. Leitura monumental, hero asset central.

## 5. Como os braços robóticos foram integrados
- Os dois braços (`Robotic_Arm_LP`, `Robotic_Arm_LP_R`) fazem parte do mesmo prefab
  final e acompanham a escala 5.5, emoldurando o tubo em ±7,4 m (dentro das paredes
  em x=±14). Animação preservada via `SimpleClipPlayer` (clip `Scene`).

## 6. Como os materiais foram separados
- **Carcaça externa:** `Tubo_Final_LP_Baked_Mat` — **intocado** (permanece estável).
- **Núcleo interno (novo):** `MAT_TerminalTube_Inner_Emissive` (URP/Lit, emissivo),
  aplicado a um cilindro `CoreInner_Emissive` inserido dentro da gaiola de metal.
- O mesh do tubo é **mesh única com 1 material**, então a separação por submesh era
  impossível; a solução foi o cilindro interno separado como único elemento colorível.

## 7. Como apenas o núcleo interno passou a mudar de cor
- `TubeCorePulse.cs` reescrito: agora recebe `innerCore` (Renderer do cilindro) e
  `coreLight` (luz interna). Oscila **somente** `_BaseColor`/`_EmissionColor` do núcleo
  e a cor/intensidade da luz, entre ciano (contenção estável) e vermelho (falha),
  com `faultBias` mantendo mais tempo no ciano. **Nunca toca o Renderer da carcaça.**
- A mudança de cor é dirigida principalmente por `_BaseColor` (confiável neste
  projeto URP; ver nota técnica abaixo). Validado: forçando vermelho, só o núcleo
  ficou vermelho — barras, base e acentos ciano da estrutura permaneceram estáveis.

## 8. Como a iluminação interna foi trabalhada
- `CoreInner_Light` (Point Light) no centro do núcleo (y=5), range 9 m, sem sombras.
- Cor e intensidade acompanham o estado (ciano ~3.5 → vermelho ~6.0), dando volume
  ao núcleo sem lavar a câmara. **Sem bloom global adicionado** (as Volumes existentes
  não têm bloom; adicionar afetaria o jogo inteiro) — o glow é local e controlado.

## 9. Testes realizados
- [x] Beta03_Principal abre; sem erros vermelhos no Console.
- [x] Console de interação E existe, visível e separado do tubo.
- [x] Tubo primitivo antigo desativado (Legacy); novo tubo no centro do dais.
- [x] Tubo grande/imponente (~8,6 m), braços ao redor proporcionais.
- [x] Somente o núcleo interno muda de cor (teste ciano e vermelho forçado).
- [x] Estrutura externa não alterna inteira — permanece estável.
- [x] Iluminação interna presente e contida.
- [x] Interação E executa `BeginFinalCutscene` sem erro (oneShot consumido).
- [x] Animação dos braços (`SimpleClipPlayer`) preservada.

## 10. Nota técnica (URP — cor em runtime)
Neste projeto, alterar `_EmissionColor` em runtime não gera bloom (as Volumes não têm
Bloom). A troca de estado do núcleo é dirigida por `_BaseColor` (comprovadamente
renderiza) + `_EmissionColor` (redundante) + a luz interna. Abordagem idêntica à usada
no `TubeCorePulse` e coerente com o restante do projeto.

## 11. Pendências / sugestões
- **Bloom:** se desejado um glow com halo no núcleo, adicionar Bloom a uma Volume
  **local** cobrindo só a câmara do Terminal (evita alterar o look do jogo todo).
  Não feito para não quebrar o visual existente.
- **Playtest completo:** a oscilação ciano↔vermelho só avança com o Editor focado
  (Time congela em play headless); recomendo uma corrida real até o Terminal para
  sentir o ritmo do pulso (`cycleSeconds=8`, `faultBias=0.35`) e ajustar se preciso.
- **Core Ring Segments** e `Dais Steps` primitivos foram mantidos como cenário de
  apoio; se competirem visualmente com o novo tubo, podem ir para o grupo Legacy.
