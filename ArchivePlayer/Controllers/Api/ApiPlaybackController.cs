using ArchivePlayer.Data;
using ArchivePlayer.DTOs;
using ArchivePlayer.Models;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArchivePlayer.Controllers.Api;

/// <summary>
/// API endpoints for media playback and progress tracking
/// </summary>
[ApiController]
[Route("api/v1/playback")]
[Authorize]
public class ApiPlaybackController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HttpClient _httpClient;
    private readonly TMDBService _tmdbService;

    public ApiPlaybackController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        HttpClient httpClient,
        TMDBService tmdbService)
    {
        _context = context;
        _userManager = userManager;
        _httpClient = httpClient;
        _tmdbService = tmdbService;
    }

    /// <summary>
    /// Get all data required to render the playback screen for a specific media item
    /// </summary>
    [HttpGet("{identifier}")]
    public async Task<ActionResult<MediaDetailsDto>> GetPlaybackDetails(string identifier)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var mediaItem = await _context.MediaItems
            .FirstOrDefaultAsync(m => m.Identifier == identifier);

        if (mediaItem == null) return NotFound("Media item not found");

        var userMediaItem = await _context.UserMediaItems
            .FirstOrDefaultAsync(umi => umi.UserId == user.Id && umi.MediaItemId == mediaItem.Id);

        var videoFiles = await GetVideoFilesAsync(identifier);

        List<EpisodeInfoDto>? episodes = null;

        // Try TMDB first
        if (mediaItem.TMDBId.HasValue && mediaItem.TMDBMediaType == "tv")
        {
            var tmdbEpisodes = await _tmdbService.FetchEpisodesAsync(mediaItem.TMDBId.Value);
            if (tmdbEpisodes != null && tmdbEpisodes.Count > 0)
            {
                episodes = tmdbEpisodes.Select(ep => new EpisodeInfoDto
                {
                    SeasonNumber = ep.SeasonNumber,
                    EpisodeNumber = ep.EpisodeNumber,
                    Name = ep.Name,
                    Overview = ep.Overview,
                    StillUrl = ep.StillUrl,
                    FileName = MatchEpisodeToFile(ep, videoFiles)
                }).Where(ep => !string.IsNullOrEmpty(ep.FileName)).ToList();
            }
        }

        // Fallback: parse from video filenames
        if (episodes == null || episodes.Count == 0)
        {
            episodes = ParseEpisodesFromFiles(videoFiles);
        }

        var response = new MediaDetailsDto
        {
            Identifier = mediaItem.Identifier,
            Title = mediaItem.Title,
            Description = mediaItem.Description,
            MediaType = mediaItem.MediaType,
            ThumbnailUrl = mediaItem.ThumbnailUrl,
            TmdbPosterUrl = mediaItem.TMDBPosterUrl,
            TmdbId = mediaItem.TMDBId,
            TmdbMediaType = mediaItem.TMDBMediaType,
            VideoFiles = videoFiles,
            Episodes = episodes,
            WatchProgressSeconds = userMediaItem?.WatchProgressSeconds ?? 0,
            CurrentEpisodeFileName = userMediaItem?.CurrentEpisodeFileName,
            LastWatched = userMediaItem?.LastWatched
        };

        return Ok(response);
    }

    /// <summary>
    /// Update the user's watch progress for a specific media item
    /// </summary>
    [HttpPost("progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] WatchProgressUpdateRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var mediaItem = await _context.MediaItems
            .FirstOrDefaultAsync(m => m.Identifier == request.Identifier);

        if (mediaItem == null) return NotFound("Media item not found");

        var userMediaItem = await _context.UserMediaItems
            .FirstOrDefaultAsync(umi => umi.UserId == user.Id && umi.MediaItemId == mediaItem.Id);

        if (userMediaItem == null)
        {
            // Create progress record if it doesn't exist yet
            userMediaItem = new UserMediaItem
            {
                UserId = user.Id,
                MediaItemId = mediaItem.Id,
                CategoryName = "Uncategorized"
            };
            _context.UserMediaItems.Add(userMediaItem);
        }

        // Update progress
        userMediaItem.WatchProgressSeconds = request.ProgressSeconds;
        userMediaItem.CurrentEpisodeFileName = request.EpisodeFileName;
        userMediaItem.LastWatched = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }


    private async Task<List<string>> GetVideoFilesAsync(string identifier)
    {
        try
        {
            var metadataUrl = $"https://archive.org/metadata/{identifier}";
            var response = await _httpClient.GetAsync(metadataUrl);
            if (!response.IsSuccessStatusCode) return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var files = new List<string>();

            if (doc.RootElement.TryGetProperty("files", out var filesElement) && filesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var file in filesElement.EnumerateArray())
                {
                    if (file.TryGetProperty("name", out var nameProp))
                    {
                        var fileName = nameProp.GetString();
                        if (!string.IsNullOrEmpty(fileName) && IsVideoFile(fileName))
                        {
                            files.Add(fileName);
                        }
                    }
                }
            }
            return files.OrderBy(f => f).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private bool IsVideoFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext == ".mp4" || ext == ".mkv" || ext == ".avi" || ext == ".webm" || ext == ".m4v";
    }

    private string MatchEpisodeToFile(TmdbEpisodeInfo ep, List<string> videoFiles)
    {
        var pattern1 = $"s{ep.SeasonNumber:D2}e{ep.EpisodeNumber:D2}";
        var pattern2 = $"{ep.SeasonNumber}x{ep.EpisodeNumber:D2}";

        var match = videoFiles.FirstOrDefault(f =>
            f.Contains(pattern1, StringComparison.OrdinalIgnoreCase) ||
            f.Contains(pattern2, StringComparison.OrdinalIgnoreCase));

        return match ?? string.Empty;
    }

    private List<EpisodeInfoDto> ParseEpisodesFromFiles(List<string> videoFiles)
    {
        var episodes = new List<EpisodeInfoDto>();
        var episodePattern = new System.Text.RegularExpressions.Regex(
            @"(?:S(\d+)[\s._-]*E(\d+)|(\d+)x(\d+)|[Ee]pisode[\s._-]*(\d+)|^(\d+)[\s._-])",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        foreach (var file in videoFiles.OrderBy(f => f))
        {
            var match = episodePattern.Match(file);
            var episode = new EpisodeInfoDto
            {
                FileName = file,
                Name = Path.GetFileNameWithoutExtension(file)
            };

            if (match.Success)
            {
                if (match.Groups[1].Success && match.Groups[2].Success)
                {
                    episode.SeasonNumber = int.Parse(match.Groups[1].Value);
                    episode.EpisodeNumber = int.Parse(match.Groups[2].Value);
                }
                else if (match.Groups[3].Success && match.Groups[4].Success)
                {
                    episode.SeasonNumber = int.Parse(match.Groups[3].Value);
                    episode.EpisodeNumber = int.Parse(match.Groups[4].Value);
                }
                else if (match.Groups[5].Success)
                {
                    episode.SeasonNumber = 1;
                    episode.EpisodeNumber = int.Parse(match.Groups[5].Value);
                }
                else if (match.Groups[6].Success)
                {
                    episode.SeasonNumber = 1;
                    episode.EpisodeNumber = int.Parse(match.Groups[6].Value);
                }
            }

            episodes.Add(episode);
        }

        return episodes;
    }
}