# Dr. Elias — Correção de Material (Round 2)

Data: 2026-07-02 · Cena: `Beta03_Principal.unity`

## Diagnóstico
- O modelo (`DrElias Visual/DrElias Model`, FBX `tripo_convert_1c0ed329...`) tem 36 renderers (`tripo_part_0..35`), todos usando o **material embutido no FBX** (não editável como asset).
- Parâmetros do material embutido: URP/Lit, Metallic=0, **Smoothness=0.5**, BaseColor branco puro (1,1,1), textura `scientistcharacter3dmodel_basecolor` + normal `scientistcharacter3dmodel_normal`.
- Causa do visual "prateado": smoothness 0.5 uniforme gera specular forte em todo o corpo (jaleco incluído) sob a iluminação do remake (key light + emissivos + ACES). Não era Metallic — era brilho especular alto + reflexos de ambiente.

## Correção aplicada
- Criado `Assets/_ProjectAurora/Characters/DrElias/MAT_DrElias_Body.mat`:
  - Shader: URP/Lit
  - `_BaseMap`: textura original `scientistcharacter3dmodel_basecolor` (preservada)
  - `_BumpMap`: normal original (preservada)
  - `_BaseColor`: (0.965, 0.955, 0.94) — branco levemente quebrado, evita estouro
  - `_Metallic`: 0
  - `_Smoothness`: **0.30** (tecido fosco)
  - Environment Reflections: desligadas (`_ENVIRONMENTREFLECTIONS_OFF`)
- Remapeado nos **36 slots** dos renderers do modelo (sharedMaterial), material único compartilhado.
- A textura basecolor já contém as cores corretas por região (jaleco branco, roupa escura, pele, cabelo) — um único material fosco resolve todas as regiões sem separar submeshes.

## Preservado
- Rig, Animator, colliders, escala e hierarquia do player intactos.
- Material embutido do FBX intacto no asset (a troca é só nos renderers da cena).
- PlayerHealth.renderers continua válido (mesmos renderers, apenas material trocado).

## Correção adicional (feedback do usuário — take frontal com prata/rosto estranho)
Causa raiz encontrada nas TEXTURAS, não só no material:
- `scientistcharacter3dmodel_normal.jpg` estava importada como **Sprite** (default 2D do projeto). Normal map lido como sprite (sRGB, sem unpack) produz shading quebrado — o rosto "estranho" e os reflexos falsos "prateados" nos detalhes da roupa.
- Corrigido: importType → **NormalMap**; basecolor → **Default (sRGB)**.
- Ajuste fino: Smoothness 0.30 → **0.26**, `_BumpScale` 0.8.

## Validação
- Close-up em editor: jaleco lê como tecido branco fosco, calça escura, barba/cabelo/pele naturais, sem prata e sem manchas de shading.
- Validado também nos Shots 01–03 da intro (sala do Dr. Elias) e na câmera de runner em play.
