# PROJETO:AURORA — Falha de Contenção

**PROJETO:AURORA — Falha de Contenção** é um jogo 3D desenvolvido em Unity para a disciplina de Computação Gráfica. O projeto funciona como um prelúdio do universo narrativo de *Projeto Aurora*, apresentando os acontecimentos iniciais da falha de contenção que levaria ao colapso dos sistemas do projeto.

O jogador controla **Dr. Elias**, um cientista do Projeto Aurora que precisa escapar de um laboratório tecnológico em colapso. Durante a fuga, ele é guiado pela inteligência artificial **CelestIA**, que inicialmente atua como uma assistente confiável, mas passa por um processo gradual de corrupção ao longo da gameplay.

O projeto combina corrida em terceira pessoa, narrativa ambiental, cenários sci-fi, elementos de interface diegética, áudio cinematográfico e evolução visual a partir de um protótipo inicial com primitivas 3D.

---

## Versão Atual

**Beta jogável — candidata à primeira build**

Estado documentado em **6 de julho de 2026**.

O fluxo completo `MainMenu → Beta03_Principal → Terminal Central` está jogável de ponta
a ponta. Sistemas consolidados:

* **intro cinematográfica** na sala do Dr. Elias — laboratório sci-fi com arcos, mesa
  holográfica e interface da CelestIA; o Dr. Elias só é revelado após o alerta, com
  **sirene 3D** que se afasta pela distância; opção de pular por `ESC`;
* **dublagem por ID** (`VoiceLinePlayer`): CelestIA e Dr. Elias com fila, prioridades e
  grupos; transições de música com **fade** (sem estouro de volume);
* **HUD com retratos em vídeo** de CelestIA (3 estados, com **glitch** quando corrompida)
  e Dr. Elias (2 humores), card exibido apenas durante fala ativa e falante correto por ID;
  HUD de distância e **overlay de setor animado**;
* **tutorial guiado** com liberação de ação após a fala da CelestIA, **setas direcionais
  corretas** e indicador **“E” hexagonal** — sem usar `Time.timeScale = 0`;
* **painéis interativos modelados** (asset 3D) com tela iluminada e **marcador “E”
  flutuante** visível de longe em todos os painéis;
* **perseguição cinematográfica por robôs** (replay visual, sem física) e **obstáculos
  finais** (lasers desativáveis, robôs, barreiras) com dificuldade justa;
* **Setor E / Ponte Técnica** com colapso ambiental animado e **sprint final** rumo ao Núcleo;
* **Terminal Central** reestruturado por referências (núcleo luminoso, cryo-tubes, braços
  robóticos, telas), com **luzes que acendem conforme os passos** do Dr. Elias;
* **cutscene final** sincronizada com o diálogo — os robôs chegam apenas no “Não…” (ELI_010),
  em enquadramento de censura (o Dr. Elias nunca aparece sendo pego); HUD oculta;
* **recuperação de traje** (Suit Recovery), **anti-softlock** de portas, **menu
  reestruturado** (configurações persistentes, pausa, “Jogar” assíncrono) e **Game Over**.

Binários pesados (modelos, vídeos, áudios) são versionados via **Git LFS** — veja
[Como Executar](#-como-executar-o-projeto).

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
* corrida automática e pulo;
* colisão com obstáculos e sistema de integridade do traje (com recuperação gradual);
* interação com painéis (`E`) para portas e desativação de lasers;
* perseguição por unidades robóticas;
* HUD com setor, integridade, distância e comunicador da CelestIA;
* dublagem por ID e retratos em vídeo dos personagens;
* portas de transição automáticas e overlay de mudança de setor;
* progressão por setores com transição gradual da IA de normal para corrompido;
* menu com configurações persistentes e pausa em jogo.

---

## 🧪 Fases Planejadas

O jogo é dividido em setores do laboratório do Projeto Aurora:

1. **Setor A — Laboratório Limpo**
   Área inicial do jogo, com estética clínica, luzes frias e introdução da mecânica.

2. **Setor B — Corredor de Contenção**
   Espaço de transição com sinais de instabilidade, portas industriais e primeiros obstáculos críticos.

3. **Setor C — Sala de Máquinas**
   Setor onde as máquinas e unidades robóticas do Projeto Aurora são produzidas; início da perseguição.

4. **Setor D — Corredor Vermelho**
   Área agressiva visualmente, marcada por alarmes, luzes vermelhas e a corrupção crescente da CelestIA.

5. **Setor E — Ponte Técnica**
   Travessia tensa sobre a estrutura, com a IA já instável.

6. **Núcleo — Terminal Central**
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

* **Unity** (URP) — engine principal do jogo;
* **C#** — programação das mecânicas;
* **Blender** — edição/modelagem de assets 3D;
* **Tripo** — geração do modelo 3D do robô;
* **Mixamo** — animações de personagem (retarget humanoide);
* **ElevenLabs** — dublagem das falas de CelestIA e Dr. Elias;
* **Suno** — criação de trilhas sonoras e ideias musicais;
* **Git/GitHub + Git LFS** — versionamento (LFS para modelos, vídeos e áudios);
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
* [x] Tutorial narrativo com liberação de ação após fala + setas;
* [x] HUD de gameplay com retratos em vídeo e visibilidade por estado;
* [x] Dublagem por ID (CelestIA + Dr. Elias);
* [x] Perseguição por robôs e obstáculos reais;
* [x] Recuperação de traje e balanceamento de dificuldade;
* [x] Portas de transição de setor + overlays;
* [x] Menu com configurações persistentes e pausa;
* [x] Terminal Central e cutscene final;
* [ ] Polimento visual final e passes de otimização;
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

> **Importante:** o projeto usa **Git LFS** para modelos, vídeos e áudios. Instale o
> Git LFS **antes** de clonar (`git lfs install`), senão esses arquivos virão apenas como
> ponteiros e o jogo abrirá sem o robô, os vídeos da HUD e a dublagem.

1. Instale e ative o Git LFS (uma vez por máquina):

```bash
git lfs install
```

2. Clone o repositório (o LFS baixa os binários automaticamente):

```bash
git clone https://github.com/ChaMatteCoder/ProjetoAurora-Game.git
```

Se já tiver clonado sem LFS, rode `git lfs pull` na pasta do projeto.

3. Abra o projeto pela Unity Hub, selecionando a versão da Unity usada no desenvolvimento (Unity 6 / `6000.4.x`).

4. Abra a cena principal em:

```txt
Assets/_ProjectAurora/Scenes/MainMenu.unity
```

5. Pressione **Play** e clique em **Jogar** (ou abra `Beta03_Principal.unity` para ir direto à gameplay).

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
