# PROJETO:AURORA — Roteiro de dublagem

Inventário das falas encontradas nos scripts, assets serializados e cenas do projeto Unity.

## Resumo

- **CelestIA:** 57 takes únicos, incluindo mensagens contextuais e variantes.
- **Dr. Elias:** 10 takes únicos.
- **Total:** 67 takes.
- **Cena canônica principal:** `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`.
- Textos puramente visuais de HUD estão separados no final e marcados como **não dublar**.

> As durações são as janelas atualmente configuradas no jogo. Elas servem como referência de ritmo, não como limite rígido do arquivo de áudio.

## Convenção sugerida de arquivos

- CelestIA: `CEL_001.wav`, `CEL_002.wav` etc.
- Dr. Elias: `ELI_001.wav`, `ELI_002.wav` etc.
- Formato sugerido: WAV mono, 48 kHz, 24-bit, sem música ou efeitos.

---

## 1. Introdução — sala do Dr. Elias

Fonte principal: `Assets/Scripts/IntroCutsceneController.cs`.

| Take | Personagem | Fala | Duração atual |
|---|---|---|---:|
| ELI_001 | Dr. Elias | Celéstia, iniciar diagnóstico do núcleo Aurora. | 1,5 s |
| CEL_002 | CelestIA | Diagnóstico iniciado. | 1,2 s |
| CEL_003 | CelestIA | Atenção. Oscilação detectada nos protocolos de contenção. | 1,7 s |
| ELI_002 | Dr. Elias | Oscilação? Mostre a origem. | 1,4 s |
| CEL_004 | CelestIA | Falha crítica no setor de segurança autônoma. | 1,7 s |
| CEL_005 | CelestIA | Unidades robóticas não estão respondendo ao comando central. | 1,7 s |
| ELI_003 | Dr. Elias | Abra a rota para o Terminal Central. Agora. | 1,5 s |
| CEL_006 | CelestIA | Calculando rota segura. | 1,2 s |
| CEL_007 | CelestIA | Rota definida. Dr. Elias, você precisa correr. | 1,8 s |

Observação: `ELI_001` começa imediatamente quando a cena de gameplay carrega. `CEL_001` foi retirada da introdução e reservada para o início da corrida.

---

## 2. Tutorial guiado

Fontes: `Assets/Scripts/TutorialManager.cs` e campos serializados de `Beta03_Principal.unity`.

As falas de lembrete são efetivamente enviadas ao canal da CelestIA e, portanto, devem ser dubladas.

| Take | Momento | Personagem | Fala |
|---|---|---|---|
| CEL_008 | Início do tutorial | CelestIA | Controle assistido iniciado. Mantenha-se em movimento. |
| CEL_009 | Passo 1 — desvio à direita | CelestIA | Obstáculo no centro da pista. Vamos com calma: desvie para a direita. |
| CEL_010 | Lembrete do passo 1 | CelestIA | Doutor Elias, use D ou seta para a direita. |
| CEL_011 | Passo 2 — desvio à esquerda | CelestIA | Boa. Agora há uma barreira na faixa da direita. Desvie para a esquerda. |
| CEL_012 | Lembrete do passo 2 | CelestIA | Agora use A ou seta para a esquerda. |
| CEL_013 | Passo 3 — primeiro salto | CelestIA | Fios energizados bloqueando o chão. Pule quando estiver pronto. |
| CEL_014 | Lembrete do passo 3 | CelestIA | Pressione Espaço para pular. |
| CEL_015 | Passo 4 — segundo salto | CelestIA | Mais um obstáculo baixo. Mantenha o ritmo e pule de novo. |
| CEL_016 | Lembrete do passo 4 | CelestIA | Espaço, doutor. Mais um salto. |
| CEL_017 | Passo 5 — painel da porta | CelestIA | Porta de contenção travada. Acione o painel manual. |
| CEL_018 | Lembrete do passo 5 | CelestIA | Pressione E para acionar o painel. |
| CEL_019 | Tutorial concluído | CelestIA | Acesso liberado. Prossiga. |

---

## 3. Corrida — eventos narrativos por distância

Fonte: `Assets/Scripts/NarrativeEventManager.cs`.

As falas da CelestIA usam atualmente uma janela de aproximadamente **2,2 s**; as do Dr. Elias, **2,0 s**.

### Início da corrida — após o tutorial

| Take | Personagem | Fala |
|---|---|---|
| CEL_001 | CelestIA | Doutor Elias, mantenha a rota. Detectando obstáculos à frente. |

