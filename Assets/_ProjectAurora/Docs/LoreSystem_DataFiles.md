# Lore System - DataFiles

## Componente oficial

`AuroraDataFileCollectible` possui catálogo, `loreId`, coleta única por save, evento, SFX opcional, visual e trigger.

Ao detectar Dr. Elias:

1. Confirma que o ID existe e é `GameplayCollectible`.
2. Chama `AuroraLoreService.TryUnlockFromGameplay`.
3. Persiste no save central.
4. Exibe feedback pelo `DataFileManager`.
5. Desativa o objeto.

No próximo spawn, o componente consulta o save e se desativa antes de poder ser coletado novamente.

## Prefab

`Assets/_ProjectAurora/Prefabs/Collectibles/PF_Aurora_DataFile.prefab`

- Exemplo configurado: `LORE_001`.
- Trigger com `Rigidbody` cinemático.
- Corpo blindado escuro, tela emissiva ciano, moldura, núcleo e portas laterais.
- Movimento leve de rotação e flutuação.
- Visual distinto da AuroraCoin e sem cubo branco bruto.

Use `Tools/Projeto Aurora/Lore/Rebuild DataFile Prefab` para reconstruir a base.

## Posicionamento atual

`Beta03_Principal` preserva os 12 pickups legados já balanceados na corrida. A ponte converte a ordem física `DF_01..DF_12` para os registros coletáveis oficiais:

| Pickup | Lore | Pickup | Lore |
|---|---|---|---|
| `DF_01` | `LORE_001` | `DF_07` | `LORE_014` |
| `DF_02` | `LORE_003` | `DF_08` | `LORE_017` |
| `DF_03` | `LORE_005` | `DF_09` | `LORE_018` |
| `DF_04` | `LORE_006` | `DF_10` | `LORE_019` |
| `DF_05` | `LORE_011` | `DF_11` | `LORE_022` |
| `DF_06` | `LORE_013` | `DF_12` | `LORE_023` |

O `DataFile System` da cena referencia `AuroraLoreCatalog.asset`, portanto o fluxo também
funciona ao abrir a gameplay diretamente. Novos pickups devem usar o prefab oficial e um
`loreId` explícito, sem reutilizar IDs compráveis ou secretos.

## Compatibilidade legada

`PF_DataFile`/`DataFileCollectible` permanece como ponte dos 12 objetos já posicionados.
Persistência por `PlayerPrefs` foi removida; todos os IDs usam o save central.

AuroraCoins continuam reaparecendo por corrida. DataFiles de Lore são permanentes por save.
