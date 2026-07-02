# Tutorial — Substituição dos Obstáculos Brancos (Round 2)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity` · Root: `Gameplay_Round2_Polish/Tutorial_RealObstacles`

## Descoberta
Os "obstáculos brancos" eram **primitivas criadas em runtime** por `TutorialManager.EnsureRuntimeSequence()` (cubos flat coloridos). O método só cria a sequência se não existir nenhum `TutorialStepTrigger` na cena — portanto a sequência foi **autorada em cena** com visuais reais, desativando os placeholders automaticamente e preservando o script.

## Correção estrutural necessária
`TutorialStepTrigger` e `TutorialActionGate` eram classes definidas dentro de `TutorialManager.cs`. Componentes de classes sem arquivo .cs homônimo **não sobrevivem à serialização de cena** (viram "Missing Script" ao recarregar). Foram extraídos para arquivos próprios:
- `Assets/Scripts/TutorialStepTrigger.cs`
- `Assets/Scripts/TutorialActionGate.cs`
Nomes de classe/API inalterados — nenhuma outra referência de código muda; o `AddComponent` runtime continua funcionando.

## Sequência autorada (mesmos Z, mensagens e gating do runtime original)
| Etapa | Trigger | Obstáculo real (visual, sem collider de dano) |
|---|---|---|
| 01 Desvie p/ DIREITA | z14 (9×3×3) | **Gabinete elétrico rompido** no centro/esquerda (z22): cabinet MachineMetal, painel quebrado inclinado, fissuras azul-elétricas emissivas, arcos elétricos sobre a faixa esquerda, cabo pendente, piso warning amarelo, lâmpada âmbar |
| 02 Desvie p/ ESQUERDA | z38 | **Colapso estrutural** na faixa direita (z46): placa de contenção caída inclinada com faixa warning, suportes, entulho, blinker âmbar |
| 03 PULE | z62 | **Cabos energizados no chão** (z64.8): 2 cabos de borracha + 1 feixe ciano emissivo atravessando a pista, emissores laterais com glow azul, faíscas |
| 04 PULE NOVAMENTE | z78 | **Tubulação baixa** (z80.8): cano industrial ⌀0.62 com anéis âmbar emissivos, suportes, válvula, strip warning no piso |
| 05 PRESSIONE E | z88 (trigger 9×4×6, InteractableObject TutorialPanel) | **Console de contenção** (pedestal + tela ciano inclinada) + **Porta de contenção** (z96): moldura escura fixa com glow ciano, slab duplo ContainmentWall com fissura vermelha central, faixas hazard e luz de status — o slab tem **collider sólido** e é desativado (porta + collider somem juntos) ao pressionar E |

- Todos os visuais usam os materiais compartilhados `MAT_Aurora_*` do remake — zero cubos brancos.
- Obstáculos do tutorial não causam dano (fiel ao original; dano só é permitido no estado Playing).
- `TutorialManager.tutorialPanel` re-apontado para o novo console.

## Legado desativado
`Gameplay Objects/Tutorial Door` (z8 — slab sem collider que o player atravessava visivelmente ao correr) e `Tutorial Panel` (z2, inativo) movidos para `Gameplay_Round2_Polish/Legacy_TutorialPlaceholders_Disabled` (desativado). Nada apagado do disco.
