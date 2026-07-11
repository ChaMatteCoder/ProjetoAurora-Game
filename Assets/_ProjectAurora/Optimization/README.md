# Aurora — Ambiente de Otimização de Assets 3D

Preparado em 2026-07-08. Esta pasta contém **cópias de trabalho** para reduzir a contagem de
polígonos de assets pesados (IA/Tripo) mantendo os **high-poly originais intactos**.

## Estrutura
- `00_Reports/` — CSV de candidatos, `aurora_optimization_manifest.json`, `aurora_optimization_plan.md`.
- `01_HighPoly_Original_Copies/<SafeName>/` — **backup pristino** do original + `source_info.json` + `*.meta.backup.txt`.
- `02_Blender_Work/<SafeName>/` — `input/` (cópia que o Blender abre), `blend/`, `output/`, `logs/`.
- `03_LowPoly_FBX/<SafeName>/` — saída FBX low-poly (vazio até otimizar).
- `04_Baked_Textures/<SafeName>/` — BaseColor/Normal PNG (vazio até otimizar).
- `05_Reviewed_Approved/`, `06_Replacement_Staging/`, `99_Archive/` — etapas posteriores.
- `02_Blender_Work/aurora_hp_to_lp_bake.py` — script adaptado (presets de jogo Unity).

## REGRAS
1. **NÃO** substituir/mover/apagar os originais nesta fase.
2. **NÃO** rodar redução em lote. Processar **1 asset por vez** com preset explícito.
3. Nunca usar textura 8192 sem confirmação manual (padrão 1024/2048; 4096 só hero).
4. Os originais continuam sendo usados nas cenas/prefabs — só troque depois de revisar em 05/06.

## Nota Unity
As cópias `.fbx/.glb` sob `Assets/` serão **importadas** pelo Unity (duplicatas pesadas,
GUIDs novos — não quebram referências). Se o custo de import incomodar, mova a pasta
`Optimization/` para fora de `Assets/` ou exclua-a do build.
