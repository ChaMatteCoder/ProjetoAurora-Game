# AuroraSky - background em vídeo

Data: 17/07/2026

Cena canônica: `Assets/_ProjectAurora/Scenes/Beta03_Principal.unity`

## Implementação

O fundo está no objeto `AuroraSky Video Background`, com `VideoPlayer` e
`AuroraSkyVideoController`. O vídeo é desenhado diretamente no `CameraFarPlane` da
`Main Camera`, usa `VideoAspectRatio.FitInside`, loop, `waitForFirstFrame` e
`skipOnDrop`. Não há áudio nem `RenderTexture` intermediária.

Essa composição mantém o quadro 16:9 inteiro atrás da geometria e evita a projeção
esférica que curvava e recortava a imagem estática anterior.

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
