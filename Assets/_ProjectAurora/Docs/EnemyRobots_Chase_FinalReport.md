# Enemy Robots / Chase — Relatório Final (Round 6)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity` (salva) · Console: 0 erros
Docs: `EnemyRobots_Chase_Audit.md`, `EnemyRobots_Chase_Implementation.md`, `EnemyRobots_ObstacleReplacement.md`

## 1–3. Assets e animação
- `PF_AuroraEnemyRobot.prefab` (modelo riggado pelo usuário, escala 1.83 ≈ altura do Dr. Elias, base no chão, collider de dano opcional desativado).
- **Walking.fbx via retarget Humanoid**: modelo e animação convertidos para Humanoid (avatares válidos/humanos gerados automaticamente — nenhuma correção manual de mapeamento foi necessária); clip em loop, root motion off; `EnemyRobot_Animator.controller` com estado Walking; velocidade por `Animator.speed` (perseguição 1.7 = corrida; obstáculo 0.7–0.85).

## 4–7. Perseguição
- **Começa**: Sala de Máquinas (z≥905, estado Playing) — cutscene ~4s: câmera olha para trás, 3 robôs surgem correndo, CelestIA "Unidades autônomas ativadas. Dr. Elias, corra." / "Elas estão atrás de você." Player invulnerável durante a cutscene.
- **Imita movimentos**: replay atrasado — ring buffer da posição do player; cada robô usa a amostra de 0.55/0.8/1.05s atrás + recuo Z (5.5–7.5) + offset lateral (0/−2.2/+2.2). Troca de faixa e pulo reproduzidos com atraso.
- **Evita colisões visuais**: por construção — os robôs percorrem exatamente o caminho que o player já provou ser livre; colliders desligados; sem dano; sem NavMesh/física; movimento centralizado no director.
- **Termina**: corredor do Terminal (z≥2560) — porta de contenção (z2566) desce DEPOIS do player passar; cutscene mostra robôs convergindo e parando bloqueados atrás dela; CelestIA "Acesso ao núcleo isolado."/"Contenção restabelecida..."; robôs desativam após 4s.

## 8–9. Robôs-obstáculo — ver EnemyRobots_ObstacleReplacement.md
4 Security Robots primitivos substituídos pelo modelo real (dano/colliders/ativação narrativa preservados) + 2 blocos convertidos (Sala de Máquinas e one-true-path do Corredor Vermelho) = 6 robôs reais.

## 10. Como testar
1. Play → JOGAR → (Esc pula intro) → tutorial → correr até z905: cutscene dos robôs surgindo, perseguição começa.
2. Trocar de faixa/pular e ver os robôs repetirem com ~0.5–1s de atraso, sempre atrás.
3. Corredor Vermelho: robôs-obstáculo reais nas faixas (dano normal ao colidir).
4. z2560: porta fecha atrás, robôs param bloqueados, CelestIA confirma isolamento.
5. Terminal com E → cutscene final → "FIM DA CONTENÇÃO".

## 11. Pendências/riscos
- **Bug corrigido em validação**: re-disparo do início durante o encerramento (guard `!endSequenceRunning`) — documentado na Implementation.
- Só há animação Walking; "corrida" é Walking acelerada (1.7×) — animação de corrida dedicada seria upgrade futuro.
- Cutscenes usam a câmera principal com lerp (padrão do projeto); Cinemachine não existe no projeto e não foi instalado (regra).
- Perseguidores somem 4s após a porta fechar (silhueta) — manter permanente é opção de 1 linha se preferir.
- Validação de mimetismo fino (faixa/pulo frame a frame) merece confirmação visual humana com o editor focado; a lógica de replay foi validada numericamente (formação/atraso/contenção na porta).
