# AuroraCoin Wallet e persistência

Data: 13/07/2026

Cena canônica: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## Autoridade de saldo

`AuroraCoinWallet` é a única autoridade de AuroraCoins. Ela é criada antes da primeira cena por `RuntimeInitializeOnLoadMethod`, usa `DontDestroyOnLoad`, rejeita duplicatas e não depende de nomes de GameObject.

Invariantes:

- uma instância de `AuroraCoinCollectible` concede exatamente 1 unidade;
- saldo sempre entre 0 e 999;
- adições nulas/negativas são rejeitadas;
- custos negativos e gastos acima do saldo são rejeitados;
- uma moeda já coletada mantém o collider desativado durante a corrida;
- recarregar a cena recria todas as instâncias de moeda, sem persistir coleta individual.

API pública principal:

```csharp
public int Balance { get; }
public bool TryAddCoins(int amount);
public bool CanAfford(int cost);
public bool TrySpendCoins(int cost);
public void Load();
public void Save();
```

Eventos:

- `OnBalanceChanged(int balance)`;
- `OnCoinsAdded(int amount, int newBalance)`;
- `OnCoinsSpent(int cost, int newBalance)`;
- `OnBalanceLimitReached()`.

## Arquivo de progresso

O progresso usa um único JSON em:

```text
Application.persistentDataPath/aurora_progress.json
```

Formato inicial:

```json
{
  "version": 1,
  "auroraCoins": 0,
  "unlockedSkins": [],
  "unlockedDataFiles": []
}
```

`AuroraProgressSaveData.Sanitize()` força a versão atual, aplica clamp ao saldo, remove IDs vazios e duplicados e ordena as listas de unlocks.

## Escrita e recuperação

- coleta usa debounce de 0,2 s com `WaitForSecondsRealtime`;
- compra salva imediatamente como uma transação de saldo + unlock;
- troca de cena, pausa da aplicação, perda de foco e encerramento forçam save;
- a escrita usa `.tmp` e substituição atômica quando suportada;
- o fallback copia o arquivo anterior para `.bak`, compatível com Windows e Linux;
- JSON principal inválido é preservado como `.corrupt-<timestamp>.json`;
- o serviço tenta o `.bak` e, se ambos falharem, cria progresso padrão sem interromper o jogo.

O save de configurações em `PlayerPrefs` e a chave `Aurora_DataFiles` não foram alterados.

## Compras

`AuroraPurchaseService` usa a própria wallet, sem saldo paralelo. `TryPurchase` valida preço, saldo, ID e compra duplicada; em seguida subtrai o custo e persiste o unlock na categoria `Skin` ou `DataFile`.

Conteúdo de validação, não publicado no menu:

- `Skin_Test_01_TEST_ONLY.asset`: 25 AuroraCoins;
- `DataFile_Test_01_TEST_ONLY.asset`: 15 AuroraCoins;
- `AuroraUnlockCatalog_TEST_ONLY.asset`: catálogo com os dois itens.

## Reset de teste

Menu: `Tools/Projeto Aurora/Economy/Reset AuroraCoin Save`.

A ação pede confirmação, zera apenas AuroraCoins e remove somente `Skin_Test_01` e `DataFile_Test_01`. Configurações, DataFiles coletados e outros saves não são apagados.

## Evidência

A suíte Editor atual passou com 80 assertions, incluindo a contagem canônica de 204 moedas e o balanceamento de cinco corridas. O Play Mode confirmou uma única wallet, coleta `000 -> 001`, persistência após nova execução e restauração do saldo original `001 -> 000`.
