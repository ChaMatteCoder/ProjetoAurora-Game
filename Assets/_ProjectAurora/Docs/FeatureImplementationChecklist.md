# Checklist para novas features

## Antes de implementar

- Confirmar se a feature deve entrar em `Beta03_Principal`.
- Confirmar se existe script equivalente antes de criar novo.
- Nao criar manager novo sem necessidade.
- Nao criar cena nova.
- Nao criar outro player.
- Nao alterar `Assets/Scripts` legado sem necessidade.
- Preferir `Assets/_ProjectAurora/Scripts`.

## Durante implementacao

- Alterar o menor numero possivel de arquivos.
- Reutilizar HUD, `GameManager`, Player e sistemas existentes.
- Usar referencias serializadas.
- Evitar `FindObjectOfType` em `Update`.
- Evitar `Time.timeScale` para sequencias que precisam animar.
- Preservar `MainMenu -> Beta03_Principal`.

## Depois de implementar

- Testar `MainMenu -> Jogar`.
- Testar `Beta03_Principal` direto.
- Verificar Console.
- Confirmar que nao ha manager duplicado.
- Confirmar que nao ha Canvas/EventSystem duplicado.
- Documentar arquivos alterados.
