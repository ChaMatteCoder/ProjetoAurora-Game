# Polish de Gameplay — Ritmo do Setor A + Perseguição (Round 16)

Data: 2026-07-19 · Cena: `Beta03_Principal.unity` · Pedido do cliente

## 1. Falas do Setor A distribuídas (antes: encavaladas pós-tutorial)

**Problema:** o tutorial termina ~z96; CEL_001 disparava no `StartFullRun` (colada na CEL_019
"Acesso liberado") e CEL_020+021 disparavam JUNTAS no gatilho único de 100m → 4 falas em ~5m.

**Correção** (`NarrativeEventManager.cs`, `CelestIAController.cs`):
- CEL_001 saiu do `CelestIAController.Begin()` (agora no-op) e virou evento de distância.
- Gatilho único de 100m dividido em três: **CEL_001 @150m · CEL_020 @230m · CEL_021 @320m**
  (demais eventos mantidos: 450/900/1350/1800/2250, índices deslocados +2).
- Novo guard `minSecondsBetweenEvents = 5s` — impede empilhamento quando vários gatilhos
  já foram ultrapassados (ex.: skip de tutorial).

**Validado em play:** CEL_001 t=159,5 → CEL_020 t=169,1 (+9,6s) → CEL_021 t=179,6 (+10,5s).

## 2. Pulo do robô perseguidor natural (antes: estirão à frente + volta)

**Causa raiz:** o clip `Jump` (Jump.fbx, compartilhado player/robô via retarget Humanoid)
tinha **Root Transform Position (XZ) → Bake Into Pose LIGADO** — o deslocamento frontal do
salto Mixamo (~2-4m) tocava DENTRO da pose. No player a câmera segue o transform e mascara;
no robô (transform clampado 4,2-6,2m atrás) a malha avançava quase alcançando o Elias e
"voltava" no fim do clip.

**Correção:** `lockRootPositionXZ = false` no clip (via ModelImporter). Com
`applyRootMotion=0` no player E no robô, o avanço extraído é descartado → pulo in-place,
todo deslocamento vem do transform (setup padrão de runner). Nenhum outro clip alterado.

**Validado em play (frame pausado 0,78s pós-pulo):** lead no ar (y=1,2) com quadril a
**−0,04m** do root em Z (antes: até ~4m) e 3,4m atrás do player — nunca ultrapassa.

## 3. Arrancada dos robôs ao bater em obstáculo (antes: robôs desaceleravam junto)

**Problema:** perseguidores são replay atrasado do player + lead clampado à distância do
player ATUAL → quando o player levava dano (slow 0,5× por 1,5s), todos desaceleravam em
sincronia — batida sem consequência dramática.

**Correção** (`RobotPursuitDirector.cs`): fator `surge` (0→1→0) disparado por
`PlayerHealth.IntegrityChanged` (só em dano real, durante perseguição, lives>0):
- **Ataque 0,35s:** lead fecha de 4,2-6,2m para `surgeLeadBehind=2,2m` (clamp interpolado);
  demais robôs encolhem `backOffset` ×0,45.
- **Hold 1,2s** colado (cobre o slow do player) · **Release 1,9s** recuando conforme o
  player re-acelera.
- Animação acelera ×(1+0,3·surge) (1,15→~1,5) · aviso "CELESTIA: Unidades ganhando
  terreno. Não pare!" (PriorityLow, não corta narrativa).
- Perseguidores seguem 100% visuais (sem colliders/dano extra); coroutine aborta se a
  perseguição terminar; hits repetidos reiniciam a arrancada.

**Validado em play:** dano → surge 0,93, anim 1,47, lead 3,6m (rompe o piso normal de
4,2m) → 3,2s depois: surge 0, anim 1,15, lead 5,8m, demais a 20-24m (elástico natural).

Console limpo (0 erros) em todos os testes.
