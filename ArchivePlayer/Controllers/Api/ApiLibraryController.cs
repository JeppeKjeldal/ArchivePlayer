using ArchivePlayer.Data;
using ArchivePlayer.DTOs;
using ArchivePlayer.Models;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePlayer.Controllers.Api;

/// <summary>
/// API endpoints for library management
/// </summary>
[ApiController]
[Route("api/v1/library")]
[Authorize]
public class ApiLibraryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HttpClient _httpClient;
    private readonly TMDBService _tmdbService;

    public ApiLibraryController(
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
    /// Get all library items grouped by category
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<LibraryResponse>> GetLibrary()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var userMediaItems = await _context.UserMediaItems
            .Where(umi => umi.UserId == user.Id)
            .Include(umi => umi.MediaItem)
            .ToListAsync();

        var categories = userMediaItems
            .GroupBy(umi => umi.CategoryName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(umi => MapToLibraryItemDto(umi)).ToList()
            );

        var response = new LibraryResponse
        {
            Categories = categories,
            CategoryNames = categories.Keys.ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Get recently watched items
    /// </summary>
    [HttpGet("recently-watched")]
    public async Task<ActionResult<List<RecentlyWatchedItemDto>>> GetRecentlyWatched([FromQuery] int limit = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var recentlyWatched = await _context.UserMediaItems
            .Where(umi => umi.UserId == user.Id && umi.LastWatched != null)
            .OrderByDescending(umi => umi.LastWatched)
            .Take(limit)
            .Include(umi => umi.MediaItem)
            .ToListAsync();

        var items = recentlyWatched.Select(umi => new RecentlyWatchedItemDto
        {
            Id = umi.Id,
            Identifier = umi.MediaItem.Identifier,
            Title = umi.MediaItem.Title,
            MediaType = umi.MediaItem.MediaType,
            ThumbnailUrl = umi.MediaItem.ThumbnailUrl,
            TmdbPosterUrl = umi.MediaItem.TMDBPosterUrl,
            CategoryName = umi.CategoryName,
            AddedAt = DateTime.UtcNow, // TODO: track actual added date
            WatchProgressSeconds = umi.WatchProgressSeconds,
            CurrentEpisodeFileName = umi.CurrentEpisodeFileName,
            LastWatched = umi.LastWatched,
            IsCompleted = umi.IsCompleted
        }).ToList();

        return Ok(items);
    }

    /// <summary>
    /// Add a new item to the library
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LibraryItemDto>> AddToLibrary([FromBody] AddToLibraryRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Check if item already exists in MediaItems
        var mediaItem = await _context.MediaItems
            .FirstOrDefaultAsync(m => m.Identifier == request.Identifier);

        if (mediaItem == null)
        {
            // Fetch metadata from Archive.org and TMDB
            mediaItem = await CreateMediaItemFromIdentifier(request.Identifier);
            if (mediaItem == null) return BadRequest("Could not fetch metadata for this identifier");
        }

        // Check if user already has this item
        var existingUserMediaItem = await _context.UserMediaItems
            .FirstOrDefaultAsync(umi => umi.UserId == user.Id && umi.MediaItemId == mediaItem.Id);

        if (existingUserMediaItem != null)
        {
            return Conflict("Item already exists in your library");
        }

        // Create new UserMediaItem
        var userMediaItem = new UserMediaItem
        {
            UserId = user.Id,
            MediaItemId = mediaItem.Id,
            CategoryName = string.IsNullOrWhiteSpace(request.CategoryName) ? "Uncategorized" : request.CategoryName
        };

        _context.UserMediaItems.Add(userMediaItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLibrary), MapToLibraryItemDto(userMediaItem));
    }

    /// <summary>
    /// Remove an item from the library
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromLibrary(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var userMediaItem = await _context.UserMediaItems
            .FirstOrDefaultAsync(umi => umi.Id == id && umi.UserId == user.Id);

        if (userMediaItem == null) return NotFound();

        _context.UserMediaItems.Remove(userMediaItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Privacy wipe: clear watch progress but keep item in library
    /// </summary>
    [HttpDelete("{id}/recent-history")]
    public async Task<IActionResult> ClearRecentHistory(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var userMediaItem = await _context.UserMediaItems
            .FirstOrDefaultAsync(umi => umi.Id == id && umi.UserId == user.Id);

        if (userMediaItem == null) return NotFound();

        userMediaItem.LastWatched = null;
        userMediaItem.WatchProgressSeconds = 0;
        userMediaItem.CurrentEpisodeFileName = null;

        await _context.SaveChangesAsync();

        return NoContent();
    }


    private LibraryItemDto MapToLibraryItemDto(UserMediaItem umi)
    {
        return new LibraryItemDto
        {
            Id = umi.Id,
            Identifier = umi.MediaItem.Identifier,
            Title = umi.MediaItem.Title,
            MediaType = umi.MediaItem.MediaType,
            ThumbnailUrl = umi.MediaItem.ThumbnailUrl,
            TmdbPosterUrl = umi.MediaItem.TMDBPosterUrl,
            CategoryName = umi.CategoryName,
            AddedAt = DateTime.UtcNow // TODO: track actual added date
        };
    }

    private async Task<MediaItem?> CreateMediaItemFromIdentifier(string identifier)
    {
        try
        {
            var metadataUrl = $"https://archive.org/metadata/{identifier}";
            var response = await _httpClient.GetAsync(metadataUrl);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var metadata = doc.RootElement.GetProperty("metadata");

            var mediaType = metadata.GetProperty("mediatype").GetString() ?? "Unknown";
            var archiveTitle = metadata.GetProperty("title").GetString() ?? identifier;
            var title = archiveTitle;
            var thumbnailUrl = $"https://archive.org/services/img/{identifier}";
            string? tmdbPosterUrl = null;
            int? tmdbId = null;
            string? tmdbMediaType = null;

            var tmdbResult = await _tmdbService.FindMatchAsync(archiveTitle);
            if (tmdbResult != null)
            {
                tmdbPosterUrl = tmdbResult.PosterUrl;
                title = tmdbResult.Title;
                tmdbId = tmdbResult.TmdbId;
                tmdbMediaType = tmdbResult.MediaType;

                if (!string.IsNullOrEmpty(tmdbPosterUrl))
                {
                    thumbnailUrl = tmdbPosterUrl;
                }
            }

            var mediaItem = new MediaItem
            {
                Identifier = identifier,
                Title = title,
                MediaType = mediaType,
                ThumbnailUrl = thumbnailUrl,
                TMDBPosterUrl = tmdbPosterUrl,
                TMDBId = tmdbId,
                TMDBMediaType = tmdbMediaType
            };

            _context.MediaItems.Add(mediaItem);
            await _context.SaveChangesAsync();

            return mediaItem;
        }
        catch
        {
            return null;
        }
    }
}