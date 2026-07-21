using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectAurora.Lore
{
    /// Converte o markdown dos arquivos de lore em rich text do TextMeshPro (Round 19).
    ///
    /// Antes esta classe APAGAVA a formatacao (removia **, __, ` e os # dos titulos),
    /// entregando um bloco de texto cru e sem hierarquia. Agora ela traduz:
    ///
    ///   ## Secao            -> cabecalho ciano em caixa alta, com respiro em volta
    ///   **Rotulo:** valor   -> rotulo ciano em negrito + valor claro (bloco de metadados)
    ///   **destaque**        -> negrito ciano no meio do corpo do texto
    ///   ---                 -> divisoria sutil de largura total
    ///   "citacao"           -> linha centralizada em italico (as frases de efeito
    ///                          dos documentos ficam com peso de epigrafe)
    ///
    /// As cores acompanham a identidade do projeto (ciano Aurora) e sao aplicadas com
    /// tags &lt;color&gt; do TMP — nada de sprite/atlas, entao nao custa nada em runtime.
    public static class AuroraLoreTextFormatter
    {
        // paleta — ciano Aurora para chaves/destaques, cinza claro para o corpo
        private const string AccentHex = "#4FD8FF";   // ciano de destaque
        private const string HeadingHex = "#7FE7FF";  // ciano mais claro p/ cabecalhos
        private const string LabelHex = "#4FD8FF";    // rotulos do bloco de metadados
        private const string ValueHex = "#D7E6F2";    // valor dos metadados
        private const string QuoteHex = "#9FD9F0";    // citacoes/epigrafes

        public static string FormatForDisplay(string source, bool omitFirstHeading = true)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            string normalized = source.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('﻿');
            string[] lines = normalized.Split('\n');
            var output = new List<string>(lines.Length);
            bool firstContentLine = true;

            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i].TrimEnd();
                string trimmed = raw.TrimStart();

                if (trimmed.Length == 0)
                {
                    output.Add(string.Empty);
                    continue;
                }

                bool heading = trimmed.StartsWith("#", StringComparison.Ordinal);

                // o titulo do topo (# LORE_00X — ...) ja aparece no cabecalho da UI
                if (firstContentLine)
                {
                    firstContentLine = false;
                    if (omitFirstHeading && heading)
                    {
                        continue;
                    }
                }

                // divisoria
                if (trimmed == "---" || trimmed == "***")
                {
                    if (output.Count > 0 && output[output.Count - 1].Length > 0)
                    {
                        output.Add(string.Empty);
                    }
                    output.Add(Divider());
                    output.Add(string.Empty);
                    continue;
                }

                if (heading)
                {
                    string title = trimmed.TrimStart('#').TrimStart();
                    if (output.Count > 0 && output[output.Count - 1].Length > 0)
                    {
                        output.Add(string.Empty);
                    }
                    output.Add(Heading(title));
                    continue;
                }

                // bloco de metadados: **Rotulo:** valor
                string label, value;
                if (TryParseMetadata(trimmed, out label, out value))
                {
                    output.Add(Metadata(label, value));
                    continue;
                }

                // corpo: converte **destaque** e trata citacoes
                output.Add(Body(trimmed));
            }

            return Collapse(output);
        }

        /// Detecta "**Rotulo:** valor" (o cabecalho de ficha dos documentos).
        private static bool TryParseMetadata(string line, out string label, out string value)
        {
            label = null;
            value = null;

            if (!line.StartsWith("**", StringComparison.Ordinal))
            {
                return false;
            }

            int close = line.IndexOf("**", 2, StringComparison.Ordinal);
            if (close < 0)
            {
                return false;
            }

            string inner = line.Substring(2, close - 2);
            if (!inner.EndsWith(":", StringComparison.Ordinal))
            {
                return false;
            }

            label = inner.Substring(0, inner.Length - 1).Trim();
            value = line.Substring(close + 2).Trim();
            return label.Length > 0;
        }

        private static string Divider()
        {
            // linha fina de largura total, discreta
            return $"<color={AccentHex}><alpha=#33>────────────────────────────────────────<alpha=#FF></color>";
        }

        private static string Heading(string title)
        {
            return $"<size=115%><b><color={HeadingHex}>{title.ToUpperInvariant()}</color></b></size>";
        }

        private static string Metadata(string label, string value)
        {
            // rotulo ciano em negrito + valor claro, alinhados como ficha tecnica
            return $"<b><color={LabelHex}>{label}</color></b>  <color={ValueHex}>{ConvertEmphasis(value)}</color>";
        }

        private static string Body(string line)
        {
            string converted = ConvertEmphasis(line);

            // frases de efeito entre aspas viram epigrafe centralizada em italico
            string bare = line.Replace("**", string.Empty).Trim();
            bool isQuote = bare.Length > 1 &&
                (bare[0] == '“' || bare[0] == '"') &&
                (bare[bare.Length - 1] == '”' || bare[bare.Length - 1] == '"');

            if (isQuote)
            {
                string quoteText = ConvertEmphasis(bare);
                return $"<align=center><i><color={QuoteHex}>{quoteText}</color></i></align>";
            }

            return converted;
        }

        /// **texto** -> negrito ciano.  __texto__ -> negrito simples.  `codigo` -> monoespacado.
        private static string ConvertEmphasis(string line)
        {
            var sb = new StringBuilder(line.Length + 32);
            bool boldOpen = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '*' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    sb.Append(boldOpen ? "</color></b>" : $"<b><color={AccentHex}>");
                    boldOpen = !boldOpen;
                    i++;
                    continue;
                }

                if (line[i] == '`')
                {
                    sb.Append(' ');
                    continue;
                }

                sb.Append(line[i]);
            }

            if (boldOpen)
            {
                sb.Append("</color></b>"); // fecha marcacao desbalanceada
            }

            return sb.ToString().Replace("__", string.Empty);
        }

        /// Junta as linhas evitando linhas em branco duplicadas.
        private static string Collapse(List<string> output)
        {
            var builder = new StringBuilder();
            bool previousBlank = true; // evita comecar com espaco em branco

            for (int i = 0; i < output.Count; i++)
            {
                bool blank = string.IsNullOrWhiteSpace(output[i]);
                if (blank && previousBlank)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }
                builder.Append(output[i]);
                previousBlank = blank;
            }

            return builder.ToString().Trim();
        }
    }
}