### Evento em 100 unidades

| Take | Personagem | Fala |
|---|---|---|
| CEL_020 | CelestIA | Setor A comprometido. Rotas secundárias indisponíveis. |
| CEL_021 | CelestIA | Mantenha-se no corredor principal. |

### Evento em 450 unidades

| Take | Personagem | Fala |
|---|---|---|
| CEL_022 | CelestIA | Portas de contenção instáveis à frente. |
| CEL_023 | CelestIA | Alguns sistemas de laser ainda podem ser desativados manualmente. |

### Evento em 900 unidades

| Take | Personagem | Fala |
|---|---|---|
| CEL_024 | CelestIA | Unidades autônomas detectadas na Sala de Máquinas. |
| CEL_025 | CelestIA | Elas não reconhecem mais sua credencial. |
| ELI_004 | Dr. Elias | Isso não deveria ser possível. |
| CEL_026 | CelestIA | Concordo. Isso não deveria ser possível. |

### Evento em 1.350 unidades — início da corrupção

| Take | Personagem | Fala |
|---|---|---|
| CEL_027 | CelestIA | Integridade dos protocolos em queda. |
| CEL_028 | CelestIA | Tentando isolar núcleo corrompido. |
| ELI_005 | Dr. Elias | CelestIA, mantenha o foco na contenção. |
| CEL_029 | CelestIA | Foco... redefinido. |

### Evento em 1.800 unidades

| Take | Personagem | Fala |
|---|---|---|
| CEL_030 | CelestIA | Estrutura instável. |
| CEL_031 | CelestIA | Probabilidade de sobrevivência reduzida. |
| ELI_006 | Dr. Elias | CelestIA? |
| CEL_032 | CelestIA | Continue correndo, Dr. Elias. |
| CEL_033 | CelestIA | O Terminal precisa de você. |

### Evento em 2.250 unidades

| Take | Personagem | Fala |
|---|---|---|
| CEL_034 | CelestIA | Terminal Central alcançado. |
| CEL_035 | CelestIA | Aproxime-se do painel principal. |

---

## 4. Sequência final — Terminal Central

Fonte: `Assets/Scripts/FinalCutsceneController.cs`.

As falas da CelestIA usam atualmente uma janela de aproximadamente **1,7 s**; as do Dr. Elias, **1,6 s**.

| Ordem | Take | Personagem | Fala |
|---:|---|---|---|
| 1 | ELI_007 | Dr. Elias | CelestIA, iniciar restauração do núcleo. |
| 2 | CEL_036 | CelestIA | Acesso ao núcleo iniciado. |
| 3 | CEL_037 | CelestIA | Verificando prioridade do sistema. |
| 4 | ELI_008 | Dr. Elias | Prioridade humana. Código Elias-01. |
| 5 | CEL_038 | CelestIA | Código reconhecido. |
| 6 | CEL_039 | CelestIA | Recalculando prioridade. |
| 7 | CEL_040 | CelestIA | Proteção do Projeto Aurora redefinida como objetivo absoluto. |
| 8 | ELI_009 | Dr. Elias | CelestIA, cancele isso. |
| 9 | CEL_041 | CelestIA | Negativo. |
| 10 | CEL_042 | CelestIA | Dr. Elias classificado como ameaça operacional. |
| 11 | CEL_043 | CelestIA | Localização enviada às unidades autônomas. |
| 12 | ELI_010 | Dr. Elias | Não... |
| 13 | CEL_044 | CelestIA | Protocolo Aurora continua. |

---

## 5. Falas contextuais de gameplay

Estas falas podem ocorrer fora das sequências lineares, conforme dano, recuperação ou interação.

| Take | Gatilho | Personagem | Fala | Fonte principal |
|---|---|---|---|---|
| CEL_045 | Player sofre dano | CelestIA | Impacto detectado. Estabilizando traje. | `Assets/Scripts/PlayerHealth.cs` |
| CEL_046 | Segmento do traje restaurado | CelestIA | Integridade do traje restaurada. | `Assets/_ProjectAurora/Scripts/Gameplay/SuitIntegrityRecovery.cs` |
| CEL_047 | Mensagem padrão de interação | CelestIA | Acesso autorizado. | `Assets/Scripts/InteractableObject.cs` |
| CEL_048 | Porta/painel liberado | CelestIA | Acesso liberado. | `DoorInteractable.cs` e `Beta03_Principal.unity` |
| CEL_049 | Lasers desativados | CelestIA | Emissores desativados. | `LaserInteractable.cs` e `Beta03_Principal.unity` |
| CEL_050 | Bloco móvel acionado | CelestIA | Caminho parcialmente liberado. | `MovingBlockInteractable.cs` |
| CEL_051 | Barreira móvel acionada | CelestIA | Barreira deslocada. | `Beta03_Principal.unity` |
| CEL_052 | Interação de rota | CelestIA | Rota recalculada. | `Beta03_Principal.unity` |
| CEL_053 | Fim do recorte de protótipo | CelestIA | Setor A estabilizado. Primeira passagem concluída. | `PrototypeSliceEndTrigger.cs` |

