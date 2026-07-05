# Implementação do controle de interrupções

## API adicionada

`VoiceLinePlayer` agora oferece:

- `Play(id, options)` e `PlayQueued(id, options)`;
- `PlaySequence(..., options)`;
- `StopCurrent`, `StopGroup`, `ClearQueue` e `ClearQueueByGroup`;
- `InterruptWith` e `IsPlayingGroup`;
- `CurrentLineId`, `CurrentGroup`, `CurrentPriority` e `CurrentOwnerStateId`.

`VoicePlaybackOptions` registra grupo, prioridade, interrupção, limpeza de fila, cancelamento na saída do estado, bloqueio de gameplay, fade e `ownerStateId`.

## Segurança contra callbacks antigos

- Cada pedido recebe um `playbackId` crescente.
- A coroutine só atualiza linha, HUD e callback enquanto seu ID ainda é o ativo.
- Cancelamentos marcam o pedido como concluído/cancelado e não executam callback.
- Pedidos removidos da fila também são finalizados para liberar coroutines de espera.
- Trocas usam fade curto de 0,08 a 0,10 s antes da próxima linha.

## Tutorial

- Ao entrar em uma etapa, o grupo Tutorial anterior é parado e sua fila é limpa.
- Cada etapa recebe `ownerStateId` próprio.
- Ao concluir a ação, a fala e o lembrete atuais são cancelados imediatamente.
- O lembrete captura `tutorialVoiceVersion`; qualquer mudança de etapa invalida a coroutine.
- O lembrete aguarda somente o grupo Tutorial, não toda voz global.
- `CEL_019` não bloqueia mais a saída do tutorial; ela toca como conclusão durante a transição para gameplay.
- `CEL_001` permanece após a conclusão, enfileirada de forma válida se `CEL_019` ainda estiver tocando.

## Intro e primeiro áudio

- A abertura usa grupo `Intro` e prioridade `Cutscene`.
- `ELI_001` e o bloco de alerta não aceitam skip por `Enter/Espaço`.
- Linhas com `AudioClip` usam `AudioSettings.dspTime` até `AudioClip.length + postDelay`, evitando cortes por frames longos.
- `Esc` cancela imediatamente o grupo Intro e o fallback visual.
- Assim, o comando usado no menu não pode mais cortar Dr. Elias e disparar CelestIA cedo.

## Contexto, HUD e vídeo

- Interação e traje usam grupos de prioridade `Context`; não cortam Tutorial, Intro ou Final.
- Novas mensagens contextuais removem pendências antigas do mesmo grupo.
- Cancelar uma linha limpa a legenda e devolve o retrato de Elias para CelestIA.
- Tokens impedem uma linha cancelada de alterar HUD/retrato posteriormente.

## Narrativa, Final e Game Over

- Narrativa por distância usa `SectorNarrative/Narrative` e substitui contexto obsoleto.
- Final limpa fila, interrompe a linha anterior e usa `Final/Critical`.
- Game Over limpa fila, faz fade curto e usa `InterruptWith` em `GameOver/Critical` para `CEL_056`.
- Encerramento usa política crítica equivalente para `CEL_057`.

## Cooldowns

- `CEL_045` mantém cooldown de 8 s no banco.
- `CEL_046` mantém cooldown de 2 s e só é solicitada quando um segmento é realmente restaurado.
- A fila `Suit` mantém somente o contexto pendente mais recente.
