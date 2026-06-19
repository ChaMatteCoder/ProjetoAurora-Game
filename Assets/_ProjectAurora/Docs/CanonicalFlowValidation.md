# PROJETO:AURORA - Validacao do Fluxo Canonico

## 1. Cena de menu usada

Cena de menu canonica usada:

`Assets/_ProjectAurora/Scenes/MainMenu.unity`

Evidencias:

- Esta no Build Settings como primeira cena.
- Contem `Canvas_MainMenu`.
- O canvas referencia `ProjectAurora.UI.Menu.AuroraMainMenuController`.
- O componente serializado ja prioriza `Beta03_Principal` em `gameplaySceneCandidates`.

## 2. Cena de gameplay canonica

Cena de gameplay canonica:

`Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

Evidencias estaticas:

- Esta no Build Settings como segunda cena.
- Contem `Dr. Elias - Player`.
- Contem `GameManager`.
- Contem `Main Camera`.
- Contem `HUD Canvas`.
- Contem `Canvas_GameOver`.
- Contem objetos de tutorial, incluindo `Tutorial Door` e `Tutorial Panel`.
- Contem `Fase 01 - Aurora Research Corridor`.
- Contem `Fase05 - Terminal Central`.
- Contem `Terminal Central Access`.
- Contem `Gameplay Objects`.
- Contem `EventSystem`.

## 3. Build Settings antes/depois

Build Settings encontrado:

```text
0 - Assets/_ProjectAurora/Scenes/MainMenu.unity
1 - Assets/_ProjectAurora/Scenes/Beta03_Principal.unity
```

Alteracao feita em Build Settings:

- Nenhuma.

Motivo:

- `MainMenu` ja estava presente.
- `Beta03_Principal` ja estava presente.
- A ordem ja estava correta.
- Nenhuma duplicacao foi detectada.

## 4. Controller usado pelo menu

Controller canonico usado pela cena de menu:

`Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

GUID do controller:

`bef7d2d6ce235b14fb3632b51f39f50b`

Referencia encontrada em:

`Assets/_ProjectAurora/Scenes/MainMenu.unity`

Controladores legados preservados:

- `Assets/Scripts/MainMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/MainMenuController.cs`

Nenhum controlador legado foi apagado, movido ou renomeado.

## 5. Resultado esperado do botao Jogar

Comportamento esperado:

1. O usuario abre `Assets/_ProjectAurora/Scenes/MainMenu.unity`.
2. O usuario aperta Play.
3. O usuario clica em `JOGAR`.
4. `AuroraMainMenuController.StartGame()` percorre `gameplaySceneCandidates`.
5. `Beta03_Principal` e encontrada nos Build Settings.
6. `SceneManager.LoadScene("Beta03_Principal")` carrega a cena canonica.

Lista padrao de fallback no script apos a correcao minima:

```text
1. Beta03_Principal
2. Gameplay
3. RunnerScene
4. GameScene
5. SampleScene
6. Fase01_SetorA_LaboratorioLimpo
```

Observacao:

- A cena `MainMenu.unity` ja serializava `Beta03_Principal` como primeira candidata, portanto o fluxo canonico ja estava apontando para a cena correta.
- O ajuste no script fortalece novas instancias/recriacao do componente sem remover o fallback existente da Fase 01.

## 6. Problemas encontrados

- `AuroraMainMenuController.cs` tinha `Beta03_Principal` em primeiro, mas a lista padrao de fallback era curta demais: apenas `Beta03_Principal` e `Fase01_SetorA_LaboratorioLimpo`.
- `Beta03_Principal.unity` ainda depende fortemente de scripts em `Assets/Scripts`, conforme mapeamento anterior. Isso nao bloqueia o fluxo canonico, mas bloqueia limpeza agressiva neste momento.
- A validacao por Unity batch mode foi inconclusiva: o executavel iniciou e conectou licenca, mas o log encerrou cedo antes de carregar cenas/assemblies. Nao foi usado como prova de Play Mode.

Nao foram detectadas referencias obvias de Missing Script em `MainMenu.unity` ou `Beta03_Principal.unity` por busca estatica de `m_Script: {fileID: 0}`.

## 7. Correcoes minimas feitas

Arquivo alterado:

`Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`

Mudanca:

- Mantido `Beta03_Principal` como primeira cena de gameplay.
- Adicionados fallbacks seguros:
  - `Gameplay`
  - `RunnerScene`
  - `GameScene`
  - `SampleScene`
- Preservado fallback existente:
  - `Fase01_SetorA_LaboratorioLimpo`

Arquivos/cenas nao alterados:

- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/_ProjectAurora/Scenes/MainMenu.unity`
- `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
- scripts legados em `Assets/Scripts`
- `GameManager`
- `TutorialManager`
- `GameOverManager`
- builders de editor

## 8. Pendencias antes da limpeza arquitetural

Antes de mover/apagar qualquer coisa:

1. Fazer teste manual no Unity:
   - abrir `Assets/_ProjectAurora/Scenes/MainMenu.unity`;
   - apertar Play;
   - clicar em `JOGAR`;
   - confirmar que `Beta03_Principal` carrega;
   - confirmar que Dr. Elias/player aparece;
   - confirmar que camera e HUD continuam funcionando;
   - verificar Console.
2. Confirmar que nao ha erros vermelhos novos no Console apos a troca de cena.
3. Somente depois disso planejar migracao incremental de scripts ainda usados em `Assets/Scripts`.
4. Nao limpar cenas antigas, builders, tutorial, GameOver ou assets gerados ate o fluxo canonico estar testado em Play Mode.
