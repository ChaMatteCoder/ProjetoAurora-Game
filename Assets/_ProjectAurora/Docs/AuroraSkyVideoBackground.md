# AuroraSky - background em vídeo

Data: 17/07/2026

Cena canônica: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## Implementação

> **Atualizado em 21/07/2026.** A descrição anterior (`CameraFarPlane`) ficou obsoleta:
> o `CameraFarPlane` prendia o quadro à TELA, então trocar de faixa fazia o cenário
> "deslizar" sobre um fundo imóvel. O caminho atual prende o céu ao MUNDO.

O fundo está no objeto `AuroraSky Video Background`, com `VideoPlayer` e
`AuroraSkyVideoController`. O caminho de renderização real é:

```
VideoClip → RT_AuroraSky (RenderTexture) → MAT_AuroraSky (Skybox/Panoramic) → domo do céu
```

O `VideoPlayer` usa `renderMode = RenderTexture`, `aspectRatio = Stretch`, loop,
`waitForFirstFrame` e `skipOnDrop`, sem áudio. O material é `Skybox/Panoramic` com
`_Mapping = Latitude Longitude` e `_ImageType = 360°`.

### Consequência de resolução (importante)

Por ser um panorama **360°×180°**, a textura é esticada pela esfera inteira. A densidade
de pixels resultante é MUITO menor que a resolução nominal do clipe — panoramas lat-long
normalmente pedem 4096x2048 ou mais. **O clipe de 1280x720 é o teto de qualidade atual e
a causa da pixelização.** Aumentar apenas a `RenderTexture` não resolve: ela já casa com
o clipe, e ampliar acima da fonte só interpola.

Testado e descartado: `_ImageType = 180°` (dobraria a densidade) deixa o céu preto na
área visível com a orientação atual (`_Rotation = 180`).

## Compatibilidade de plataforma (codec)

O importador **precisa** de `enableTranscoding = true` com `codec = VP8`.

O player Linux do Unity não tem decodificador H.264 garantido (depende de bibliotecas do
sistema). Com o `.mp4` H.264 embarcado cru (`enableTranscoding = false`), o vídeo não
decodifica no Linux, nada é escrito na `RT_AuroraSky`, e o skybox exibe a RenderTexture
vazia — **céu preto**. Foi o bug reportado na build Linux de 21/07/2026.

Além do codec, o campo `fallbackTexture` do controller deve estar preenchido
(`AuroraSky_Fallback.png`). Ele estava nulo, então `ApplySkyboxTexture(fallback)` era
no-op e não havia rede de proteção: qualquer falha do vídeo virava céu preto em vez de
uma imagem estática.

## Asset definitivo

| Campo | Original recebido | Versão no projeto |
|---|---:|---:|
| Resolução | 3840x2160 | 1280x720 |
| Frame rate | 29,97 fps | 29,97 fps |
| Duração | 16,9 s | 16,9 s |
| Áudio | 0 faixas | 0 faixas |
| Tamanho | 32.459.600 bytes | 5.237.739 bytes |
| Codec/cor | H.264 sem primárias declaradas | H.264 com `nclx` BT.709 |

Redução do fonte: 83,9%. O importador usa o arquivo diretamente, sem nova
transcodificação. O `.mp4` continua coberto pelo Git LFS.

## Validação visual

Captura em Game View 1280x720 no início do Setor E / Ponte Térmica, com jogador em
`z=1900` e câmera fixa atrás da pista:

- quadro completo e totalmente visível;
- sem estiramento, curvatura, barras ou recorte;
- estrelas e nuvens permanecem legíveis em movimento;
- `VideoPlayer` preparado, tocando e avançando frames em loop;
- console do primeiro carregamento: 0 erros e 0 warnings.

## Desempenho

Cinco amostras no mesmo enquadramento, Unity Editor, Game View 1280x720:

| Variante | Main thread | Render thread | Frame total | FPS equivalente |
|---|---:|---:|---:|---:|
| Imagem estática (baseline) | 9,42 ms | 3,43 ms | 12,51 ms | 79,9 |
| H.264 1080p transcodificado | 10,31 ms | 4,12 ms | 15,85 ms | 63,1 |
| VP8 720p | 13,04 ms | 5,37 ms | 16,68 ms | 59,9 |
| **H.264 720p direto (final)** | **10,18 ms** | **3,61 ms** | **14,37 ms** | **69,6** |

Na versão final, o frame total variou de 13,05 a 15,99 ms. As cinco amostras ficaram
abaixo do orçamento de 16,67 ms para 60 FPS. O custo médio sobre a imagem estática foi
de 1,86 ms, sem aumento de draw calls causado pelo fundo.

Os números são uma medição local no Editor, não substituem profiling de uma build
standalone nos requisitos mínimos de hardware.
