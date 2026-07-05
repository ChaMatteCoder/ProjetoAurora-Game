# Auditoria de interrupções de voz

## Escopo

Cena auditada: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`.

Sistemas revisados: `VoiceLinePlayer`, `VoiceLineDatabase`, `DialogueManager`, `TutorialManager`, `TutorialStepTrigger`, `TutorialActionGate`, HUD narrativa, retratos em vídeo, intro, narrativa por distância, player, Game Over e final.

## Funcionamento anterior

- `VoiceLinePlayer` mantinha uma fila global de sequências, sem grupo ou identidade da etapa que originou a fala.
- A duração utilizava corretamente `AudioClip.length + postDelay`, mas o pedido continuava válido mesmo quando a gameplay já havia mudado de estado.
- `TutorialManager` tocava a instrução principal e aguardava globalmente `VoiceLinePlayer.IsPlaying` antes de emitir lembrete.
- Ao concluir uma etapa, a coroutine de áudio anterior não era cancelada e sua fila permanecia ativa.
- A conclusão do tutorial aguardava toda a reprodução de `CEL_019` antes de liberar a corrida completa.
- Mensagens contextuais podiam ficar enfileiradas atrás de narrativa protegida e aparecer tarde demais.

## Causa do corte de ELI_001

- `ELI_001` possui duração real aproximada de **3,1608 s** no Unity.
- A sequência de abertura era iniciada com `allowSkip = true`.
- `VoiceLinePlayer.Update` tratava `Enter` e `Espaço` como avanço imediato da linha.
- O `Enter` utilizado no menu podia permanecer ativo no primeiro frame da gameplay, encerrando `ELI_001` e iniciando `CEL_002` antes do fim.
- A espera por `Time.unscaledDeltaTime` também podia avançar demais em um frame longo de carregamento, enquanto o áudio continuava no mixer.
- Não havia um grupo `Intro` protegido para distinguir skip explícito da cutscene de entrada residual.

## Pontos de acúmulo encontrados

- `TutorialManager.ActivateStep`: nova instrução era enviada sem invalidar a anterior.
- `TutorialManager.ReminderRoutine`: lembrete podia sobreviver à mudança de etapa e usava estado global de reprodução.
- `TutorialManager.FinishTutorial`: a gameplay aguardava a fala de conclusão.
- `VoiceLinePlayer`: callbacks não possuíam token de validade nem `ownerStateId`.
- Chamadas de dano, recuperação e interação usavam fila global sem grupo contextual.
- `DialogueManager` enfileirava mensagens inferiores atrás de narrativa, mesmo quando já estariam obsoletas.

## Classificação definida

Grupos: `Intro`, `Tutorial`, `Gameplay`, `Interaction`, `Suit`, `SectorNarrative`, `RobotChase`, `Final` e `GameOver`.

Prioridades preservadas/ampliadas: `Low`, `Context`, `Gameplay`, `Tutorial`, `Narrative`, `Cutscene` e `Critical`.

## Falas protegidas

- Intro: protegida contra contexto/gameplay; somente `Esc` cancela o grupo.
- Final: grupo crítico e bloqueador de gameplay.
- Game Over: crítico, limpa fila e interrompe qualquer grupo.
- Narrativa de setor: pode substituir contexto/interação e narrativa de setor obsoleta.

## Plano aplicado

1. Adicionar opções de playback com grupo, prioridade, proprietário e política de interrupção.
2. Invalidar cada pedido por `playbackId` ao cancelar.
3. Limpar fila e fala do grupo Tutorial em toda entrada/saída de etapa.
4. Usar versão de voz no lembrete para impedir callbacks antigos.
5. Separar input do tutorial da duração do áudio.
6. Limpar HUD/retrato ao cancelar uma linha.
7. Dar a Final e Game Over interrupção crítica.
8. Desabilitar skip por `Enter/Espaço` na intro, mantendo `Esc`.
