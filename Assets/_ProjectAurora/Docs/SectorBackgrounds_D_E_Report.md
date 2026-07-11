# Backgrounds de Setor D & E — Relatório (Round 19)

Backdrops estáticos (matte painting) para o **Setor D — Corredor Vermelho** e o
**Setor E — Ponte Técnica**, na cena `Beta03_Principal`. Técnica: planos 3D com material
**URP/Unlit**, integrados a fog/luz, emoldurados pela própria arquitetura do corredor.
Nenhum skybox, nenhuma esfera, nenhum panorama 360°, nenhum Sprite de UI.

Data: 07/07/2026.

---

## 1. Assets movidos
Da raiz do projeto para dentro de `Assets/`:

| Antes (raiz) | Depois |
|---|---|
| `SetorD_Background.png` | `Assets/_ProjectAurora/Art/Backgrounds/Sectors/SetorD_Background.png` |
| `SetorE_Background.png` | `Assets/_ProjectAurora/Art/Backgrounds/Sectors/SetorE_Background.png` |

A raiz ficou **sem** os PNGs. Ambos 1916×821 (~2.334:1).

## 2. Import settings aplicados (as duas texturas)
- Texture Type: **Default** (não Sprite)
- sRGB: **true**
- Alpha Is Transparency: **false** (Alpha Source = None — imagens opacas)
- Mip Maps: **true**
- Wrap Mode: **Clamp**
- Filter Mode: **Trilinear**
- Compression: **High Quality**
- Max Size: **2048** (nativo ~1916, sem upscale desnecessário)

## 3. Materiais criados
Pasta `Assets/_ProjectAurora/Materials/Backgrounds/`:

| Material | Shader | Textura | Tint | Sombras |
|---|---|---|---|---|
| `MAT_SetorD_Background_Unlit.mat` | URP/Unlit | SetorD_Background | `(1.00, 0.92, 0.92)` leve quente | não recebe/projeta |
| `MAT_SetorE_Background_Unlit.mat` | URP/Unlit | SetorE_Background | `(0.92, 0.96, 1.00)` leve ciano | não recebe/projeta |

Unlit + o fog do URP fazem o backdrop **fundir-se ao fog** na distância (revelação
gradual). Tints sutis preservam a arte. GPU instancing ligado.

## 4. Onde ficou o background do Setor D
- Root de cena: `Gameplay_Backgrounds/SectorD_RedCorridor_Backdrop` (Quad).
- Posição: **(0, 4.5, 1797)** — no **fundo do corredor**, logo antes do arco de transição
  D→E (z≈1800). Setor D ocupa z 1350–1800 (`sectorLength=450`).
- Escala: **(37.34, 16, 1)** → altura 16 × largura 34.9 (aspect 2.334 preservado).
- Face voltada para o player (−Z), back-face culling (invisível ao ser ultrapassado).
- As paredes/teto/chão do corredor **emolduram** o plano e escondem suas bordas; o fog
  o revela conforme o jogador se aproxima do fim do setor. Combina com a iluminação
  vermelha e a corrupção (bulkhead "D-07", "CONTAINMENT FAILURE", "CELESTIA OVERRIDE").

## 5. Onde ficou o background do Setor E
- Root de cena: `Gameplay_Backgrounds/SectorE_TechnicalBridge_Backdrop` (Quad).
- Posição: **(0, 4.5, 2243)** — no fundo da Ponte Técnica (Setor E: z 1800–2250), antes
  da transição para o NÚCLEO.
- Escala: **(37.34, 16, 1)** (aspect 2.334 preservado).
- Dá **profundidade/altura/vazio**: a ponte se abre para a vista distante com a **aurora
  azul/roxa** e as megaestruturas. Bordas escondidas pela estrutura do corredor + fog +
  props (caixas) à frente. Tint ciano casa com o setor.

## 6. Como a proporção 2.33:1 foi preservada
Aspect = 1916/821 ≈ **2.334**. Os planos usam `scaleX = altura × 2.334`, `scaleZ = 1`
(altura 16 → largura 34.9). Nunca há escala independente que estique/achate: a razão
X/Y do plano é sempre igual à razão da imagem. Wrap **Clamp** evita repetição nas bordas.

