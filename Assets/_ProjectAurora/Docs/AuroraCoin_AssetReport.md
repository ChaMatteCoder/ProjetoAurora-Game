# Aurora HoloCoin - Relatório do Asset

## 1. Direção visual utilizada

O `Aurora_HoloCoin` foi criado como um token energético octogonal, com moldura hard-surface em metal grafite, núcleo holográfico ciano, oito módulos periféricos e o símbolo angular oficial do Projeto Aurora em geometria. A leitura foi simplificada para gameplay: silhueta simétrica, símbolo central grande, emissão concentrada e ausência de microdetalhes ou texto.

Frente e verso receberam acabamento. A frente usa o núcleo holográfico translúcido; o verso combina placa metálica recuada, anel ciano e repetição do símbolo Aurora.

## 2. Imagem de referência analisada

- Inspiração fornecida: `Assets/_ProjectAurora/Art/Collectibles/AuroraCoin/References/AuroraCoin_Inspiration.png`.
- Identidade oficial consultada: símbolo angular presente em `Assets/_ProjectAurora/Art/Menu/References/MenuFull.png`.
- A imagem de inspiração não foi usada como textura, plano ou sprite do prefab.

## 3. Dimensões do modelo

- Dimensão nominal no Blender: `0,48 x 0,48 x 0,118 m`.
- Bounds validados no Unity: `0,461 x 0,461 x 0,126 m` em `X/Y/Z`.
- Escala do prefab: `1, 1, 1`.
- Rotação e posição da raiz: `0, 0, 0`.
- Orientação final: moeda vertical, eixo `Y` para cima e profundidade no eixo `Z`.

## 4. Vértices e triângulos

| Mesh | Vértices no Blender | Triângulos |
|---|---:|---:|
| Coin_Frame | 672 | 1.280 |
| Coin_HologramCore | 48 | 92 |
| Coin_AuroraSymbol | 240 | 440 |
| Coin_EmissionDetails | 576 | 1.088 |
| Coin_BackPlate | 144 | 284 |
| **Total** | **1.680** | **3.184** |

O Unity reporta `3.510` vértices após as separações normais de UVs, normais e submeshes do importador. A contagem de triângulos permanece em `3.184`, abaixo da meta preferencial de 6.000.

## 5. Materiais criados

- `MAT_AuroraCoin_Frame`: URP/Lit, grafite azulado, Metallic `0,78`, Smoothness `0,62`.
- `MAT_AuroraCoin_Hologram`: URP/Lit transparente, ciano com alpha controlado e emissão.
- `MAT_AuroraCoin_Emission`: URP/Lit emissivo para símbolo, aro e barras laterais.

Não há Point Light no prefab. A pulsação é aplicada por `MaterialPropertyBlock`, sem instanciar materiais em runtime.

## 6. Texturas criadas

Nenhuma textura foi necessária. O asset usa materiais URP e cores/emissão, evitando mapas 4K e mantendo baixo custo de memória. As UVs foram geradas e organizadas no Blender para permitir texturização futura.

## 7. Organização da mesh

```text
PF_Aurora_HoloCoin
└── VisualRoot
    ├── Coin_Frame
    ├── Coin_HologramCore
    ├── Coin_AuroraSymbol
    ├── Coin_EmissionDetails
    └── Coin_BackPlate
```

A raiz contém `SphereCollider` como trigger, `Rigidbody` cinemático sem gravidade, `AuroraCoinVisualController` e `AuroraCoinCollectible`.

## 8. Animação Idle

`AuroraCoinVisualController` executa, de forma independente de framerate:

- rotação completa em `3,2 s`;
- flutuação vertical de `0,055 m` em ciclo de `1,6 s`;
- pulso senoidal suavizado da emissão;
- fase derivada da posição para reduzir sincronização entre instâncias.

Não há Animator, alocação contínua, `GetComponent` em `Update` ou alteração de material compartilhado.

## 9. Animação de coleta

`PlayCollectAnimation()` inicia uma sequência de `0,42 s`: acelera a rotação, sobe `0,22 m`, cresce até `1,15`, aplica flash ciano, reduz a escala a zero, invoca `OnCollectAnimationCompleted` e desativa o GameObject para permitir pooling.

## 10. Integração ao gameplay

`AuroraCoinCollectible` procura `PlayerHealth` no objeto que entra no trigger, seguindo o padrão já usado pelos DataFiles. A coleta desativa imediatamente o trigger, impede duplicação, dispara `OnCollected` e inicia a animação visual.

Não foi encontrado um sistema existente de moeda/currency adequado. Por isso, nenhum manager paralelo foi criado. O valor padrão é `1` e a recompensa deve ser conectada ao `UnityEvent OnCollected` quando o sistema oficial for definido.

Referências opcionais para `AudioClip`, `AudioSource` e `ParticleSystem` ficaram expostas, sem baixar ou criar conteúdo externo.

## 11. Arquivo Blender

`C:/ProjetoAurora-Game/SourceAssets/Blender/AuroraCoin/Aurora_HoloCoin.blend`

Gerador reproduzível:

`C:/ProjetoAurora-Game/tools/blender/create_aurora_holo_coin.py`

## 12. Arquivo FBX

`Assets/_ProjectAurora/Art/Collectibles/AuroraCoin/Models/Aurora_HoloCoin.fbx`

Exportado com objetos selecionados, unidade métrica, `-Z Forward`, `Y Up`, sem câmera, luz, rig ou animação baked.

## 13. Prefab

`Assets/_ProjectAurora/Prefabs/Collectibles/PF_Aurora_HoloCoin.prefab`

Comandos de manutenção:

- `Tools/Projeto Aurora/Collectibles/Rebuild Aurora HoloCoin`.
- `Tools/Projeto Aurora/Collectibles/Validate Aurora HoloCoin`.

## 14. Validação e pendências

Validações concluídas:

- script Blender compilado com `python -m py_compile`;
- `.blend` salvo e FBX exportado pelo Blender 5.1.2;
- frente e verso renderizados para inspeção;
- três scripts Unity validados sem erros de compilação;
- validação final do Editor: `meshes=5`, `vertices=3510`, `triangles=3184`, `bounds=(0.461, 0.461, 0.126)`, `materials=3`, trigger correto, Rigidbody cinemático e `collectAnimation=pass`;
- Console limpo na validação isolada, sem warnings ou erros do asset;
- Idle confirmado em Play Mode com rotação e flutuação;
- instância e luz temporárias removidas da `Beta03_Principal`, sem salvar alteração de progressão.

Evidências visuais:

- `Assets/_ProjectAurora/Docs/AuroraCoinValidationShots/AuroraCoin_Unity_EditMode_Lit.png`.
- `Assets/_ProjectAurora/Docs/AuroraCoinValidationShots/AuroraCoin_Unity_PlayMode_Idle.png`.
- `SourceAssets/Blender/AuroraCoin/Aurora_HoloCoin_Preview.png`.
- `SourceAssets/Blender/AuroraCoin/Aurora_HoloCoin_Preview_Back.png`.

Pendências intencionais:

- conectar `OnCollected` ao sistema oficial de moeda/recompensa quando ele existir;
- atribuir SFX e burst de partículas aprovados pelo projeto;
- posicionar instâncias oficiais nas fases e validar contraste nos corredores ciano e vermelhos;
- a `Beta03_Principal` já apresenta `ArgumentNullException` e avisos de fonte durante Play Mode, anteriores e não relacionados ao HoloCoin; investigar separadamente antes da validação integral da cena.
