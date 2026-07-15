# Menu de Skins - Seleção e Save

## Dois estados independentes

`AuroraSkinSelectionController` mantém o índice visualizado. `AuroraSkinSelectionService` mantém o ID equipado.

Navegar com A/D, setas ou botões altera somente:

- Splash Art;
- nome e descrição;
- contador;
- preview e estados visuais.

A skin equipada só muda após `SELECIONAR` ou Enter. A skin equipada mostra botão `EQUIPADA`, badge ciano e o status no cabeçalho.

## Regras de seleção

`CanSelect` exige:

- ID presente no catálogo;
- skin desbloqueada;
- `gameplayPrefab` válido.

Skin bloqueada mostra `BLOQUEADA` e não altera o save. Skin sem modelo mostra `INDISPONÍVEL` e também não pode ser equipada. Selecionar novamente a mesma skin não grava nem emite evento desnecessário.

## Persistência central

`AuroraProgressSaveData` está na versão 2 e recebeu:

```csharp
public string selectedSkinId = "default";
```

`AuroraCoinWallet` continua sendo a autoridade carregada em runtime e expõe `SelectedSkinId` e `TrySetSelectedSkinId`. `AuroraSkinSelectionService` salva somente o ID por essa autoridade, que usa `AuroraProgressSaveService`.

Não existe `PlayerPrefs` ou arquivo paralelo.

Ao carregar:

1. o ID salvo é procurado no catálogo;
2. unlock e modelo são validados;
3. um ID inválido, bloqueado ou sem modelo cai para a default;
4. o fallback corrigido é persistido novamente.

## Integração com Extra

- `SKIN` abre `SkinSelectionPanel` dentro de `Panel_Extra`.
- O card do hub fica inativo, eliminando click-through.
- O overlay do seletor bloqueia raycasts.
- `VOLTAR` retorna ao hub do Extra.
- `ESC` primeiro retorna ao hub; um segundo ESC fecha o Extra.
- O botão de retorno do subpainel não recebe o callback global de fechar todos os painéis.

## Preparação para gameplay

O ponto futuro de leitura é:

```csharp
AuroraSkinSelectionService.GetSelectedSkin().GameplayPrefab
```

Uma futura troca deve substituir somente o filho visual de Dr. Elias, preservando rig compatível, `Animator`, `PlayerHealth`, `CharacterController`, colliders e scripts do Player. Essa substituição não faz parte desta versão.
