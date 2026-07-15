# Relatório final - sistema de AuroraCoins

Data: 13/07/2026

Cena canônica: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## Entrega

1. `AuroraCoinWallet` criada como autoridade única, persistente e sem GameManager adicional.
2. Saldo limitado a 999, sem negativos e protegido contra overflow.
3. JSON versionado com saldo, skins e DataFiles desbloqueados.
4. Prefab oficial integrado à wallet sem remover animação, VFX, evento ou suporte a pooling.
5. Fundação de compras para `Skin` e `DataFile`, com custo inteiro e unlock persistente.
6. Dois itens e um catálogo claramente marcados como conteúdo de teste.
7. Card `HUD_AuroraCoinCounter` integrado à HUD existente e atualizado por eventos.
8. Rodada inicial de 30 moedas preservada e expansão manual atual com 194 moedas nos seis grupos da rota principal.
9. Janela Editor para criação, alinhamento, linhas, arcos, renomeação e validação.
10. Reset Editor isolado para saldo e unlocks de teste.

## Testes executados

- compilação Unity: 0 erros;
- validação estática dos 13 scripts tocados/criados: 0 erros;
- cena canônica: 0 problemas, 0 missing scripts e 0 prefabs quebrados;
- suíte Editor: 62/62 assertions;
- validador de placement: 194 moedas, 0 erros e 42 avisos consultivos de level design;
- coleta real: wallet única, `000 -> 001`, 29 moedas ativas e 1 coletada;
- nova execução: saldo `001`, 30 moedas ativas e 0 coletadas na corrida;
- restauração de teste: saldo e HUD retornaram a `000`;
- limite: 998 + 1 = 999; nova adição recusada; entrada extrema não causa overflow;
- compras: custo exato, unlock salvo e recompra recusada;
- corrupção: principal preservado, backup carregado e fallback validado;
- HUD: bounds, padding, intervalo de 16 px e resolução de referência validados;
- visibilidade: Intro 0, Tutorial 0, Gameplay 1, Pause 1, Game Over 1, Final 0;
- Play Mode após correção defensiva: 0 erros vermelhos novos.

Os 42 avisos atuais são 32 proximidades de DataFiles, 6 espaçamentos menores que 4 m
e 4 moedas altas em padrões de salto. Como não há erro estrutural e as posições foram
expandidas manualmente, o teste preserva o layout e trata esses pontos como revisão de ritmo.

## ArgumentNullException

O stack trace histórico e o erro reproduzido apontaram para `PanelScreenPulse.Update`, em `Renderer.GetPropertyBlock(mpb)`, porque `mpb` podia estar nulo. Não havia relação com HoloCoin, wallet ou HUD.

Como o erro passou a gerar spam e bloquear os testes de Play Mode, foi aplicada somente a guarda defensiva que cria `MaterialPropertyBlock` antes do uso. A validação posterior permaneceu sem erros.

## Compatibilidade e regressão

Os scripts de runtime não usam `UnityEditor`, caminhos específicos de plataforma ou plugins novos. A persistência possui fallback para plataformas sem `File.Replace`, mantendo compatibilidade de código com Windows e Linux.

Não foram alterados sistemas de intro, dublagem, vida, perseguição, distância, Game Over ou terminal. A única alteração no controller da HUD registra o novo card no fluxo de visibilidade já existente.

## Pendências

- definir preços e conteúdo final com game design;
- ligar o catálogo à futura interface do painel Extra;
- atribuir SFX/VFX adicionais somente se houver assets aprovados;
- fazer playtest humano completo de ritmo, tutorial, perseguição, Game Over e terminal;
- executar builds standalone Windows e Linux no pipeline de release. Nesta rodada foram validados compilação, APIs e ausência de dependências específicas, mas não foram gerados binários finais.
