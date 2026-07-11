# Setores D & E — Rework a partir de Referência (Round 20)

Reversão da abordagem de "imagem como background" e **remodelagem dos setores com
geometria real**, usando as imagens apenas como **referência visual (moodboard)**.
No Setor E, o trecho virou uma **ponte aberta, sem teto**.

Cena: `Beta03_Principal`. Data: 07/07/2026.

---

## 1. O que foi revertido da implementação anterior
A feature anterior (R19) aplicava `SetorD/E_Background.png` como **planos 3D** (backdrops
frontais e depois laterais) com material Unlit. Tudo isso foi **removido**:

- **Removidos da cena:** root `Gameplay_Backgrounds` com os 4 quads
  (`SectorD_Backdrop_Left/Right`, `SectorE_Backdrop_Left/Right`) — `DestroyImmediate`.
- **Materiais removidos:** `MAT_SetorD_Background_Unlit.mat`, `MAT_SetorE_Background_Unlit.mat`
  (pasta `Materials/Backgrounds/` apagada).
- **Script removido:** `SectorBackdropParallax.cs` (parallax da feature anterior).
- Nenhuma imagem continua atribuída a material renderizado na cena.

## 2. Objetos/backgrounds removidos ou desativados
| Item | Ação |
|---|---|
| `Gameplay_Backgrounds` (4 quads) | destruído |
| `MAT_Setor D/E _Background_Unlit.mat` | apagados |
| `SectorBackdropParallax.cs` | apagado |
| Teto do Setor E (112 objetos: `Center Ceiling`, `Ceiling Shoulder L/R`, `Main Duct L/R`, `Cyan Conduit L/R`, `Arch Top/Panel/Glow`, `Ceiling Light L/R + Frame`) | movidos para `Legacy_SectorE_Ceiling_Disabled` (inativo) |
| Paredes laterais do Setor E (32 objetos: `Lab Back Wall L/R`, `Lab Glass`) | movidos para o mesmo grupo legado (inativo) |

Nada de gameplay foi tocado (piso/pista/lanes/obstáculos/triggers/robôs/terminal
preservados). O jogador é travado nas lanes `x∈[-3,3]`, então abrir as laterais **não**
permite cair.

## 3. Onde as imagens de referência foram organizadas
Movidas (mantendo GUID) para uma pasta de **consulta visual**, fora de qualquer material:
- `Assets/_ProjectAurora/Art/References/Sectors/SetorD_Background.png`
- `Assets/_ProjectAurora/Art/References/Sectors/SetorE_Background.png`

Não estão atribuídas a nenhum material ativo — são apenas moodboard.

## 4. Setor D — Corredor Vermelho (remodelado com base na referência)
Referência: bulkhead vermelho, avisos de contenção, corrupção da CelestIA, corredor
agressivo. Traduzido em **geometria/luz** (root `SectorD_RedDressing`), mantendo o
corredor fechado e a legibilidade do runner:
- **4 luzes de alerta vermelhas** ao longo do corredor (point lights, sem sombra).
- **14 faixas emissivas vermelhas** correndo nas duas paredes (material
  `MAT_Alert_Red_Emissive`).
- **6 vigas hazard vermelhas** sob o teto (viga de alerta atravessando).
- **4 acentos ciano corrompidos** ("glitch") esparsos nas paredes
  (`MAT_Corrupt_Cyan_Emissive`).
- Resultado: clima mais agressivo, vermelho dominante, sinais de corrupção — sem poluir a
  pista (tudo fora de `x±4.9`, acima/nas laterais).

## 5. Setor E — Ponte Técnica (remodelado com base na referência)
Referência: ponte técnica exposta sobre um vazio, megaestruturas ao fundo, céu de aurora,
sensação de altura/travessia. Traduzido em **ponte aberta real** (root
`SectorE_OpenBridge`):
- **Teto removido** (roof desativado) → céu aberto acima.
- **Laterais abertas** (paredes de fundo desativadas), mantendo **corrimãos** (Glass
  Rails), **pilares/postes** (Arch Pillars) e **catwalks** — leitura de ponte exposta.
- **5 vigas superiores localizadas** (trusses) com faixa emissiva vermelha — suporte
  parcial, não um teto.
- **10 pilares de suporte descendo ao vazio** + **10 braços diagonais** sob o deck →
  sensação de ponte elevada sobre estrutura/abismo.
- **4 cabos soltos** pendendo das vigas.
- **7 torres + 3 passarelas externas** (silhuetas distantes) dos dois lados → "estrutura
  externa visível" do complexo, coerente com a referência.
- **5 luzes de emergência** (3 vermelhas + 2 frias) — o setor perdeu as luzes de teto.
- Materiais próprios de dressing em `Materials/SectorDressing/`
  (`MAT_Bridge_DarkMetal`, `MAT_Alert_Red_Emissive`, `MAT_Corrupt_Cyan_Emissive`).

## 6. Confirmação: Setor E é ponte aberta e SEM teto
Validado em Play Mode: acima do deck há **céu aberto** (fundo escuro real da câmera), com
apenas **vigas/passarelas localizadas** — nenhum teto sólido cobrindo o setor. As laterais
mostram estrutura externa (torres) e o vazio. A leitura principal é de **travessia exposta**
antes do Núcleo.

## 7. Testes realizados (Play Mode)
| # | Verificação | Resultado |
|---|---|---|
| 1 | Sem imagem colada como fundo (D e E) | ✅ backdrops/materiais removidos |
| 2 | Setor D remodelado, mais agressivo/vermelho | ✅ vigas/faixas/luzes vermelhas + glitch ciano |
| 3 | Setor E parece ponte real | ✅ deck + postes + corrimãos + pilares + torres |
| 4 | Setor E aberto, sem teto principal | ✅ céu aberto; só vigas localizadas |
| 5 | Frente da pista livre / travessia jogável | ✅ lanes livres, obstáculos na pista |
| 6 | Obstáculos e triggers funcionando | ✅ preservados (só ambientação alterada) |
| 7 | Robôs/perseguição/terminal intactos | ✅ não tocados |
| 8 | Performance | ✅ ~10 luzes sem sombra + geometria simples; leve |
| 9 | Console | ✅ apenas avisos cosméticos de *color primaries* (MP4) |

Screenshots de validação foram capturados e removidos após a conferência (não versionados).

## 8. Pendências
- O céu aberto do Setor E usa o fundo sólido escuro da câmera (sem skybox). Fica
  atmosférico; se quiserem um céu de aurora real depois, dá para adicionar um skybox
  procedural/HDRI **só** para esse trecho sem afetar os setores fechados.
- O `Setor D` continua com base clara (teal) sob o overlay vermelho; se quiserem um
  vermelho ainda mais dominante, dá para escurecer os materiais base do corredor D numa
  passada futura (evitei alterar materiais compartilhados agora para não afetar outros
  setores).
- Relatório anterior `SectorBackgrounds_D_E_Report.md` fica **obsoleto** (aquela abordagem
  foi revertida) — mantido apenas como histórico.

## Arquivos/roots tocados
- Cena `Beta03_Principal`: novos roots `SectorD_RedDressing`, `SectorE_OpenBridge`;
  grupo `Legacy_SectorE_Ceiling_Disabled` (inativo); remoção de `Gameplay_Backgrounds`.
- `Materials/SectorDressing/` (3 materiais novos).
- `Art/References/Sectors/` (imagens como referência).
- Removidos: `Materials/Backgrounds/*`, `Scripts/Environment/SectorBackdropParallax.cs`.
