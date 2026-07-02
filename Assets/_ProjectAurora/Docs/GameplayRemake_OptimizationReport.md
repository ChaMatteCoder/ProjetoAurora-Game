# Gameplay Remake — Relatório de Otimização

Data: 2026-07-01 · Cena: `Beta03_Principal.unity`

## Correção estrutural de pipeline (maior impacto visual E de correção)
- **Root cause do visual de protótipo**: o URP usava **Renderer2D** como renderer default — um jogo 3D renderizando pelo pipeline 2D (sem emissão, sem sombras, post stack degradado).
- Criado `Aurora3DRenderer.asset` (UniversalRendererData, Forward) em `Art/Generated/Environment/GameplayRemake/` e **adicionado** à lista de renderers (índice 1). O Renderer2D permanece default (índice 0) — **o menu não foi afetado**. Apenas a Main Camera da gameplay aponta para o renderer 3D.
- `AdditionalLightsPerObjectLimit`: 4 → 8.

## Redução de carga
- `FASE01_CinematicEnvironment` (ambiente v2 duplicado, 61 renderers sobrepostos ao v1 em z15–435): **desativado** → menos overdraw e risco de z-fighting.
- `GameplayInteractions_Examples` (3 interações de exemplo ativas em produção): desativado.
- `Legacy_Primitives`, `Fase01 - Lighting` (v1 com Volume vazio): agrupados e desativados.
- Tudo movido para `Legacy_Disabled_Remake` (nada apagado do disco).
- Camera farClipPlane = 500 (fog linear termina em 285 — nada visível além disso).

## Iluminação disciplinada
- 35 luzes realtime ativas na cena inteira (2 direcionais + ~33 points distribuídos em 2700u de pista — densidade baixa por trecho; ranges 14–22).
- Nenhuma luz com sombras exceto a key direcional (soft).
- 20 luzes do Fase05 com intensidades 850–2200 normalizadas para 4–20 (valores autorados para o renderer 2D quebrado).
- Emissão + bloom fazem o trabalho de acento no lugar de luzes adicionais.

## Materiais e batching
- 15 materiais novos compartilhados em `Materials/GameplayRemake/` (nenhum material por-objeto).
- Ambiente reutiliza o set `M_F01_*` existente; retema por setor feito por **troca de sharedMaterial** (zero materiais duplicados criados).
- Ambiente, obstáculos visuais e Fase05 (exceto grupos de cutscene) marcados static (já estavam, verificado); ~160 props novos do dressing criados já static.
- Props decorativos novos **sem colliders** (primitivas criadas e colliders removidos na geração).
- Visuais referenciados por `LaserHazard.visual` excluídos do static marking (mudam de cor em runtime).

## Números finais
- Renderers ativos: 4915 (4754 antes do dressing; v2 desativado compensa parte)
- Luzes realtime ativas: 35 · Colliders novos adicionados: 0
- Console: 0 erros / 0 warnings (edit mode e play mode)

## Não feito (deliberado)
- Occlusion culling bake e lightmap bake: exigiriam bake demorado; a cena é um corredor linear com fog a 285u — ganho marginal.
- Combinação de meshes: SRP Batcher + static batching já cobrem; combinar destruiria a editabilidade por setor.