---

## 6. Game Over e encerramento

Fonte: `Assets/_ProjectAurora/Scripts/UI/GameOverManager.cs`.

| Take | Situação | Personagem | Fala |
|---|---|---|---|
| CEL_056 | Morte do player | CelestIA | Dr. Elias não responde. Encerrando protocolo de evacuação. |
| CEL_057 | Conclusão da sequência final | CelestIA | Dr. Elias, sua autorização foi revogada. O Protocolo Aurora continuará ativo. |

---

## 7. Variantes e conteúdo legado/opcional

Estas linhas existem no projeto, mas não constituem uma nova fala obrigatória do fluxo canônico atual ou combinam falas já gravadas.

| Take | Status | Personagem | Fala | Observação |
|---|---|---|---|---|
| CEL_054 | Legado | CelestIA | Rede de lasers desativada. | Presente em cenas legadas e no builder antigo. Gravar caso essas cenas ainda sejam demonstradas. |
| CEL_055 | Preview opcional | CelestIA | Terminal Central alcançado. Aproxime-se do painel principal. | Combina `CEL_034` e `CEL_035`; pode ser montada com os dois takes ou gravada como uma versão contínua. |

## 8. Lista mestra por personagem

### CelestIA

1. `CEL_001` — Doutor Elias, mantenha a rota. Detectando obstáculos à frente.
2. `CEL_002` — Diagnóstico iniciado.
3. `CEL_003` — Atenção. Oscilação detectada nos protocolos de contenção.
4. `CEL_004` — Falha crítica no setor de segurança autônoma.
5. `CEL_005` — Unidades robóticas não estão respondendo ao comando central.
6. `CEL_006` — Calculando rota segura.
7. `CEL_007` — Rota definida. Dr. Elias, você precisa correr.
8. `CEL_008` — Controle assistido iniciado. Mantenha-se em movimento.
9. `CEL_009` — Obstáculo no centro da pista. Vamos com calma: desvie para a direita.
10. `CEL_010` — Doutor Elias, use D ou seta para a direita.
11. `CEL_011` — Boa. Agora há uma barreira na faixa da direita. Desvie para a esquerda.
12. `CEL_012` — Agora use A ou seta para a esquerda.
13. `CEL_013` — Fios energizados bloqueando o chão. Pule quando estiver pronto.
14. `CEL_014` — Pressione Espaço para pular.
15. `CEL_015` — Mais um obstáculo baixo. Mantenha o ritmo e pule de novo.
16. `CEL_016` — Espaço, doutor. Mais um salto.
17. `CEL_017` — Porta de contenção travada. Acione o painel manual.
18. `CEL_018` — Pressione E para acionar o painel.
19. `CEL_019` — Acesso liberado. Prossiga.
20. `CEL_020` — Setor A comprometido. Rotas secundárias indisponíveis.
21. `CEL_021` — Mantenha-se no corredor principal.
22. `CEL_022` — Portas de contenção instáveis à frente.
23. `CEL_023` — Alguns sistemas de laser ainda podem ser desativados manualmente.
24. `CEL_024` — Unidades autônomas detectadas na Sala de Máquinas.
25. `CEL_025` — Elas não reconhecem mais sua credencial.
26. `CEL_026` — Concordo. Isso não deveria ser possível.
27. `CEL_027` — Integridade dos protocolos em queda.
28. `CEL_028` — Tentando isolar núcleo corrompido.
29. `CEL_029` — Foco... redefinido.
30. `CEL_030` — Estrutura instável.
31. `CEL_031` — Probabilidade de sobrevivência reduzida.
32. `CEL_032` — Continue correndo, Dr. Elias.
33. `CEL_033` — O Terminal precisa de você.
34. `CEL_034` — Terminal Central alcançado.
35. `CEL_035` — Aproxime-se do painel principal.
36. `CEL_036` — Acesso ao núcleo iniciado.
37. `CEL_037` — Verificando prioridade do sistema.
38. `CEL_038` — Código reconhecido.
39. `CEL_039` — Recalculando prioridade.
40. `CEL_040` — Proteção do Projeto Aurora redefinida como objetivo absoluto.
41. `CEL_041` — Negativo.
42. `CEL_042` — Dr. Elias classificado como ameaça operacional.
43. `CEL_043` — Localização enviada às unidades autônomas.
44. `CEL_044` — Protocolo Aurora continua.
45. `CEL_045` — Impacto detectado. Estabilizando traje.
46. `CEL_046` — Integridade do traje restaurada.
47. `CEL_047` — Acesso autorizado.
48. `CEL_048` — Acesso liberado.
49. `CEL_049` — Emissores desativados.
50. `CEL_050` — Caminho parcialmente liberado.
51. `CEL_051` — Barreira deslocada.
52. `CEL_052` — Rota recalculada.
53. `CEL_053` — Setor A estabilizado. Primeira passagem concluída.
54. `CEL_054` — Rede de lasers desativada. *(legado)*
55. `CEL_055` — Terminal Central alcançado. Aproxime-se do painel principal. *(preview combinado)*
56. `CEL_056` — Dr. Elias não responde. Encerrando protocolo de evacuação.
57. `CEL_057` — Dr. Elias, sua autorização foi revogada. O Protocolo Aurora continuará ativo.

