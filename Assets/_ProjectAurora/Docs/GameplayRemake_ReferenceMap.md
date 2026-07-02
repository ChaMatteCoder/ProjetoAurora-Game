# Gameplay Remake — Mapa de Referências Visuais

Todas as imagens analisadas em 2026-07-01. Cada fase corresponde a um trecho de 450u da cena canônica `Beta03_Principal.unity` (ver GameplayRemake_Audit.md).

## FASE 01 — Laboratório Limpo A (z 0–450)
**Imagens:** `Aurora_Box.png`, `A_obstáculo (2..5).png` (arte dos obstáculos)
**Assets:** materiais `MAT_Fase01_*` (Cenário/Materials), modelos GLB `Art/Generated/Obstacles/Aurora_Box_01/02`, `Aurora_Door_01`, `Aurora_Lazer_01/02`
**Identidade:** branco clínico + metal grafite + ciano único de acento; cápsulas de vidro com plantas; bancadas/terminais; luz fria zenital; strips ciano demarcando as 3 faixas; obstáculos brancos com warning amarelo/preto.
**Na cena:** ambiente v1 `Setor A - Laboratorio` já rico (arcos, vitrines, chevrons) — precisa de contraste/bloom e consolidação com v2 duplicado.

## FASE 02 — Setor de Contenção (z 450–900)
**Imagens:** `Inspiração (1).png`, `(2).png`, `(3).png` — REFERÊNCIA PRIORITÁRIA
**Identidade extraída:**
- Corredor industrial branco/cinza com piso grafite escuro e strips ciano
- **Barreiras de laser vermelho multi-feixe em postes** cruzando faixas
- Caixas de carga brancas com logo Aurora, faixas warning amarelo/preto e strips ciano
- Pórticos de segurança escuros ("AURORA") sobre a pista
- Vitrines de contenção retroiluminadas (hologramas de DNA, plantas)
- Postes-balizadores com strip ciano vertical; faixas amarelo/preto nas bordas das lanes
- Sinalização "SETOR DE CONTENÇÃO / ACESSO RESTRITO" em painéis escuros; braços robóticos (Insp. 2)
- Teto estrutural com vigas e luz branca fria; acentos vermelhos APENAS em lasers/alertas
**Na cena:** trocar acentos do trecho para ciano+vermelho de contenção, warning amarelo nos pés dos arcos (já existem `Arch Hazard Foot`), lasers vermelhos, sinalização.

## FASE 03 — Sala de Máquinas (z 900–1350)
**Imagens:** `Inspiração (1..3).png`
**Identidade:** hall industrial mais escuro; braços robóticos em vitrines de vidro azul; máquinas/racks; amarelo warning no chão; chevrons ciano intensos; teto alto escuro com estrutura aparente; painel "MACHINE CREATION DIVISION".
**Na cena:** escurecer paredes (metal escuro no lugar de branco), acentos azul-elétricos, mais densidade de "maquinário" nas laterais (reuso de cabinets/ducts), luz mais dura.

## FASE 04 — Corredor Vermelho (z 1350–1800)
**Imagens:** `Inspiração (1..3).png`
**Identidade:** ambiente escuro quase preto; TODA a linguagem emissiva vira vermelha (strips, arcos, chevrons, painéis "ALERTA VERMELHO/CRÍTICO"); lasers vermelhos; balizadores de emergência; logos Aurora ciano-pálido no chão como único contraponto.
**Na cena:** swap total de acentos ciano→vermelho no trecho, paredes escuras, luzes de emergência vermelhas, chevrons vermelhos.

## FASE 05 — Terminal Central (z 2250–2700) + Ponte Técnica (1800–2250)
**Imagens:** `Inspiração (1..3).png` (pasta FASE 05)
**Identidade:** câmara monumental com pilar/terminal central ciano brilhante; cápsulas de contenção com espécimes; escadaria/plataforma central; mistura ciano dominante + acentos vermelhos (portal de laser vermelho); painéis "FASE 05 — TERMINAL CENTRAL".
**Na cena:** `Fase05 - Terminal Central` já tem Chamber+Lead-In+Set Dressing e 19 luzes ciano/vermelhas; precisa de bloom, correção dos props enterrados e composição. Ponte Técnica (1800–2250) é o setor "corrompido" de transição: acentos vermelhos + ciano degradado.

## Regras transversais (todas as referências)
- Centro da pista sempre livre; obstáculos ocupam no máx. 2 de 3 faixas
- Chevrons luminosos no chão guiam o olhar para frente
- Warning amarelo/preto = linguagem exclusiva de obstáculo/perigo físico
- Vermelho = laser/alerta; ciano = estado normal/energia; branco frio = iluminação base
- Piso escuro reflexivo com strips laterais luminosas define as 3 faixas
