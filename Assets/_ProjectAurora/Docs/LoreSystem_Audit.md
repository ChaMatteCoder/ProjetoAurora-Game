# Lore System - Auditoria

Data da auditoria: 14/07/2026

## Escopo

- Projeto: `C:\ProjetoAurora-Game`
- Cena oficial: `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- Fluxo oficial: `MainMenu -> Beta03_Principal`
- Fonte final dos textos: `Assets/_ProjectAurora/Data/Lore/Text`

## Arquivos encontrados

- Foram encontrados 24 arquivos, de `LORE_001.txt` a `LORE_024.txt`.
- Arquivos ausentes: nenhum.
- IDs duplicados: nenhum.
- Textos vazios: nenhum.
- Arquivos extras no padrão `LORE_*.txt`: nenhum.
- Todos passaram por decodificação UTF-8 estrita e não possuem BOM.
- Nenhum marcador conhecido de mojibake (`Ã§`, `Ã£`, `Ã©`, `Â`, `�`) foi encontrado.
- Os arquivos originais foram movidos com segurança da pasta externa `Lore` para a pasta oficial dentro de `Assets`; não existe segunda fonte narrativa.

## Sistemas reaproveitados

- `AuroraCoinWallet`: saldo, limite 999, eventos e gravação atômica.
- `AuroraPurchaseService`: compra e desbloqueio na mesma transação.
- `AuroraProgressSaveData.unlockedDataFiles`: lista canônica para IDs `LORE_XXX`.
- `AuroraProgressSaveService`: JSON UTF-8, arquivo temporário e backup.
- `AuroraMenuExtraController`: fluxo Extra, Skin, Lore e retorno ao hub.
- `AuroraMainMenuController`: ESC, painéis e fluxo JOGAR.
- `DataFileManager`: somente feedback visual de coleta; a persistência antiga por `PlayerPrefs` foi removida.

## Estado anterior da UI

- O painel Extra já possuía `Button_Lore` e o placeholder `Card/Sub_Lore`.
- O `EventSystem` oficial já existia e foi preservado.
- O menu de Skin já utilizava um subpainel full-screen; o Lore segue o mesmo contrato de navegação.

## Riscos identificados e tratamento

- Encoding PT-BR: validado por bytes antes e depois da importação como `TextAsset`.
- Gasto sem desbloqueio: evitado por `TrySpendAndUnlock`, que altera saldo e lista antes de uma única gravação.
- Dois saves para DataFiles: eliminado; não há `PlayerPrefs` no fluxo oficial.
- IDs legados `DF_XX`: a ponte cobre `DF_01..DF_12` na ordem oficial dos 12 registros coletáveis e possui regressão de coleta/reload.
- Vazamento de segredo: o catálogo mantém o `TextAsset`, mas o menu nunca o apresenta enquanto bloqueado.
- Click-through: o overlay full-screen recebe raycast e o Extra desativa seu card principal durante o Lore.

## Plano executado

1. Centralizar e validar os 24 textos.
2. Criar definições e catálogo data-driven.
3. Integrar desbloqueios ao save e à economia existentes.
4. Criar coletável e prefab oficial.
5. Instalar menu, carrossel e leitura com scroll.
6. Validar regras, resoluções, persistência, Console e JOGAR.
