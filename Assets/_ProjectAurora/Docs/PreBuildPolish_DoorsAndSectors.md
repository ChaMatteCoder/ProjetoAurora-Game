# Pre-Build Polish — Portas e Transições de Setor (Round 11)

## AuroraDoorController (novo padrão de porta)

`Assets/_ProjectAurora/Scripts/Environment/AuroraDoorController.cs`
- Dois painéis deslizam em X local PARA DENTRO da estrutura (nunca bloco subindo);
- Ease: AnimationCurve opcional, senão SmoothStep; duração 1,1-1,15s;
- Collider de bloqueio desativa em 45% da abertura (`unblockAtProgress`);
- Luzes de status: vermelho (fechada/travada) → verde (aberta), com emissão;
- API: `Open() · Close() · SetLocked(bool) · PlayStatusLight() · OnApproachOpen()`;
- Campos de SFX prontos (`sfxSource`/`openClip` — sem clipe de porta no projeto ainda).

## Retrofit das portas existentes (abertura convincente)

| Porta | Antes | Agora |
|---|---|---|
| TutorialDoor_Containment (z106) | `Door_Slab` sumia via SetActive(false) | Slab_L/Slab_R deslizam para as molduras (travel 4,3); seam glow/hazard acompanham; status Status_Red; painel do tutorial aponta para o controller |
| Containment Door (z520) | painel deslizava a ESTRUTURA INTEIRA +4,2 Y | estrutura fica parada; novas folhas Leaf_L/Leaf_R (ContainmentWall + hazard + seam) fecham o vão e deslizam lateralmente (travel 3,7); collider de bloqueio próprio |
| Interactable_Door_01 (z150) | bloco subia 4m | folhas Aurora no lugar do blocker (renderer antigo oculto), controller integrado ao DoorInteractable |

Integração sem sistemas paralelos: `InteractableObject` (OpenDoor/TutorialPanel) e
`DoorInteractable` detectam `AuroraDoorController` no alvo e delegam `Open()`;
sem controller, o comportamento antigo permanece (fallback).

## PF_AuroraSectorDoor (prefab)

`Assets/_ProjectAurora/Prefabs/Environment/PF_AuroraSectorDoor.prefab`
DoorRoot → Frame_Left/Frame_Right (colunas), TopTrack + glow ciano, Panel_Left/Panel_Right
(6,3×4,7, ContainmentWall, seam vermelho + hazard stripes + ribs), StatusLight_L/R,
BlockingCollider (12,6×4,7), ApproachTrigger (14×6×3 @ z-30) com `AuroraSectorDoorTrigger`
(targetDoor, openDistance=30, sectorFrom/To, openOnce).

## Portas de transição (5, grupo Gameplay_SectorTransitions)

| Porta | z | Motivo da posição |
|---|---|---|
| Door_Transition_Lab_To_Containment | 452 | janela livre entre obstáculos 438/467 |
| Door_Transition_Containment_To_MachineRoom | 888 | antes do robô 902 e do início da perseguição (z905) |
| Door_Transition_MachineRoom_To_RedCorridor | 1351 | janela livre 1337/1366 |
| Door_Transition_RedCorridor_To_Bridge | 1817 | APÓS o LaserGate_Challenge_02 (z1801, desafio E) e antes da barreira z1830 |
| Door_Transition_Bridge_To_Terminal | 2250 | janela livre entre barreiras 2236/2265 |

Regras atendidas: alinhadas ao corredor de 3 faixas; abrem ~30m antes (2-2,5s de
antecedência); bloqueiam até abrir; ficam abertas (openOnce — perseguidores nunca são
presos; a única porta que fecha atrás segue sendo a da perseguição no Terminal z2566);
revelam o próximo setor pelo vão.

## Overlay de mudança de setor

`SectorTitleOverlayController` (HUD Canvas/Sector Card reciclado — CanvasGroup + título
58pt espaçado + linha divisória + subtítulo 24pt). Fade in 0,35s · hold 2,0s · fade out
0,45s; não bloqueia input nem pausa; 1 vez por setor (guarda por índice); nunca na intro
(SectorManager só dispara em Tutorial/Playing) e o setor A é apresentado no
`StartFullRun` (momento em que a HUD completa entra).

| Setor | Título | Subtítulo | Cor |
|---|---|---|---|
| 0 | SETOR A | Laboratório Limpo | ciano |
| 1 | SETOR B | Setor de Contenção | ciano |
| 2 | SETOR C | Sala de Máquinas | ciano |
| 3 | SETOR D | Corredor Vermelho | vermelho |
| 4 | SETOR E | Ponte Técnica | vermelho |
| 5 | NÚCLEO | Terminal Central | vermelho |

## Validado em play ([R11V]/[R11W])
- 5/5 portas de transição abriram na aproximação e o player passou sem parar
  (z461/897/1360/1826/2259); porta fechada BLOQUEIA de fato (comprovado por teleporte de
  QA que caiu além do trigger — o player ficou retido até o timeout, sem atravessar);
- porta do tutorial abriu pelo painel (IsOpen=True) com folhas deslizando;
- overlay disparou por setor (índices 1→5) + SETOR A no início da corrida;
- perseguição intacta após a porta 888 (3 perseguidores ativos);
- sem softlock: todas as travessias completaram.
