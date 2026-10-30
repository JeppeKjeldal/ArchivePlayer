using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchivePlayer.Services
{
    /// <summary>
    /// Handles live search queries against the Archive.org Advanced Search API.
    /// </summary>
    public class ArchiveSearchService
    {
        private readonly HttpClient _httpClient;

        public ArchiveSearchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ArchiveSearchResult>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ArchiveSearchResult>();

            var encodedQuery = Uri.EscapeDataString(query);

            var url = $"https://archive.org/advancedsearch.php?q={encodedQuery}&fl[]=identifier,title,mediatype,image&rows=12&output=json";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<ArchiveSearchResult>();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                var results = new List<ArchiveSearchResult>();

                if (doc.RootElement.TryGetProperty("response", out var responseElement) &&
                    responseElement.TryGetProperty("docs", out var docsElement) &&
                    docsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var docElement in docsElement.EnumerateArray())
                    {
                        var identifier = docElement.TryGetProperty("identifier", out var idProp) ? idProp.GetString() : null;
                        if (string.IsNullOrEmpty(identifier)) continue;

                        results.Add(new ArchiveSearchResult
                        {
                            Identifier = identifier,
                            Title = docElement.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : identifier,
                            MediaType = docElement.TryGetProperty("mediatype", out var mediaProp) ? mediaProp.GetString() : "Unknown",
                            ThumbnailUrl = $"https://archive.org/services/img/{identifier}"
                        });
                    }
                }

                return results;
            }
            catch
            {
                return new List<ArchiveSearchResult>();
            }
        }
    }

    public class ArchiveSearchResult
    {
        public string Identifier { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }
}