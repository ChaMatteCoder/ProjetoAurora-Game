# Lore System - Desbloqueio e Compra

## Autoridade

Somente `AuroraLoreService` decide se um arquivo está desbloqueado e qual API pode alterá-lo.

- `IsUnlocked(id)`: consulta default e save central.
- `TryPurchase(id)`: aceita apenas `AuroraCoinPurchase`.
- `TryUnlockFromGameplay(id)`: aceita apenas `GameplayCollectible`.
- `TryUnlockSecret(id, missionId)`: aceita apenas `SecretMission` com ID exato.

## Compra atômica

1. Resolver a definição no catálogo.
2. Validar categoria, preço, saldo e duplicidade.
3. Executar `AuroraCoinWallet.TrySpendAndUnlock`.
4. Atualizar saldo e `unlockedDataFiles` em memória.
5. Sanitizar e salvar uma vez pelo `AuroraProgressSaveService`.
6. Emitir eventos e atualizar UI.

Se qualquer validação falhar, saldo e desbloqueio permanecem inalterados. Saldo negativo e compra duplicada são impossíveis pelo serviço.

## Defaults

`LORE_008` e `LORE_009` são sincronizados no primeiro uso do catálogo e contam no progresso `02 / 24`.

## Secretos

- `LORE_020`: `SECRET_MISSION_LORE_020`.
- `LORE_024`: `SECRET_MISSION_LORE_024`.

Nenhum fluxo atual chama `TryUnlockSecret`. A API está pronta para uma missão futura, mas o menu não conhece nem exibe o `futureMissionId`.

Exemplo futuro, após a missão oficial validar sua conclusão:

```csharp
AuroraLoreService.Instance.TryUnlockSecret(
    "LORE_020",
    "SECRET_MISSION_LORE_020");
```

## Ferramentas

- `Validate Lore Files`: valida arquivos, catálogo, categorias, preços e IDs oficiais do save.
- `Reset Lore Unlocks`: pede confirmação, remove compras/coletas, mantém `LORE_008/009`, AuroraCoins e settings.
