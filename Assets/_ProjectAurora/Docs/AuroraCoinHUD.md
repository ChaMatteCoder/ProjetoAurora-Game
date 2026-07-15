# HUD de AuroraCoins

Data: 13/07/2026

Objeto: `HUD Canvas/HUD_AuroraCoinCounter`

## Integração visual

O contador é um card filho do `HUD Canvas` existente. Não recria a HUD e usa a mesma fonte TextMeshPro, linguagem ciano, fundo translúcido, contorno fino e símbolo Aurora.

Conteúdo:

- símbolo compacto `A` em losango;
- saldo com três dígitos (`000` a `999`);
- rótulo `AURORACOINS`;
- linha de status temporária com autosizing.

## Medidas

O `CanvasScaler` mantém referência de 1920 x 1080.

| Elemento | Âncora | Posição | Tamanho |
|---|---|---:|---:|
| Distance System | superior direita | `(-42, -34)` | `560 x 126` |
| HUD_AuroraCoinCounter | superior direita | `(-42, -176)` | `300 x 72` |

O limite inferior da distância é `-160`; o topo do contador é `-176`, deixando 16 px de intervalo na resolução de referência. Todos os quatro textos permanecem dentro dos bounds do card e há margem direita de 42 px.

## Estados de visibilidade

O card entra no mesmo grupo controlado por `AuroraGameplayHUDController`.

| Estado | Alpha validado |
|---|---:|
| IntroCinematic | 0 |
| Tutorial | 0 |
| Gameplay | 1 |
| Paused | 1 |
| GameOver | 1 |
| Final | 0 |

Diálogo, prompt e overlay de setor continuam em grupos próprios. O contador ocupa somente a área abaixo da distância, sem tocar os painéis centrais.

## Atualização e feedback

`AuroraCoinHudController` escuta eventos da wallet; não existe polling em `Update`.

- pulso de escala/brilho: 0,22 s, usando tempo não escalado;
- primeira coleta: `AURORACOIN ADQUIRIDA`;
- limite: `LIMITE DE AURORACOINS ATINGIDO`, com cooldown de 5 s;
- nenhuma fala ou dublagem é interrompida.

## Evidência visual

- `AuroraCoinValidationShots/AuroraCoin_HUD_Runtime.png`: captura 16:9 em 2004 x 1128, próxima da referência 1920 x 1080;
- `AuroraCoinValidationShots/AuroraCoin_HUD_Runtime_Small.png`: captura 16:9 em 668 x 376.

As duas capturas exibem o card separado da distância, com ícone, saldo e padding preservados.
