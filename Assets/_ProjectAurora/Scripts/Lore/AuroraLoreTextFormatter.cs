using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectAurora.Lore
{
    public static class AuroraLoreTextFormatter
    {
        public static string FormatForDisplay(string source, bool omitFirstHeading = true)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            string normalized = source.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('\uFEFF');
            string[] lines = normalized.Split('\n');
            var output = new List<string>(lines.Length);
            bool firstContentLine = true;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                string trimmed = line.TrimStart();
                bool heading = trimmed.StartsWith("#", StringComparison.Ordinal);

                if (firstContentLine && trimmed.Length > 0)
                {
                    firstContentLine = false;
                    if (omitFirstHeading && heading)
                    {
                        continue;
                    }
                }

                if (trimmed == "---" || trimmed == "***")
                {
                    if (output.Count > 0 && output[output.Count - 1].Length > 0)
                    {
                        output.Add(string.Empty);
                    }
                    continue;
                }

                if (heading)
                {
                    trimmed = trimmed.TrimStart('#').TrimStart();
                    line = trimmed;
                }

                line = line.Replace("**", string.Empty)
                    .Replace("__", string.Empty)
                    .Replace("`", string.Empty);
                output.Add(line);
            }

            var builder = new StringBuilder(normalized.Length);
            bool previousBlank = false;
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
