# Lore System - Menu e Carrossel

## Fluxo

`MainMenu -> Extra -> LoreArchivePanel -> Extra`

- `Button_Lore` abre o painel.
- O card principal do Extra é desativado enquanto Lore ou Skin está aberto.
- `ESC` e `Button_Retornar_LoreArchive` retornam ao hub Extra.
- O `EventSystem` oficial foi preservado; não há duplicação.

## Estrutura

- Header: título, progresso, saldo e Voltar.
- FileCarousel: um único `FileCard`, anterior/próximo e posição `XX / 24`.
- LoreContentPanel: título, categoria e `ScrollRect` vertical.
- ActionArea: feedback e botão de compra quando aplicável.
- Footer: comandos de teclado e mouse.

O carrossel não instancia 24 cards. O controller troca os dados de uma única apresentação reutilizável.

## Controles

- `A`, seta esquerda ou botão `<`: arquivo anterior.
- `D`, seta direita ou botão `>`: próximo arquivo.
- `Enter`: tenta desbloquear somente quando o arquivo é comprável.
- Mouse/roda: rolagem do texto.
- `ESC` ou Voltar: retorna ao Extra.

## Estados visuais

- Desbloqueado: título real e texto completo.
- Coletável bloqueado: `DATAFILE NÃO LOCALIZADO`, sem conteúdo ou compra.
- Comprável bloqueado: título, descrição curta, preço, saldo e botão.
- Secreto bloqueado: `ARQUIVO SECRETO`, `CONTEÚDO CLASSIFICADO`, sem preço e sem botão.

O Markdown simples é apresentado sem `#`, `**`, separadores ou crases; parágrafos e listas permanecem legíveis. O arquivo-fonte não é alterado.

## Responsividade validada

- 1920x1080: texto desbloqueado, scrollbar, header, carrossel e ActionArea sem sobreposição.
- 1280x720: estado comprável e secreto sem colisões ou texto fora do painel.
- Canvas: `Scale With Screen Size`, referência 1920x1080.

Evidências:

- `LoreSystemValidationShots/LoreArchive_Unlocked_1920x1080_Settled.png`
- `LoreSystemValidationShots/LoreArchive_Purchase_1280x720_Actual.png`
- `LoreSystemValidationShots/LoreArchive_Secret_1280x720_Settled.png`
