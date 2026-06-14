# PROJETO:AURORA - Falha de Contenção

Beta 0.2 mecânica de um runner narrativo 3D feito em Unity 6 com primitivas. Dr. Elias atravessa seis setores enquanto a CelestIA orienta o jogador e gradualmente apresenta sinais de corrupção.

## Abrir e rodar

1. Abra `C:\ProjetoAuroraGame` no Unity Hub.
2. Abra `Assets/Scenes/MainMenu.unity`.
3. Pressione Play e clique em **Jogar**.

As cenas `MainMenu` e `Game` já estão configuradas no Build Settings.

## Controles

- `A` ou `Seta esquerda`: mover uma faixa para a esquerda
- `D` ou `Seta direita`: mover uma faixa para a direita
- `Espaço`: pular
- `E`: interagir com painéis, lasers e terminal
- `Esc`: pausar ou continuar

## Beta 0.2

- Percurso de 2700 metros, dividido em seis setores de 450 metros.
- Velocidade progressiva de 8 m/s até 16 m/s.
- Tutorial obrigatório de movimento, salto e interação.
- Portas, painéis e lasers interativos.
- Três vidas, invulnerabilidade, piscar e lentidão após impacto.
- Card de entrada para cada novo setor.
- CelestIA em vídeo com estados normal, transição e corrompida.
- Terminal Central exige interação com `E`.
- Música reiniciada a cada nova tentativa e interrompida no menu ou Game Over.

## Arquivos de mídia

- Música: `Assets/Audio/Falha de Contenção.mp3`
- CelestIA normal: `Assets/Videos/CelestIA/Celestia01.mp4`
- Transição: `Assets/Videos/CelestIA/Celestia02.mp4`
- CelestIA corrompida: `Assets/Videos/CelestIA/Celestia03.mp4`

O jogo continua funcionando caso algum arquivo esteja ausente, registrando um aviso no Console.

Os vídeos fornecidos podem gerar um aviso do Windows Media Foundation sobre timestamps ou primárias de cor. Isso vem da codificação original dos MP4 e não interrompe a reprodução.

## Regenerar cenas

Use `Tools > Projeto Aurora > Build Beta 0.2`. O gerador recria as cenas, materiais, HUD, corredor, obstáculos e referências de mídia.

## Próximos passos

- Substituir primitivas por arte final.
- Produzir versões dos vídeos otimizadas para reprodução multiplataforma.
- Adicionar agachamento e novos padrões de laser.
- Criar configurações reais de áudio, vídeo e acessibilidade.