## 7. Parallax
Componente `SectorBackdropParallax` (em `Scripts/Environment/`) **anexado nos dois
backdrops, porém desligado por padrão** (`enableParallax = false`) — os backdrops
funcionam perfeitamente estáticos. É seguro e sutil: baseia-se no **desvio** da câmera
em relação à referência inicial (não acumula, não "foge" com o avanço contínuo do
runner), sem alocação, `LateUpdate` simples. Para ativar: marcar `enableParallax` e
ajustar `parallaxStrengthX` (~0.02). Recomenda-se manter `parallaxStrengthZ = 0` em
runner de avanço contínuo.

## 8. Integração com fog/luz
- Fog da cena: **Linear**, cor `(0.035, 0.055, 0.085)`, start 105 / end 285 (não
  alterado — global). O Unlit respeita o fog, então os backdrops **emergem do fog** ao
  serem aproximados, evitando aparência de "tela chapada".
- Nenhuma luz realtime nova foi criada; nenhum collider; nenhuma sombra
  (cast/receive desligados). A iluminação vermelha (D) e a atmosfera (E) já existentes
  compõem com o backdrop, e props/estruturas à frente quebram as bordas.

## 9. Como testar
1. Abrir `Beta03_Principal`.
2. Play; jogar até o **Setor D** (HUD "SETOR D: Corredor Vermelho", ~z1350–1800).
   Para teste rápido no editor: em Play, teleportar o player para z≈1700 (ou usar o
   PULAR TUTORIAL e correr). O bulkhead vermelho surge do fog no fim do corredor.
3. Confirmar: aparece, sem deformação, não bloqueia obstáculos, casa com a luz vermelha.
4. Seguir até o **Setor E** (HUD "SETOR E: Ponte Técnica", ~z1800–2250): a ponte se abre
   para a aurora ao fundo, dando profundidade; não parece skybox distorcido.
5. Verificar Console (sem erros vermelhos) e performance (sem queda relevante).

## 10. Performance
- **2 planos** no total (1 por setor), sem collider, sem Rigidbody, sem Update ativo
  (parallax desligado), materiais dedicados com instancing, textura 2048 HQ, sem sombras.
  Impacto desprezível (2 draw calls Unlit, quase sempre fora de tela/atrás do fog).

## 11. Pendências / preparação para vídeo futuro
- Estrutura pronta para trocar o **matte estático por vídeo animado**:
  - roots e nomes claros (`Gameplay_Backgrounds/Sector*_Backdrop`);
  - materiais separados por setor;
  - `SectorBackdropParallax` só mexe no Transform (desacoplado da textura).
- **Para animar depois:** trocar o material do plano por um que receba uma
  `RenderTexture` alimentada por um `VideoPlayer` (Render Mode = Render Texture), ou
  apontar o `VideoPlayer` para um Renderer com material Unlit. Nada mais precisa mudar.
- Presença dos backdrops é intencionalmente **sutil ao longe** (fog) e **plena ao
  aproximar** do fim de cada setor. Se no futuro quiserem presença maior no meio do
  setor, considerar reduzir localmente o fog ou adicionar uma 2ª camada mais próxima
  (parallax) — deixado como evolução, fora do escopo desta rodada.

## Arquivos tocados
- `Assets/_ProjectAurora/Art/Backgrounds/Sectors/SetorD_Background.png` (movido + import)
- `Assets/_ProjectAurora/Art/Backgrounds/Sectors/SetorE_Background.png` (movido + import)
- `Assets/_ProjectAurora/Materials/Backgrounds/MAT_SetorD_Background_Unlit.mat` (novo)
- `Assets/_ProjectAurora/Materials/Backgrounds/MAT_SetorE_Background_Unlit.mat` (novo)
- `Assets/_ProjectAurora/Scripts/Environment/SectorBackdropParallax.cs` (novo)
- `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity` (root `Gameplay_Backgrounds` + 2 backdrops)
