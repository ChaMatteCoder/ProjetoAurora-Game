# Round 18 — Tutorial: menu por ESC + PULAR TUTORIAL

**Objetivo:** primeira feature da fase "produto de qualidade". Durante o tutorial, o
jogador precisava (1) abrir o menu com **ESC** (ajustar áudio/qualidade, voltar ao menu,
sair, continuar) e (2) ter a opção **PULAR TUTORIAL**, já que refazer o tutorial toda vez
é cansativo depois de aprendido.

Data: 07/07/2026 · Cena: `Beta03_Principal` · Backend estável (Mono) preservado.

---

## O que mudou

### 1) ESC abre a pausa **durante o tutorial** — `GameManager.cs`
Antes, `TogglePause()` retornava cedo no estado `Tutorial` (ESC não fazia nada). Agora:
- `Tutorial` e `Playing` podem alternar a pausa; cinemáticas (intro/final) e fim de jogo
  continuam bloqueados.
- Novo campo `stateBeforePause`: o **Continuar** restaura o estado correto
  (`Tutorial → Tutorial`, `Playing → Playing`) em vez de sempre cair em `Playing`.
- O input do jogador é **congelado enquanto pausado** (`SetInputEnabled(false)`),
  evitando trocar de faixa/pular — ou completar um passo do tutorial — com o jogo parado.
- Propriedade `IsPausedFromTutorial` (usada pelo menu).

### 2) PULAR TUTORIAL (skip seguro) — `TutorialManager.cs` + `GameManager.cs`
- `TutorialManager.SkipToEnd()`: **teleporta** o Dr. Elias para logo depois da porta de
  contenção e conclui o tutorial (`CompleteTutorial → StartFullRun`).
  - Destino calculado: último passo do tutorial + 13 → **z≈111**, já **passando a porta
    sólida `Door_Slab` (z≈106)** e **antes do primeiro obstáculo real (z≈125)**. Sem o
    teleporte, o skip prenderia o jogador na porta fechada.
  - Teleporte feito com o `CharacterController` desabilitado momentaneamente (mover o
    transform com o CC ativo é ignorado pela engine).
- `GameManager.SkipTutorialFromPause()`: encerra a pausa (timeScale/áudio/input/painel) e
  chama `SkipToEnd()`. Guardas: só age se `IsPausedFromTutorial` e tutorial ativo.

### 3) Botão **PULAR TUTORIAL** no menu de pausa — `AuroraPauseMenuController.cs` + cena
- Novo `Button_PularTutorial` no `Main_Panel`, clonado do estilo existente, em
  **y = −603** (continua o ritmo de 86 px dos outros 5 botões e cabe no painel), acento
  ciano para se destacar como atalho.
- Visível **apenas** quando a pausa foi aberta no tutorial (`IsPausedFromTutorial`); nesse
  caso o hint padrão "ESC retoma a corrida" cede o espaço. Fora do tutorial, o menu de
  pausa fica **idêntico ao anterior** (nada reposicionado).
- Clique → confirmação ("Pular o tutorial e ir direto para a gameplay?") → skip.

### 4) Descoberta — hint de ESC no tutorial — `AuroraGameplayHUDController.cs`
- O hint inferior (que na intro mostra "ESC — Pular abertura") agora também aparece no
  **tutorial** com o texto **"ESC — Menu · Pular tutorial"**, sinalizando de imediato que
  o ESC abre o menu e que dá para pular.

---

## Validação em Play (Beta03)

Dirigida via API pública (mesmo ponto de entrada do ESC), com a intro pulada por reflexão.

| Teste | Resultado |
|---|---|
| ESC no tutorial abre a pausa | ✅ `State=Paused`, `IsPausedFromTutorial=True`, `timeScale=0`, painel ativo |
| Botão PULAR TUTORIAL visível só no tutorial | ✅ `SkipBtn active=True`, `Hint active=False` |
| Input congelado na pausa | ✅ `inputEnabled=False` |
| Continuar volta ao **Tutorial** (não Playing) | ✅ `State=Tutorial`, `timeScale=1`, input restaurado, tutorial ainda ativo |
| PULAR TUTORIAL → gameplay | ✅ `State=Playing`, `player.z: 12.5 → 111.0`, `IsComplete=True`, pausa fechada, autorun ligado |
| Corrida segue após o skip | ✅ avançou z 111 → 204 correndo (GameOver no fim = driver automático não desvia; jogador humano controla) |
| Console | ✅ sem exceções da feature |

**Restaurado após o teste:** `playModeStartScene = MainMenu` (comportamento menu-first do
jogo mantido).

---

## Observações / pendências
- Aviso pré-existente e **alheio ao R18**: `Coroutine couldn't be started because the game
  object 'Interactable_Door_01' is inactive!` — surgiu quando o driver automático correu
  sem desviar até uma porta distante. Nenhum código novo inicia coroutines em portas;
  candidato a hardening separado (checar `isActiveAndEnabled` antes de `StartCoroutine` no
  script da porta).
- Avisos cosméticos de *color primaries* dos MP4 permanecem (já documentados).

## Arquivos tocados
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/TutorialManager.cs`
- `Assets/_ProjectAurora/Scripts/UI/Pause/AuroraPauseMenuController.cs`
- `Assets/_ProjectAurora/Scripts/UI/AuroraGameplayHUDController.cs`
- `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` (novo `Button_PularTutorial` + wiring)
