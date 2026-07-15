# Menu de Skins - Preview 3D

## Estrutura

`SkinPreviewSystem` vive na `MainMenu` e contém:

- `PreviewRoot`;
- `PreviewCharacterAnchor`;
- `PreviewCamera`;
- `PreviewKeyLight`;
- `PreviewFillLight`;
- `PreviewBackLight`;
- `PreviewBackdrop`;
- `PreviewFloor`.

A saída usa `Assets/_ProjectAurora/Art/Skin/RenderTextures/RT_SkinPreview.renderTexture`, em 1024x1024, exibida no `PreviewRawImage` quadrado.

## Isolamento

- Layer exclusiva: `SkinPreview`.
- `PreviewCamera.cullingMask`: somente `SkinPreview`.
- Câmera principal: exclui `SkinPreview`.
- As três luzes afetam somente essa layer.
- Todos os filhos instanciados recebem a layer e tag `Untagged`.
- Colliders são desativados.
- Rigidbodies ficam cinemáticos e sem colisão.
- `AudioSource`, `Animator` e `MonoBehaviour` do prefab são desativados.

O preview não executa lógica de Player, corrida, vida, áudio ou gameplay.

## T-pose

O Dr. Elias usa o prefab de preview gerado a partir do visual oficial. O `Animator` é removido da execução e a pose de referência do FBX é preservada. A definição padrão aplica rotação Y de 90 graus para apresentar o personagem frontalmente.

O rig e o FBX originais não foram modificados. Para futuras skins, a prioridade continua sendo um prefab salvo em pose de referência; se isso não existir, a definição deve documentar a pose disponível.

## Enquadramento automático

Ao abrir uma entrada com modelo, `AuroraSkinPreviewController`:

1. destrói o preview anterior;
2. instancia um único prefab;
3. aplica escala, rotação e offset da definição;
4. combina os bounds de todos os `Renderer` ativos;
5. centraliza X/Z e apoia o menor Y no piso;
6. calcula FOV vertical e horizontal conforme o aspecto da RenderTexture;
7. escolhe a distância necessária para conter altura, largura e profundidade;
8. aplica margem de enquadramento e ajusta near/far clip.

O cálculo usa os bounds reais de cada modelo; não depende de uma distância fixa global.

## Ciclo de vida

- A câmera inicia desligada.
- Ela liga somente quando o painel está aberto e há preview válido.
- Navegar destrói a instância anterior antes de criar outra.
- Fechar o painel desliga a câmera e destrói a instância.
- Uma skin sem modelo oculta a `RawImage`, evita frame residual e mostra `MODELO 3D INDISPONÍVEL`.

## Iluminação

O setup usa key light branca suave, fill fraca e back light ciano, sem pós-processamento adicional. Backdrop e piso são simples e não recebem lógica.

## Evidência visual

- `Assets/_ProjectAurora/Docs/SkinMenuValidationShots/SkinMenu_Default_1920x1080.png`
- `Assets/_ProjectAurora/Docs/SkinMenuValidationShots/SkinMenu_Default_1280x720.png`
- `Assets/_ProjectAurora/Docs/SkinMenuValidationShots/SkinMenu_Brazil_NoModel_1920x1080.png`
