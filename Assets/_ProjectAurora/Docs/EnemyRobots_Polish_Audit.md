# Enemy Robots — Auditoria de Polish (Round 7)

Data: 2026-07-03 · Cena: `Beta03_Principal.unity`

## Escala
- Prefab R6: RobotModel scale 1.83 → altura real **1.85** vs Dr. Elias **1.99** (bounds medidas) → robô MENOR que o player ("minion"). Alvo: 1.15–1.3× → **2.35** (1.18×) → scale **2.33**.

## Materiais/texturas ("pretos demais")
- 18 texturas basecolor do robô importadas como **Sprite** (default 2D do projeto) — corrigir para Default.
- Medição de brilho da basecolor (part_0): **média 0.14** — as texturas são genuinamente quase pretas (design Tripo). Não é bug de import: com albedo ~preto, nenhum multiplicador resolve.
- Decisão: corpo com **1 material compartilhado** `MAT_EnemyRobot_DarkMetal` (cinza-escuro 0.20/0.215/0.24, metallic 0.65, smooth 0.52 → leitura por specular das luzes do corredor) + **emissivos vermelhos** para identidade/ameaça. Bônus: 18 materiais→1 = SRP batching e menos state changes.

## Animação
- `DrElias_RunJump.controller` localizado (`Characters/DrElias/Animation/`): params Jump(Trigger)/IsRunning(Bool)/IsJumping(Bool); estados Idle Nervous/Running(0.7s)/Jump(1.0s); **clips Humanoid** (humanMotion=true) + AnyState→Jump. Avatar do robô é Humanoid válido → **retarget direto funciona**; Walking.fbx vira fallback documentado.

## Perseguição invisível (medido em play)
- Formação R6: lead a dz≈12 do player; câmera de runner fica a **8** atrás do player → todos os perseguidores ficavam ATRÁS da câmera (fora do frustum). Fix: lead com clamp de distância dz∈[min,max] com max < 8.

## Robôs-obstáculo parados
- Walking em loop sem deslocamento (esteira invisível). Fix: mover kinematic +Z (velocidade menor que o player) + janela de ativação por distância.

## Performance (fatores encontrados)
- 18 SkinnedMeshRenderers por robô × 9 robôs em cena; Animators sempre ativos; sombras em obstáculos; 18 materiais distintos (state changes); texturas até 2K.
- Medidas: material único; sombras OFF em todos SMRs; `updateWhenOffscreen=false`; Animator CullCompletely (obstáculos) / CullUpdateTransforms (perseguidores secundários) / AlwaysAnimate (só o lead); mover desliga Animator fora da janela do player; texturas max 1024; perseguidores mantidos em 3.
