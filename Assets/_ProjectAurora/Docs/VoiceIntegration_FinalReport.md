# Voice Integration — Final Report

- Banco: `Assets/_ProjectAurora/Audio/Voice/Database/VoiceLineDatabase.asset`
- Entradas: **67**
- Áudios integrados: **67**
- Áudios ausentes obrigatórios: nenhum
- Áudios ausentes opcionais: nenhum
- Duração: `AudioClip.length + postDelay`, respeitando `minDisplayTime`.
- Fallback sem áudio: duração por caracteres, sem interromper o fluxo.
- Reprodução: `AudioSource` 2D dedicado, fila por prioridade e cooldown por ID/prioridade.
- HUD: speaker, legenda limpa, estado da CelestIA e humor do Dr. Elias integrados ao retrato existente.
- `CEL_054` e `CEL_055`: opcionais.
