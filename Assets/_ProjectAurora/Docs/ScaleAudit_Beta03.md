# Auditoria de escala — Beta03_Principal

Cena auditada: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`  
Escopo: somente a cena canônica de gameplay.  
Método: leitura da hierarquia completa no Unity, transforms, bounds agregados de `Renderer`, bounds e propriedades de `Collider`, scripts e estado de prefab. A extração bruta fica em `Temp/ScaleAudit_Beta03.json` e não integra o projeto.

## Referências físicas detectadas

- Piso jogável: `Y = 0`. A referência foi calculada pela base do `CharacterController` de `Dr. Elias - Player`.
- Player: `CharacterController` com aproximadamente `2,05u`; visual com aproximadamente `1,99u` de altura.
- Faixas: centros em `X = -3`, `0` e `3`; espaçamento de `3u`.
- Raízes globais da cena e o player estão em escala `(1,1,1)`. Não há justificativa técnica para escalar a fase inteira.
- O corredor e as três faixas permanecem a referência de X/Z. As correções propostas abaixo alteram apenas Y e/ou o visual local.

## Plano de escala

- Portas comuns: abertura visual de aproximadamente `3,2–4,0u` de altura; largura ditada pela função. Uma porta de faixa deve preservar a faixa, enquanto um portão transversal pode cobrir as três faixas.
- Porta tutorial: alvo de `7,8u × 3,4u`, suficiente para cobrir as três faixas sem a silhueta excessivamente larga atual.
- Caixas baixas: `0,6–1,1u` de altura, largura inferior ao espaçamento de faixa e base em `Y=0`.
- Obstáculos altos: `1,2–2,65u`, conforme a função de bloqueio, sem invadir faixas vizinhas.
- Lasers: collider de dano deve acompanhar apenas o feixe, não os emissores. Feixes de salto permanecem entre aproximadamente `0,55–1,1u`.
- Painéis: visual central em altura humana (`~1u`) e trigger com alcance deliberadamente maior; o volume não deve penetrar o piso.
- Props importados: preservar escala/rotação e corrigir o pivot central pela base dos bounds.

## Problemas encontrados

| Objeto / caminho | Posição atual | Escala atual | Bounds aproximado | Problema | Correção recomendada |
|---|---:|---:|---:|---|---|
| `Gameplay Objects/Tutorial Door` | `(0,2,8)` | root `(1,1,1)`; três visuais `(1.674,4.314,9.202)` | `9,00 × 4,20 × 0,72`; minY `-0,099` | Porta inicial larga e levemente enterrada; três visuais coincidentes foram preservados | Redimensionar somente os visuais para `7,8 × 3,4u` e apoiar a raiz por bounds |
| `Gameplay Objects/Containment Door` | `(0,2.2,520)` | `(1,1,1)` | `9,02 × 4,48 × 0,92`; minY `0` | Portão largo, mas alinhado ao corredor e já apoiado; não há collider funcional direto | Preservar para não alterar o bloqueio transversal das três faixas; revisão visual manual opcional |
| `Fase05 - Terminal Central/Approach Corridor - Three Lanes/Terminal Entry Gate` | `(0,0,2652)` | `(2.789,5.650,10.736)` | `10,50 × 5,50 × 1,20`; minY `-2,748` | Portão final com metade do visual sob o piso por pivot central | Elevar somente Y em `~2,748u`, preservando X/Z/escala |
| `Curated Obstacle Pass/Low Cargo Obstacle` — X/Z `(-3,90)`, `(0,205)`, `(-3,250)`, `(3,300)`, `(0,400)` | roots em `Y=0` | visual típico `(3.167,2.671,2.398)` | `2,35 × 0,85 × 1,10`; minY `-0,425` | Os cinco visuais estão metade enterrados; os BoxColliders já estão corretos, base em Y=0 | Elevar apenas `Obstacle Visual` em `~0,425u` |
| `Curated Obstacle Pass/Tall Containment Obstacle` — X/Z `(3,125)`, `(-3,205)`, `(0,300)`, `(3,350)` | roots em `Y=0` | visual varia por importação | `2,35 × 2,65 × 1,10`; minY `-1,323` | Os quatro visuais estão metade enterrados; colliders já medem `2,35 × 2,65 × 1,10` e começam em Y=0 | Elevar apenas `Obstacle Visual` em `~1,323u` |
| `Curated Obstacle Pass/Laser Obstacle` — X/Z `(0,160)`, `(3,250)`, `(-3,350)` | roots em `Y=0` | `Laser Unit Visual` `(4.461,6.098,2.960)` | unidade `2,90 × 2,80 × 1,20`; minY `~ -1,402` | Emissores importados enterrados; o trigger do feixe já está em Y `0,9`, size `2,55 × 0,20 × 0,35` | Elevar somente `Laser Unit Visual`; preservar feixe e trigger |
| `Fase01 - Detailed Obstacles/Aurora_Box_02 Visual` | `(-3,0.35,90)` | `(3.167,2.702,2.602)` | `2,55 × 0,86 × 1,10`; minY `-0,080` | Penetração residual no piso | Snap por bounds, sem alterar escala |
| `Fase01 - Detailed Obstacles/Aurora Cargo Visual` baixos — Z `438, 612, 786, 960, 1134, 1308, 1656, 1830, 2004, 2178, 2352, 2526` | X `-3`, Y `0,35` | `(1,1,1)` | `2,444 × 0,727 × ~0,986`; minY `-0,017` | Doze caixas baixas levemente atravessam o piso | Elevar cada visual em `~0,017u`; proporção baixa é intencional e coerente |
| `Fase05 - Terminal Central/Terminal Set Dressing/Containment Cargo L` | `(-8.5,0,2665)` | `(3.511,4.778,3.059)` | `3,0 × 3,1 × 2,4`; minY `-1,547` | Prop decorativo enterrado | Snap por bounds para Y=0 |
| `.../Containment Cargo R` | `(8.3,0,2671)` | `(6.334,4.399,4.081)` | `4,0 × 1,4 × 2,2`; minY `-0,700` | Prop decorativo enterrado | Snap por bounds para Y=0 |
| `.../Corrupted Laser Bank L` | `(-9.5,0,2684)` | `(4.316,6.114,5.094)` | `5,0 × 3,4 × 1,4`; minY `-1,701` | Prop decorativo enterrado | Snap por bounds para Y=0 |
| `.../Corrupted Laser Bank R` | `(9.5,0,2689)` | `(5.205,7.404,5.104)` | `5,0 × 3,4 × 1,4`; minY `-1,703` | Prop decorativo enterrado | Snap por bounds para Y=0 |
| `Gameplay Objects/Painel de lasers` | `(-3,1,735)` | `(1,1,1)` | visual `0,9 × 1,4 × 0,25`; trigger `4 × 3 × 5`; minY do trigger `-0,5` | Trigger justo em X/Z, porém meia unidade sob o piso | Manter Size/isTrigger e mudar `center.y` de `0` para `0,5` |
| `Gameplay Objects/Painel de porta` | `(3,1,505)` | `(1,1,1)` | visual `0,9 × 1,4 × 0,25`; trigger `4 × 3 × 5`; minY `-0,5` | Trigger atravessa o piso | Manter Size/isTrigger e mudar `center.y` para `0,5` |
| `Gameplay Objects/Tutorial Panel` (inativo) | `(0,1,2)` | `(1,1,1)` | visual `0,9 × 1,4 × 0,25`; trigger serializado `4 × 3 × 5` | Ao ativar, o trigger penetraria o piso | Corrigir `center.y` para `0,5` sem ativar o objeto |

## Itens verificados e preservados

- Obstáculos legados `Low Barrier` (`2,2 × 0,7 × 0,8`) e `Containment Barrier` (`2,2 × 2,6 × 0,8`) têm bounds de collider apoiados em Y=0.
- Obstáculos progressivos têm visuais apoiados, proporções coerentes e ocupam uma única faixa.
- Lasers existentes têm `BoxCollider.isTrigger = true` alinhado ao feixe; escalas finas são funcionais, não achatamento acidental.
- `GameplayInteractions_Examples` mantém `DoorInteractable`, `LaserInteractable` e `MovingBlockInteractable`; seus triggers começam em Y=0.
- `Terminal Central Access` permanece acessível e apoiado; o trigger maior que o console é intencional para interação.
- Não foram encontrados objetos nomeados como cabo/fio/cable/wire. É possível que cabos estejam incorporados em meshes de cenário sem nomenclatura semântica; isso requer revisão visual manual.
- Nenhum prefab global precisa ser alterado; os objetos auditados são instâncias/objetos da cena.

## Incertezas antes da correção

- A cena possui milhares de peças arquitetônicas com escalas não uniformes deliberadas (pisos, paredes, frisos e luzes). Elas não devem ser normalizadas automaticamente.
- `Containment Door` e `Terminal Entry Gate` são portões de corredor inteiro; sua largura não deve ser julgada como porta de uma faixa.
- Triggers de interação não devem ser ajustados ao volume do pequeno painel visual; isso reduziria injustamente o alcance da tecla E.

## Correções aplicadas

Aplicadas e validadas em 2026-07-01 (remake da gameplay — ver `GameplayRemake_FinalReport.md`):

- **Tutorial Door**: visuais redimensionados de `9,0 × 4,2` para `7,8` de largura (altura final `2,75u`, acima do player de `2,05u`); raiz apoiada por bounds em `Y=0` (estava flutuando `0,324u` após o resize).
- **Terminal Entry Gate, Curated Obstacle Pass (Low Cargo ×5, Tall Containment ×4, Laser ×3), Detailed Obstacles, Terminal Set Dressing**: verificação por bounds confirmou que os visuais já estavam apoiados em `Y=0` (correções haviam sido aplicadas em sessão anterior). Nenhuma alteração adicional necessária.
- **Painéis interativos** (`Painel de lasers`, `Painel de porta`, `Tutorial Panel`): triggers confirmados com `center.y = 0,5` (size `4×3×5` preservado) — não penetram mais o piso.
- **Sector Labels** (5 letreiros): rotação Y corrigida de `180°` para `0°` — o texto era exibido espelhado para o jogador em aproximação.
