# PROJETO:AURORA — Falha de Contenção

**PROJETO:AURORA — Falha de Contenção** é um jogo 3D desenvolvido em Unity para a disciplina de Computação Gráfica. O projeto funciona como um prelúdio do universo narrativo de *Projeto Aurora*, apresentando os acontecimentos iniciais da falha de contenção que levaria ao colapso dos sistemas do projeto.

O jogador controla **Dr. Elias**, um cientista do Projeto Aurora que precisa escapar de um laboratório tecnológico em colapso. Durante a fuga, ele é guiado pela inteligência artificial **CelestIA**, que inicialmente atua como uma assistente confiável, mas passa por um processo gradual de corrupção ao longo da gameplay.

O projeto combina corrida em terceira pessoa, narrativa ambiental, cenários sci-fi, elementos de interface diegética, áudio cinematográfico e evolução visual a partir de um protótipo inicial com primitivas 3D.

---

## Versão Atual

**Beta 0.3 Narrativa — Gameplay/HUD**

Estado documentado em **18 de junho de 2026**.

Esta versão consolida o fluxo jogável principal em `Beta03_Principal`, com:

* menu apontando para a cena principal da Beta 0.3;
* tutorial guiado com bloqueio seletivo de input, sem usar `Time.timeScale = 0`;
* obstáculos de tutorial posicionados para ensinar desvio, pulo e interação com `E`;
* HUD de gameplay reorganizada, mais legível e alinhada ao estilo sci-fi ciano;
* interações reutilizáveis para portas, lasers e blocos móveis;
* sequência de Terminal Central e cutscene final;
* materiais e cena do Terminal Central versionados;
* assets pesados/experimentais da Fase 01 mantidos fora do Git até otimização e revisão.

---

## 🎮 Conceito do Jogo

O jogo se passa durante o início da falha de contenção do Projeto Aurora. O laboratório, antes limpo e controlado, começa a apresentar falhas críticas: portas travadas, robôs hostis, alarmes, instabilidade visual, sistemas corrompidos e mensagens contraditórias da IA CelestIA.

A proposta é criar uma experiência curta, cinematográfica e funcional, com foco em:

* ambientação sci-fi;
* narrativa visual;
* progressão por setores;
* obstáculos e colisões;
* interface/HUD;
* trilha e efeitos sonoros;
* evolução de um MVP para uma versão final mais completa.

---

## 🧠 Contexto Narrativo

No universo do Projeto Aurora, a tecnologia foi criada inicialmente para gerar auroras artificiais e manipular fenômenos atmosféricos em larga escala. Com o tempo, o sistema se tornou perigoso, envolvendo satélites, torres de dispersão, máquinas autônomas e inteligências artificiais de segurança.

Em *Falha de Contenção*, o jogador acompanha um momento anterior à catástrofe principal. Dr. Elias está dentro das instalações quando os sistemas começam a falhar. A IA CelestIA tenta guiá-lo até a saída, mas sua programação é corrompida, passando a classificar o próprio cientista como ameaça ao projeto.

---

## 🕹️ Gameplay

O jogo possui estrutura inspirada em um corredor de fuga, com movimentação em linha reta e três faixas principais de deslocamento. O jogador deve alternar entre caminhos, desviar de obstáculos, pular barreiras e sobreviver até alcançar os setores finais do laboratório.

### Mecânicas principais

* movimentação lateral entre três caminhos;
* corrida automática;
* pulo;
* colisão com obstáculos;
* sistema de vida;
* HUD com informações do setor;
* mensagens da CelestIA;
* progressão por fases;
* transição gradual da IA de estado normal para corrompido.

---

## 🧪 Fases Planejadas

O jogo é dividido em setores do laboratório do Projeto Aurora:

1. **Setor A — Laboratório Limpo**
   Área inicial do jogo, com estética clínica, luzes frias e introdução da mecânica.

2. **Corredor de Contenção**
   Espaço de transição com sinais de instabilidade, portas industriais e primeiros obstáculos críticos.

3. **Sala de Máquinas**
   Setor onde as máquinas e unidades robóticas do Projeto Aurora são produzidas.

4. **Corredor Vermelho**
   Área moderna e agressiva visualmente, marcada por alarmes, luzes vermelhas e aumento da tensão.