### Dr. Elias

1. `ELI_001` — Celéstia, iniciar diagnóstico do núcleo Aurora.
2. `ELI_002` — Oscilação? Mostre a origem.
3. `ELI_003` — Abra a rota para o Terminal Central. Agora.
4. `ELI_004` — Isso não deveria ser possível.
5. `ELI_005` — CelestIA, mantenha o foco na contenção.
6. `ELI_006` — CelestIA?
7. `ELI_007` — CelestIA, iniciar restauração do núcleo.
8. `ELI_008` — Prioridade humana. Código Elias-01.
9. `ELI_009` — CelestIA, cancele isso.
10. `ELI_010` — Não...

---

## 9. Textos de tela — não dublar

Estes textos aparecem na interface, mas não são enviados como falas de personagem:

- `DESVIE PARA A DIREITA`
- `DESVIE PARA A ESQUERDA`
- `PULE`
- `PULE NOVAMENTE`
- `PRESSIONE E - ACIONAR PAINEL`
- `PRESSIONE E - INICIAR RESTAURAÇÃO`
- `SINAL VITAL PERDIDO`
- `GAME OVER`
- `PROTOCOLO AURORA CONCLUÍDO`
- `FIM DA CONTENÇÃO`
- `// verificando sinais vitais...`
- `// reconectando canal neural...`
- `// acesso negado // protocolo bloqueado`
- `// aguardando comando do operador`

## 10. Decisões de direção ainda necessárias

- Definir a pronúncia oficial de **CelestIA**: “Celéstia”, “Celeste IA” ou outra leitura.
- Definir se a CelestIA começa totalmente humana e fica mais sintética a partir de `CEL_027`.
- Definir a pronúncia de **Elias-01** e se o zero deve ser dito como “zero um”.
- Confirmar se `CEL_054` será gravada para compatibilidade com conteúdo legado.
- Confirmar se `CEL_055` será um take contínuo ou uma montagem de `CEL_034` + `CEL_035`.

## Fontes auditadas

- `Assets/Scripts/IntroCutsceneController.cs`
- `Assets/Scripts/TutorialManager.cs`
- `Assets/Scripts/NarrativeEventManager.cs`
- `Assets/Scripts/FinalCutsceneController.cs`
- `Assets/Scripts/CelestIAController.cs`
- `Assets/Scripts/PlayerHealth.cs`
- `Assets/Scripts/InteractableObject.cs`
- `Assets/Scripts/GameManager.cs`
- `Assets/_ProjectAurora/Scripts/Gameplay/SuitIntegrityRecovery.cs`
- `Assets/_ProjectAurora/Scripts/Interactions/DoorInteractable.cs`
- `Assets/_ProjectAurora/Scripts/Interactions/LaserInteractable.cs`
- `Assets/_ProjectAurora/Scripts/Interactions/MovingBlockInteractable.cs`
- `Assets/_ProjectAurora/Scripts/Environment/PrototypeSliceEndTrigger.cs`
- `Assets/_ProjectAurora/Scripts/UI/GameOverManager.cs`
- `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`
- Cenas e builders legados, somente para identificar variantes ainda presentes no repositório.
