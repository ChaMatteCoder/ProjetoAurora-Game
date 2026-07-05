# Enemy Robots — Polish Final Report (Round 7)

Data: 2026-07-03 · Cena: `Beta03_Principal.unity` (salva) · Console: 0 erros (só warnings benignos de color primaries dos mp4)
Docs: `EnemyRobots_Polish_Audit.md`, `EnemyRobots_AnimationPolish.md`, `EnemyRobots_PerformancePolish.md`

## 1. Escala
Antes: 1.85 de altura (menor que o Dr. Elias 1.99 — "minion"). Agora: **2.35** (1.18× o player, medido por bounds), base no chão, collider de dano do prefab e dos 6 roots de obstáculo redimensionados (1.5×2.35×0.9 / center y1.2).

## 2. Material/textura
Causa do "preto demais": as basecolor do Tripo têm brilho médio **0.14** (quase pretas por design — não era import). Corpo agora usa **MAT_EnemyRobot_DarkMetal** compartilhado (cinza-escuro metálico, specular pega as luzes do corredor). 18 texturas corrigidas para Default/1024 (estavam como Sprite, mesmo bug do projeto 2D).

## 3. Emissivos vermelhos
5 pontos **ancorados nos ossos** (seguem a animação): visor nos olhos (`Head`, `MAT_EnemyRobot_RedEyes` 6.5 HDR), núcleo no peito (`Spine02`), 2 luzes de ombro (`L/R_Clavicle`), strip traseira (`Waist`) — `MAT_EnemyRobot_RedEmission` 4.5 HDR. Zero luz realtime por robô; leitura garantida em corredor escuro/fog.

## 4. Controller do Dr. Elias
`DrElias_RunJump.controller` **copiado** (nunca editado o original nem `.meta`) para `EnemyRobot_RunJump.controller`; default→Running; clips Humanoid do Elias retargetados no avatar Humanoid do robô. Walking.fbx rebaixado a fallback documentado. Perseguidores em speed 1.15 (corrida crível); pulo do player replicado com o delay do replay (trigger Jump + IsJumping) — validado (`leadY=0.41, animIsJumping=True`).

## 5. Lead Pursuer visível
Robô 0 = lead: offset lateral 1.5, delay 0.45s e **clamp de distância dz∈[4.0, 5.4]** atrás do player (a câmera de runner fica a 8 — o lead vive ENTRE a câmera e o player, nunca ultrapassa, nunca dá dano). Calibração medida em play: primeira janela (4.2–6.2) deu 64% de frames na tela (borda); janela final **4.0–5.4 = 100% dos frames na tela**. Secundários a 10–11.5 no fog. followSharpness 22 no lead (sem lag atrás da câmera).

## 6. Robôs-obstáculo em movimento
`EnemyRobotObstacleMover` em todos os 6: avanço +Z kinematic a **2.2 u/s** (player corre 8–16 — alcança e desvia com telegraph), trecho máx. 40u, faixa X travada; o **guard do one-true-path (z1685)** anda a 1.0 u/s num trecho de 8u para preservar o padrão. Mover também liga/desliga o Animator pela janela do player (perf).

## 7. Otimizações — ver EnemyRobots_PerformancePolish.md
Resumo: 18 materiais→1 (SRP batching), sombras off, updateWhenOffscreen off, Animator culling por papel, movers dormindo fora da janela, texturas 1024. Medido: **4 Animators ativos** durante a perseguição (antes: todos, sempre).

## 8. Testes (drivers em play)
- Perseguição ativa (3 robôs), formação atrás, **lead 100% visível na câmera**, pulo mimetizado, porta do terminal fecha (slabY=0.00), silhueta 8s.
- Bônus: **Game Over disparou corretamente durante a perseguição** (o driver correu sem desviar) — resiliência do fluxo confirmada com o lead visível no quadro da morte.
- Console: 0 erros.

## 9. Pendências/riscos
- Confirmação visual humana da qualidade do retarget (animação do Elias no rig do robô) — fallback Walking é troca de 2 cliques se deformar.
- Cutscenes usam os mesmos robôs (escala/emissivos novos aplicam automaticamente); enquadramentos não foram alterados — revisar no playtest.
- O usuário integrou `VoiceLinePlayer` (vozes CEL_/ELI_) na intro/tutorial em paralelo — sem conflito com esta rodada; mensagens da perseguição continuam textuais (IDs de voz podem ser plugados depois).
