using System.Text.Json;

namespace ArchivePlayer.Services
{
    public class ArchiveSearchService
    {
        private readonly HttpClient _httpClient;
        private const int ResultsPerPage = 12;

        public ArchiveSearchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ArchiveSearchResponse> SearchAsync(string query, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ArchiveSearchResponse();

            var encodedQuery = Uri.EscapeDataString(query);

            var searchQuery = $"mediatype:(movies OR tv OR animation) AND ({encodedQuery})";
            var encodedSearchQuery = Uri.EscapeDataString(searchQuery);

            var url = $"https://archive.org/advancedsearch.php?q={encodedSearchQuery}&fl[]=identifier,title,mediatype,image&rows={ResultsPerPage}&page={page}&output=json";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new ArchiveSearchResponse();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                var results = new List<ArchiveSearchResult>();
                int numFound = 0;

                if (doc.RootElement.TryGetProperty("response", out var responseElement))
                {
                    numFound = responseElement.TryGetProperty("numFound", out var numFoundProp) ? numFoundProp.GetInt32() : 0;

                    if (responseElement.TryGetProperty("docs", out var docsElement) && docsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var docElement in docsElement.EnumerateArray())
                        {
                            var identifier = docElement.TryGetProperty("identifier", out var idProp) ? idProp.GetString() : null;
                            if (string.IsNullOrEmpty(identifier)) continue;

                            var title = docElement.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
                            var mediaType = docElement.TryGetProperty("mediatype", out var mediaProp) ? mediaProp.GetString() : null;

                            results.Add(new ArchiveSearchResult
                            {
                                Identifier = identifier,
                                Title = string.IsNullOrWhiteSpace(title) ? identifier : title,
                                MediaType = string.IsNullOrWhiteSpace(mediaType) ? "Unknown" : mediaType,
                                ThumbnailUrl = $"https://archive.org/services/img/{identifier}"
                            });
                        }
                    }
                }

                var hasMoreResults = (page * ResultsPerPage) < numFound;

                return new ArchiveSearchResponse
                {
                    Results = results,
                    HasMoreResults = hasMoreResults
                };
            }
            catch
            {
                return new ArchiveSearchResponse();
            }
        }
    }

    public class ArchiveSearchResponse
    {
        public List<ArchiveSearchResult> Results { get; set; } = new();
        public bool HasMoreResults { get; set; }
    }

    public class ArchiveSearchResult
    {
        public string Identifier { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }
}