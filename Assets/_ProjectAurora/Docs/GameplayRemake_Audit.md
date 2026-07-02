# Gameplay Remake — Auditoria Inicial (Beta03_Principal)

Data: 2026-07-01 · Cena: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` (17 roots, 4754 renderers, 34 luzes realtime)

## Setores (SectorManager: sectorLength=450, finishDistance=2700)

| # | Setor (HUD) | Faixa Z | Ambiente na cena | CelestIA |
|---|---|---|---|---|
| 0 | SETOR A: Laboratório Limpo | 0–450 | `Fase 01 - Aurora Research Corridor/Setor A - Laboratorio` (1050 filhos) | Normal |
| 1 | Corredor de contenção | 450–900 | `.../Corredor de Contencao` (525 filhos) | Normal |
| 2 | Sala de máquinas | 900–1350 | `.../Sala de Maquinas` (525) | Normal |
| 3 | Corredor vermelho | 1350–1800 | `.../Corredor Vermelho` (525) | Transition |
| 4 | Ponte técnica | 1800–2250 | `.../Ponte Tecnica` (525) | Corrupted |
| 5 | Terminal central | 2250–2700 | `Fase05 - Terminal Central` (Lead-In 2264–2544, Chamber 2659–2710) | Corrupted |

**Problema central:** os setores 1–4 são clones exatos do Setor A — mesma paleta, mesmos materiais, zero identidade. O "Corredor Vermelho" não tem nada vermelho.

## Sistemas críticos (NÃO TOCAR)
- `Dr. Elias - Player`: CharacterController + PlayerRunner/PlayerInteraction/PlayerHealth/DrEliasAnimationController
- `Game Systems`: GameManager, SectorManager, TutorialManager, CelestIAController, FinalCutsceneController (um só GO)
- `HUD Canvas`, `Canvas_GameOver`, `Main Camera` (CameraFollow, postProcessing=ON), `EventSystem`, `Music Manager`
- `ObstacleSpawner` é inerte (só metadado `authoredObstacleCount=84`) — não spawna nada em runtime

## Obstáculos (3 camadas)
1. **`Gameplay Objects`** — 45 filhos: colliders funcionais (`Obstacle`/`LaserHazard`) com renderer **desabilitado** (Low Barrier 2.2×0.7×0.8, Containment Barrier, Laser Hazard, Security Robot ×4) a cada 58u de z438–z2526 + Tutorial Door/Panel, Containment Door (z520), painéis interativos (z505, z735)
2. **`Fase01 - Detailed Obstacles`** — 46 visuais (Aurora Cargo/Box/Laser Emitter Posts) casados 1:1 com os colliders acima. Vários enterrados (ver ScaleAudit)
3. **`Gameplay Objects/Fase01 - Progressive Obstacles`** (207 renderers, z931–2526) e **`Fase01 - Curated Obstacle Pass`** (z90–400)

## Duplicações / lixo identificado
- `FASE01_CinematicEnvironment` (v2, 61 renderers, z15–435) duplica arcos/paredes/chão sobre o ambiente v1 no Setor A → risco de z-fighting e overdraw. **Desativar.**
- `Fase01 - Lighting` (v1) já desativado; contém `Fase01 Global Volume` (profile **vazio** — inútil como está)
- `Legacy_Primitives` vazio e desativado
- `GameplayInteractions_Examples` ativo em z148–214 (exemplos de porta/laser/bloco com scripts próprios) — sobrepõe a zona do Curated Pass. **Desativar** (referência de código preservada em disco)
- `ScaleReference_Beta03`: EditorOnly, desativado — manter

## Iluminação atual
- Key: `FASE01_Lighting/Key_Light_Fase01` (directional 0.75, sem sombras) + 2 point cyan
- `Fase05 - Integrated Lighting`: 19 point lights (900–2200 de intensidade)
- Ambient Flat RGB(0.28,0.34,0.40) — **alto demais, lava tudo**; Fog ativo
- Materiais emissivos corretos (M_F01_CyanEmission emission 4.5 HDR) mas **sem Bloom** (nenhum Volume ativo, profile vazio) → nada brilha

## Materiais (ambiente)
M_F01_DarkMetal ×1299, M_F01_WhitePanel ×745, M_F01_CyanEmission ×591, M_F01_Plant ×410, M_F01_WhiteEmission ×282, M_F01_Glass ×197, M_F01_Hazard ×184 — bom compartilhamento; faltam variantes por setor (vermelho, laranja industrial).

## Problemas de escala (de ScaleAudit_Beta03.md — a aplicar)
- Tutorial Door: 9.0×4.2, levemente enterrada → alvo 7.8×3.4
- Terminal Entry Gate: metade sob o piso (minY −2.748)
- 5× Low Cargo + 4× Tall Containment + 3× Laser (Curated Pass): visuais enterrados (colliders OK)
- 12× Aurora Cargo baixas: −0.017 sob o piso
- 4 props do Terminal Set Dressing enterrados
- Painéis interativos: trigger com center.y=0 atravessa o piso → center.y=0.5

## Estratégia do remake (ordem)
1. Consolidar hierarquia + desativar legado (v2 env, examples)
2. Post-processing global (Bloom/ACES/Vignette) + ambient escuro + key light com sombras
3. Correções de escala (lista acima)
4. Retematização por setor: trocar materiais de acento (arch glow, conduits, chevrons, ceiling lights, sector signs) por variantes de cor por setor + props/sinalização
5. Otimização: static flags, luzes, limpeza
