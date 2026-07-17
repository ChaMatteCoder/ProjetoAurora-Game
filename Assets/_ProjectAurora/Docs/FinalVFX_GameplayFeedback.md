# FinalVFX — Feedback de Gameplay (referência rápida)

**Data:** 2026-07-16. Detalhes completos, testes e histórico de bugs no `FinalVFX_Wave1_Report.md`.

| Ação do jogador | Resposta visual | Onde está ligado |
|---|---|---|
| Tomar dano | Faíscas no traje (0,35 s) + shake 0,10 m/0,22 s + blink de i-frames preservado (2 s) | `PlayerHealth.TakeDamage` → `AuroraVFXController.PlayerDamage` |
| Recuperação do traje — início | Energia ciano contínua subindo pelo corpo (loop, presa ao player) | `SuitIntegrityRecovery` (start) |
| Recuperação — conclusão | Flash ciano (reusa CollectBurst) + HUD via sistema existente | `SuitIntegrityRecovery` (complete) |
| Recuperação — cancelada por dano | Efeito para e limpa imediatamente; nada preso ao personagem | `SuitIntegrityRecovery` (cancel + OnDisable) |
| Coletar AuroraCoin | Burst ciano radial (0,45 s) + pulso do contador 1→1.08 (já existia, validado) | `AuroraCoinCollectible` → pool · `AuroraCoinHudController` |
| Coletar DataFile | Scan digital — linhas verticais esticadas subindo (material próprio), sem moedas | `DataFileManager.ShowCollectedFeedback` |
| Prompt E disponível | Glow/cantos do card pulsando (só cor/alpha, layout intacto) | `AuroraPromptPulse` no prompt |
| Pressionar E (aceito) | Pulso de partículas no objeto interagido (nunca na UI) | `PlayerInteraction` → `InteractionConfirm` |
| Desativar laser | Faíscas nos emissores (máx. 3) no instante do desligamento | `LaserInteractable.HandleInteraction` |
| Porta abrindo | Poeira leve no vão | `AuroraDoorController.Open` |
| Morte | Todos os efeitos vivos recolhidos (`StopAll`) | assinatura de `PlayerHealth.OnDeath` |

Princípios aplicados: fachada no-op se ausente (gameplay nunca depende de VFX); pooling com teto (24/prefab); zero Point Light nova; materiais compartilhados com HDR serializado; MPB apenas onde a matriz validou (keyword `_EMISSION` serializada).
