using System;
using System.Collections.Generic;
using System.Text;

namespace RaidSchedule.Util
{
    public static class ChatHelper
    {
        // Standard Minecraft-style color codes mapped to hex
        private static readonly Dictionary<char, string> Colors = new()
        {
            ['0'] = "000000", ['1'] = "0000aa", ['2'] = "00aa00", ['3'] = "00aaaa",
            ['4'] = "aa0000", ['5'] = "aa00aa", ['6'] = "ffaa00", ['7'] = "aaaaaa",
            ['8'] = "555555", ['9'] = "5555ff", ['a'] = "55ff55", ['b'] = "55ffff",
            ['c'] = "ff5555", ['d'] = "ff55ff", ['e'] = "ffff55", ['f'] = "ffffff",
        };

        /// <summary>
        /// Translate &amp;c-style color codes into Unity rich text.
        /// &amp;l = bold, &amp;o = italic, &amp;r = reset (closes all open tags).
        /// </summary>
        public static string Format(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new StringBuilder(input.Length);
            var openTags = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '&' && i + 1 < input.Length)
                {
                    var code = char.ToLowerInvariant(input[i + 1]);
                    if (Colors.TryGetValue(code, out var hex))
                    {
                        sb.Append($"<color=#{hex}>");
                        openTags++;
                        i++;
                        continue;
                    }
                    if (code == 'l') { sb.Append("<b>"); openTags++; i++; continue; }
                    if (code == 'o') { sb.Append("<i>"); openTags++; i++; continue; }
                    if (code == 'r')
                    {
                        // Close all open tags. We don't track which kind; rely on Unity being lenient.
                        for (int k = 0; k < openTags; k++) sb.Append("</color></b></i>");
                        openTags = 0;
                        i++;
                        continue;
                    }
                }
                sb.Append(input[i]);
            }

            // Close anything still open at end of string
            for (int k = 0; k < openTags; k++) sb.Append("</color></b></i>");
            return sb.ToString();
        }

        /// <summary>
        /// "2d 4h 32m" style. Drops zero-value leading units.
        /// Always shows minutes for windows under an hour, seconds only if under a minute.
        /// </summary>
        public static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalSeconds < 0) ts = TimeSpan.Zero;

            var parts = new List<string>();
            if (ts.Days > 0) parts.Add($"{ts.Days}d");
            if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
            if (ts.Days == 0 && ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
            if (ts.TotalMinutes < 1) parts.Add($"{ts.Seconds}s");

            return parts.Count == 0 ? "0s" : string.Join(" ", parts);
        }
    }
}