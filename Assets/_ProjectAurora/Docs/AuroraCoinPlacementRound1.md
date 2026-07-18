# Posicionamento de AuroraCoins - rodada 1

Data: 13/07/2026

Total: 30 moedas em 6 grupos

## Hierarquia

```text
Gameplay_Collectibles
└── AuroraCoins
    ├── SectorA_Coins
    ├── Containment_Coins
    ├── MachineRoom_Coins
    ├── RedCorridor_Coins
    ├── TechnicalBridge_Coins
    └── FinalApproach_Coins
```

Cada grupo contém cinco instâncias do prefab oficial `PF_Aurora_HoloCoin`, nomeadas sequencialmente.

## Posições

| Grupo | Posições `(x, y, z)` | Intenção |
|---|---|---|
| SectorA | `(3,1.1,112)`, `(3,1.1,118)`, `(3,1.1,150)`, `(3,1.1,156)`, `(3,1.1,162)` | ensinar a coleta após o tutorial e manter a faixa direita segura |
| Containment | `(3,1.1,568)`, `(3,1.1,576)`, `(3,1.1,584)`, `(3,1.1,592)`, `(3,1.1,600)` | sequência legível na faixa direita |
| MachineRoom | `(0,1.1,1008)`, `(0,1.1,1016)`, `(0,1.1,1024)`, `(-1.5,1.1,1032)`, `(-3,1.1,1040)` | transição progressiva do centro para a esquerda |
| RedCorridor | `(0,1.1,1664)`, `(1,1.1,1671)`, `(2,1.1,1678)`, `(3,1.1,1685)`, `(3,1.1,1692)` | indicação moderada do centro para a direita |
| TechnicalBridge | `(0,1.1,1888)`, `(0,1.1,1896)`, `(0,1.1,1904)`, `(0,1.1,1912)`, `(0,1.1,1920)` | linha central curta na ponte |
| FinalApproach | `(0,1.1,2490)`, `(0,1.1,2500)`, `(0,1.1,2510)`, `(0,1.1,2520)`, `(0,1.1,2530)` | última sequência antes do Terminal, fora dele |

## Rota segura e DataFiles

- a primeira moeda começa em `z = 112`, depois do tutorial;
- nenhuma moeda está na introdução, em cutscene ou dentro do Terminal Central;
- as linhas usam apenas a pista principal e as faixas reais `x = -3, 0, 3`;
- os 12 DataFiles, entre `z = 133.91` e `z = 2085`, foram comparados pelo validador;
- o limite de alerta para rota de lore é 12 m e nenhuma moeda gerou aviso;
- as sequências não apontam para desvios secretos de DataFile.

## Ferramenta de ajuste

Menu: `Tools/Projeto Aurora/Collectibles/Aurora Coin/Placement Tools`.

Operações disponíveis:

- criar na Scene View ou na posição selecionada;
- criar linha com quantidade, espaçamento, faixa X, altura e parent de setor;
- alinhar moedas selecionadas ao chão;
- distribuir seleção em linha ou arco;
- renomear sequencialmente;
- validar posicionamento;
- instalar a rodada 1 de modo idempotente.

O instalador não sobrescreve posições já existentes. O validador apenas registra problemas; não corrige automaticamente.

## Validação

Resultado final: `coins=30, errors=0, warnings=0`.

Foram verificados trigger, Rigidbody cinemático, controller visual, referência visual, escala, limites da pista, parent, chão, colisores sólidos, espaçamento e distância dos DataFiles.

## Estado atual após ajustes manuais

Em 17/07/2026, a cena contém 204 moedas mantendo os seis grupos e a hierarquia acima:
`SectorA=72`, `Containment=42`, `MachineRoom=51`, `RedCorridor=12`,
`TechnicalBridge=26` e `FinalApproach=1`.

O validador retorna `errors=0` e `warnings=39`. A rodada de 30 permanece documentada
como baseline reproduzível; os avisos da expansão são consultivos e devem ser reavaliados
no próximo playtest de ritmo, sem remoção automática dos ajustes manuais.
