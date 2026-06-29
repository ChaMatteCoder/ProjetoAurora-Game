# MenuPolishReport

## Assets PNG

- `Card.png` foi movido da raiz do projeto para `Assets/_ProjectAurora/Art/Menu/UI/Card.png`.
- `Icons.png` foi movido da raiz do projeto para `Assets/_ProjectAurora/Art/Menu/UI/Icons.png`.
- A raiz do projeto nao mantem mais `Card.png` nem `Icons.png`.

## Transparencia

- `Card.png` e `Icons.png` ja possuem transparencia real.
- Nao foram gerados `Card_Transparent.png` nem `Icons_Transparent.png`, porque a remocao automatica de branco nao foi necessaria.
- Os poucos pixels near-white opacos foram mantidos como brilho/anti-alias dos elementos.

## Importacao e Sprites

- `Card.png` foi configurado como Sprite (2D and UI), Single, sem mip maps, alpha transparency ativo, filtro Bilinear e compressao Uncompressed.
- `Icons.png` foi configurado como Sprite (2D and UI), Multiple, sem mip maps, alpha transparency ativo, filtro Bilinear e compressao Uncompressed.
- `Icons.png` foi fatiado em 5 sprites horizontais:
  - `Icon_Play`
  - `Icon_Settings`
  - `Icon_Extra`
  - `Icon_Credits`
  - `Icon_Quit`

## Menu Oficial

- Cena alterada: `Assets/_ProjectAurora/Scenes/MainMenu.unity`.
- Controller preservado: `Assets/_ProjectAurora/Scripts/UI/Menu/AuroraMainMenuController.cs`.
- Os cinco cards do `MenuButtonsPanel` usam `Card.png` como base visual.
- Cada botao possui `Image_Icon` com o sprite correto e `Text_Label` em TextMeshPro.
- Os cards foram reduzidos para `500x76` com espacamento menor, mantendo os botoes atras do video e sem SVG.
- Elementos antigos desativados foram agrupados em `Legacy_MenuVisuals` quando aplicavel.

## Video

- `Assets/_ProjectAurora/Art/Menu/Characters/Dr.Elias_Loop.mp4` foi configurado no `VideoPlayer_DrEliasLoop`.
- O video usa `RT_DrEliasLoop.renderTexture` e `RawImage_DrEliasLoopBackground`.
- Loop simples ativo: sem reverse, sem ping-pong, sem `playbackSpeed` negativo.
- O fallback de imagem foi removido da cena; o fundo visivel do menu e somente o video.
- O import do video esta sem transcoding forcado, sem audio, sem flip horizontal/vertical.
- RawImage do video nao bloqueia raycasts dos botoes.

## Como Testar

1. Abrir `Assets/_ProjectAurora/Scenes/MainMenu.unity`.
2. Apertar Play.
3. Confirmar que o video `Dr.Elias_Loop.mp4` aparece e toca em loop.
4. Confirmar que os cinco botoes usam o card PNG e os icones corretos.
5. Passar o mouse nos botoes e verificar hover discreto.
6. Abrir e fechar Configuracoes, Extra e Creditos.
7. Clicar Jogar e confirmar carregamento de `Beta03_Principal`.
8. Verificar o Console.
