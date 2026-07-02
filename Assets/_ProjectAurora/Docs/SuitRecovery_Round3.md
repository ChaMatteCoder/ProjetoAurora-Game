# Suit Recovery — Regeneração de Integridade (Round 3)

Data: 2026-07-02 · Script novo: `Assets/_ProjectAurora/Scripts/Gameplay/SuitIntegrityRecovery.cs` (componente no `Dr. Elias - Player`)

## Design
Compensa o aumento de dificuldade: após um período sem dano, o traje recalibra e restaura 1 segmento por vez.

| Campo | Valor | Nota |
|---|---|---|
| maxIntegrity | 3 | teto = PlayerHealth.MaxIntegrity |
| recoveryDelay | **60s** | sem dano para iniciar a carga |
| recoveryDurationPerSegment | **10s** | carga gradual visível na HUD |
| resetRecoveryOnDamage | true | dano cancela a carga E reinicia o cooldown |
| recoverOnlyDuringGameplay | true | só em `GameState.Playing` (não em intro/tutorial/pausa/game over/cutscene) |

- Após restaurar um segmento, o próximo exige novo delay completo.
- Não ressuscita (Lives ≤ 0 nunca recupera → Game Over inalterado).
- Pausa: `Time.time` congela (timeScale 0) → cooldown/carga não avançam; a exibição some sem zerar o progresso.

## Integração (sem sistema de vida paralelo)
- `PlayerHealth.TryRestoreSegment()` (novo, único ponto que incrementa Lives): atualiza `ui.SetLives` + dispara `IntegrityChanged`.
- Detecção de dano por assinatura do evento `IntegrityChanged` (lives diminuiu → reset).
- Mensagem: "CELESTIA: Integridade do traje restaurada." ao completar.

## HUD (extensão do AuroraGameplayHUDController — nada refeito)
- `SetIntegrityRecoveryProgress(segmentIndex, progress)`: o segmento em recarga interpola de vazio→ciano conforme o progresso, com **pulso** senoidal de alpha.
- Label **"RECALIBRANDO TRAJE"** (TMP, criado sob Integrity System, fonte herdada) pisca suavemente durante a carga; some fora dela.
- `NotifyRecoveryComplete(segmentIndex)`: flash branco→ciano de 0.7s no segmento restaurado.

## Validação em play (driver, tempos encurtados em runtime p/ teste: 6s+3s)
- Dano (3→2) → restaurado para 3 em **9.0s exatos** ✓
- Dano durante a carga → carga cancelada (lives permaneceu 1 por toda a janela de verificação) ✓
- Recovery pós-cancelamento → restaurou em 9.0s a partir do novo dano ✓
- CelestIA anunciou a restauração; segmentos da HUD atualizaram ✓
- Valores de produção na cena: 60s + 10s.