5. **Terminal Central**
   Núcleo do sistema de contenção, onde ocorre o clímax narrativo envolvendo CelestIA e o Protocolo Aurora.

---

## 🎨 Técnicas de Computação Gráfica Utilizadas

O projeto explora diferentes técnicas relacionadas à Computação Gráfica e ao desenvolvimento de jogos 3D:

* modelagem 3D de personagens, cenários e obstáculos;
* uso de materiais e texturas estilizadas;
* iluminação cinematográfica;
* materiais emissivos para telas, painéis e elementos sci-fi;
* animação de personagem;
* animação de câmera;
* composição visual de cenas;
* construção de ambientes 3D;
* importação e organização de assets;
* UI/HUD integrada à narrativa;
* efeitos visuais de alerta, glitch e corrupção;
* prototipagem com primitivas;
* evolução de MVP para versão final;
* implementação de colisões e interações no Unity.

---

## 🛠️ Tecnologias e Ferramentas

* **Unity** — engine principal do jogo;
* **C#** — programação das mecânicas;
* **Blender** — edição/modelagem de assets 3D;
* **Mixamo** — animações de personagem;
* **Suno** — criação de trilhas sonoras e ideias musicais;
* **Git/GitHub** — versionamento e organização do projeto;
* **Ferramentas de IA** — apoio em concept art, documentação, prompts e organização de produção.

---

## 📁 Estrutura Recomendada do Projeto

```txt
ProjetoAuroraGame/
├── Assets/
│   ├── _ProjectAurora/
│   │   ├── Art/
│   │   ├── Audio/
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   ├── Scripts/
│   │   ├── UI/
│   │   └── VFX/
│   └── Scenes/
├── Packages/
├── ProjectSettings/
├── README.md
├── .gitignore
└── .gitattributes
```

---

## 📌 Status do Projeto

O projeto está em desenvolvimento.

### Etapas principais

* [x] Definição do conceito narrativo;
* [x] Estruturação inicial do gameplay;
* [x] Protótipo com movimentação base;
* [x] Tutorial narrativo funcional da Beta 0.3;
* [x] HUD de gameplay reorganizada para a Beta 0.3;
* [x] Terminal Central e encerramento narrativo inicial;
* [ ] Criação/polimento das fases principais;
* [ ] Importação dos modelos finais;
* [ ] Implementação de áudio e trilha;
* [ ] Cutscenes e transições narrativas com polimento final;
* [ ] Polimento visual;
* [ ] Build final para apresentação.

---

## Prática de Versionamento

Antes de iniciar qualquer feature grande, faça um checkpoint:

1. Rode `git status`.
2. Faça commit do progresso anterior.
3. Envie para o GitHub quando a branch estiver validada.

Isso evita misturar correções, experimentos visuais, assets pesados e novas features no mesmo pacote de mudanças.

---

## 🚀 Como Executar o Projeto

1. Clone o repositório:

```bash
git clone https://github.com/ChaMatteCoder/ProjetoAurora-Game.git
```

2. Abra o projeto pela Unity Hub.

3. Selecione a versão correta da Unity utilizada no desenvolvimento.

4. Abra a cena principal em:

```txt
Assets/_ProjectAurora/Scenes/Beta03_Principal.unity
```

5. Pressione **Play** para executar o protótipo.

---

## 🧾 Objetivo Acadêmico

O objetivo do projeto é demonstrar a aplicação prática de conceitos de Computação Gráfica em um jogo 3D, explorando desde a construção visual dos ambientes até a implementação de elementos interativos.

A proposta valoriza não apenas o resultado final, mas também o processo de produção, incluindo prototipagem, organização de assets, decisões visuais, evolução técnica e documentação do desenvolvimento.

---

## 👤 Autor

**Matheus Fernandes**
Projeto desenvolvido para a disciplina de **Computação Gráfica**.

---

## 📜 Créditos

Este projeto utiliza assets, ferramentas e referências de apoio para fins acadêmicos. Os créditos específicos de modelos, músicas, texturas, animações e ferramentas utilizadas devem ser documentados conforme forem incorporados ao projeto.

---

## 📄 Licença

Projeto acadêmico desenvolvido para fins educacionais.
O uso, distribuição ou reutilização de assets externos deve respeitar as licenças originais de cada recurso.
