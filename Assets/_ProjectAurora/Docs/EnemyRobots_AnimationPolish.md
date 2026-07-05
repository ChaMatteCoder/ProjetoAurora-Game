# Enemy Robots — Animation Polish (Round 7)

Data: 2026-07-03

## Controller de corrida/pulo (reuso do Dr. Elias)
- **NÃO** foi editado nenhum `.meta`. O controller real `DrElias_RunJump.controller` foi **copiado via AssetDatabase.CopyAsset** para `Assets/_ProjectAurora/Animations/Enemies/EnemyRobot_RunJump.controller`.
- Adaptações na cópia: **default state = Running** (robôs nascem correndo; Elias usa Idle Nervous); AnyState→Jump via trigger já existia no original e foi herdado.
- **Retarget Humanoid**: os clips do Elias são humanMotion; o avatar do robô (`robootAvatar`) é Humanoid válido → o Animator do robô toca Running/Jump do Elias retargetados no esqueleto do robô. Sem retrabalho manual.
- Prefab atualizado para o novo controller. `EnemyRobot_Animator.controller` (Walking) permanece como **fallback documentado** (não usado).

## Drive de animação
- **Perseguidores**: `IsRunning=true` no spawn; `Animator.speed=1.15` (corrida crível sem fast-forward — antes era Walking 1.7× esquisito). **Pulo mimetizado**: o director passa o Y da amostra atrasada; `EnemyPursuitRobot.ApplyAirborneState` dispara `Jump`(trigger)+`IsJumping` no takeoff e limpa no pouso — o robô pula ONDE e QUANDO o player pulou (com o delay do replay).
- **Robôs-obstáculo**: mesmo controller, `speed=1.0`, avanço físico real pelo `EnemyRobotObstacleMover` (a corrida agora desloca).

## Validação em play
- `[R7V] MIMETISMO PULO OK: leadY=0.41 animIsJumping=True` — replay de pulo + estado de animação confirmados.
- Se em playtest humano a animação retargetada apresentar deformação inaceitável no rig do robô, trocar o motion dos estados pela Walking (fallback) é operação de 2 cliques no controller do inimigo — sem tocar no do player.
