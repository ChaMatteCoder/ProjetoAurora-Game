# Auditoria do sistema de AuroraCoins

Data: 13/07/2026

Cena canônica: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## Escopo inspecionado

- prefab `PF_Aurora_HoloCoin`, com `SphereCollider` trigger, `Rigidbody` cinemático, cinco meshes, `AuroraCoinVisualController` e `AuroraCoinCollectible`;
- HUD real da `Beta03_Principal`, incluindo setor, integridade, distância, diálogo, prompt, overlay, pausa, Game Over e final;
- `AuroraGameplayHUDController` e o `CanvasScaler` de referência 1920 x 1080;
- configurações persistidas por `AuroraSettingsService` em `PlayerPrefs`;
- `DataFileManager`, `DataFileCollectible`, 12 DataFiles da cena e suas posições;
- painel Extra do menu, já dividido em hubs de Skin e Lore, ainda como placeholder;
- placeholders visuais de skins em `Assets/_ProjectAurora/Art/Skin`;
- cena, setores, obstáculos e documentação de tuning da rota;
- Console atual e logs anterior/atual do Unity.

## Sistemas reaproveitados

- o prefab oficial e sua animação de coleta são preservados;
- `PlayerHealth` continua sendo a identidade funcional do coletor;
- o HUD existente recebe somente um card filho dedicado;
- o painel Extra permanece intacto e poderá consumir a API de compras no futuro;
- o save de configurações não é alterado;
- o fluxo recorrente dos DataFiles em gameplay continua independente da futura compra de lore no menu.

## Lacunas encontradas

Não existia wallet, currency, economia ou save estruturado de progresso. O único progresso semelhante era o `DataFileManager`, opcionalmente salvo em `PlayerPrefs` pela chave `Aurora_DataFiles`; ele representa coleta de fase e não deve virar autoridade de moeda.

## Arquitetura escolhida

- `AuroraCoinWallet`: autoridade única, persistente e limitada a 999;
- `AuroraProgressSaveData` e `AuroraProgressSaveService`: um JSON versionado para saldo e unlocks;
- `AuroraPurchaseService`: transação sobre a wallet, sem saldo paralelo;
- `AuroraPurchasableItem` e `AuroraUnlockCatalog`: configuração autorada por assets;
- `AuroraCoinHudController`: atualização por eventos e animação curta sem polling;
- ferramentas Editor para posicionamento, validação, teste e reset.

## Persistência

Arquivo escolhido: `Application.persistentDataPath/aurora_progress.json`.

O serviço mantém backup da versão válida anterior, preserva um arquivo `.corrupt-<data>` quando o JSON principal é inválido, tenta recuperar o `.bak`, aplica clamp de 0 a 999 e usa escrita temporária/atômica com fallback compatível com Windows e Linux. Escritas de coleta são agrupadas por debounce curto; troca de cena, pausa, perda de foco e encerramento forçam um save seguro.

## HUD medido

- `Distance System`: âncora superior direita, posição `(-42, -34)`, tamanho `560 x 126`, limite inferior em `-160`;
- novo card reservado: âncora superior direita, posição `(-42, -176)`, tamanho `300 x 72`, com 16 px de separação;
- o card fica fora da área horizontal do `Sector Card`, acima do diálogo e do prompt e é incluído no mesmo grupo de visibilidade da HUD de gameplay.

## Posicionamento inicial

Plano: 30 moedas, cinco por grupo, nos intervalos reais dos seis setores. As linhas começam após o tutorial (`z > 111`), usam as faixas `x = -3, 0, 3`, evitam obstáculos medidos e mantêm ao menos 12 m dos 12 DataFiles. Nenhuma moeda entra na introdução, no Terminal Central ou em rota de lore.

## ArgumentNullException preexistente

O primeiro Play Mode direto não reproduziu o erro, mas uma execução contínua posterior voltou a gerá-lo em alta frequência. O stack trace histórico e o reproduzido apontaram para:

```text
ArgumentNullException: Value cannot be null.
Parameter name: dest
UnityEngine.Renderer.GetPropertyBlock(MaterialPropertyBlock properties)
PanelScreenPulse.Update() em Assets/_ProjectAurora/Scripts/Environment/PanelScreenPulse.cs:34
```

A origem era o `MaterialPropertyBlock mpb` nulo em `PanelScreenPulse`, não o HoloCoin. Quando o spam passou a bloquear os testes de Play Mode, foi aplicada a correção defensiva mínima: criar o `MaterialPropertyBlock` imediatamente antes de `GetPropertyBlock` caso ele esteja nulo. O teste direto posterior permaneceu sem erros vermelhos.

## Riscos

- o sistema de DataFiles ainda possui persistência própria opcional; uma migração futura deve ser explícita para não confundir coleta de fase com unlock comprado;
- os preços e itens criados nesta rodada são somente conteúdo de teste e não aparecem no menu;
- a primeira distribuição é segura pelos dados atuais e pelo validador, mas continua ajustável manualmente após playtest de ritmo;
- a distribuição inicial passou no validador, mas o ritmo e a leitura das faixas ainda devem receber playtest humano completo.
