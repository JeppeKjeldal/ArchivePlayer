using ArchivePlayer.DTOs;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ArchivePlayer.Controllers.Api;

/// <summary>
/// API endpoints for search functionality
/// </summary>
[ApiController]
[Route("api/v1/search")]
[Authorize]
public class ApiSearchController : ControllerBase
{
    private readonly ArchiveSearchService _archiveSearchService;
    private readonly TMDBService _tmdbService;
    private readonly HttpClient _httpClient;

    public ApiSearchController(
        ArchiveSearchService archiveSearchService,
        TMDBService tmdbService,
        HttpClient httpClient)
    {
        _archiveSearchService = archiveSearchService;
        _tmdbService = tmdbService;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Search Archive.org for video content
    /// </summary>
    [HttpGet("archive")]
    public async Task<ActionResult<ArchiveSearchApiResponse>> SearchArchive([FromQuery] string query, [FromQuery] int page = 1)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        var serviceResponse = await _archiveSearchService.SearchAsync(query, page);

        var apiResponse = new ArchiveSearchApiResponse
        {
            Results = serviceResponse.Results.Select(r => new ArchiveSearchResultDto
            {
                Identifier = r.Identifier,
                Title = r.Title,
                MediaType = r.MediaType,
                ThumbnailUrl = r.ThumbnailUrl
            }).ToList(),
            HasMoreResults = serviceResponse.HasMoreResults,
            CurrentPage = page,
            TotalResults = 0
        };

        return Ok(apiResponse);
    }

    /// <summary>
    /// Get TMDB category suggestions for a specific Archive.org identifier
    /// </summary>
    [HttpGet("category-suggestions")]
    public async Task<ActionResult<List<CategorySuggestionDto>>> GetCategorySuggestions([FromQuery] string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return BadRequest("Identifier parameter is required");
        }

        try
        {
            var metadataUrl = $"https://archive.org/metadata/{identifier}";
            var response = await _httpClient.GetAsync(metadataUrl);

            if (!response.IsSuccessStatusCode)
            {
                return Ok(new List<CategorySuggestionDto>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var archiveTitle = doc.RootElement.GetProperty("metadata").GetProperty("title").GetString() ?? identifier;

            var tmdbMatch = await _tmdbService.FindMatchAsync(archiveTitle);

            if (tmdbMatch != null && tmdbMatch.TmdbId > 0 && !string.IsNullOrEmpty(tmdbMatch.MediaType))
            {
                var genres = await _tmdbService.FetchGenresAsync(tmdbMatch.TmdbId, tmdbMatch.MediaType);

                var suggestions = genres.Select(g => new CategorySuggestionDto
                {
                    Name = g,
                    Source = "TMDB"
                }).ToList();

                return Ok(suggestions);
            }
        }
        catch
        {
            // Fail silently and return empty suggestions
        }

        return Ok(new List<CategorySuggestionDto>());
    }
}