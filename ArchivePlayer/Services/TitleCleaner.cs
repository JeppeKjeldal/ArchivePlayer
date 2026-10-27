using System.Text.RegularExpressions;

namespace ArchivePlayer.Services
{
    /// <summary>
    /// Transforms messy Archive.org identifiers and titles into clean, 
    /// TMDB-friendly search queries.
    /// </summary>
    public static class TitleCleaner
    {
        /// <summary>
        /// Strips uploader suffixes, episode markers, and formatting artifacts 
        /// from an Archive.org title to produce a clean search query.
        /// 
        /// Examples:
        ///   "bakugan-battle-brawlers-dktale_202604" → "bakugan battle brawlers"
        ///   "ultimate-spider-man-dktale"            → "ultimate spider man"
        ///   "Chowder (2007)"                        → "Chowder"
        ///   "Show-Name-S01E05.mkv"                  → "Show Name"
        /// </summary>
        public static string CleanForSearch(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return string.Empty;

            var cleaned = title;

            cleaned = RemoveUploaderSuffixes(cleaned);
            cleaned = RemoveTrailingNumericTags(cleaned);
            cleaned = RemoveParenthesizedContent(cleaned);
            cleaned = RemoveEpisodeMarkers(cleaned);
            cleaned = NormalizeSeparators(cleaned);
            cleaned = CollapseWhitespace(cleaned);

            return cleaned.Trim();
        }

        private static string RemoveUploaderSuffixes(string title)
        {
            return Regex.Replace(title, @"[-_]?dktale.*", "", RegexOptions.IgnoreCase).Trim();
        }

        private static string RemoveTrailingNumericTags(string title)
        {
            return Regex.Replace(title, @"[_]\d+$", "", RegexOptions.IgnoreCase).Trim();
        }

        private static string RemoveParenthesizedContent(string title)
        {
            return Regex.Replace(title, @"\s*\([^)]*\)", "").Trim();
        }

        private static string RemoveEpisodeMarkers(string title)
        {
            var withoutStandardMarker = Regex.Replace(title, @"\s*-\s*S\d+E\d+.*", "", RegexOptions.IgnoreCase).Trim();
            return Regex.Replace(withoutStandardMarker, @"\s*\d+x\d+.*", "", RegexOptions.IgnoreCase).Trim();
        }

        private static string NormalizeSeparators(string title)
        {
            return title.Replace("-", " ").Replace("_", " ").Trim();
        }

        private static string CollapseWhitespace(string title)
        {
            return Regex.Replace(title, @"\s+", " ");
        }
    }
}