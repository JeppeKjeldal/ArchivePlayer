using ArchivePlayer.Data;
using ArchivePlayer.Models;
using ArchivePlayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArchivePlayer.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient _httpClient;
        private readonly TMDBService _tmdbService;

        public SearchController(
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var categories = await _context.UserMediaItems
                    .Where(u => u.UserId == user.Id)
                    .Select(u => u.CategoryName)
                    .Distinct()
                    .ToListAsync();

                ViewBag.ExistingCategories = categories;
            }
            else
            {
                ViewBag.ExistingCategories = new List<string>();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddSource(SearchInputModel searchInputModel)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.ExistingCategories = await _context.UserMediaItems
                        .Where(u => u.UserId == user.Id)
                        .Select(u => u.CategoryName)
                        .Distinct()
                        .ToListAsync();
                }
                return View("Index", searchInputModel);
            }

            string identifier = searchInputModel.InputString.Contains("archive.org/details/")
                ? searchInputModel.InputString.Split("archive.org/details/")[1].TrimEnd('/')
                : searchInputModel.InputString;

            Console.WriteLine($"[DEBUG] Parsed Identifier: {identifier}");

            await CreateNewMediaItem(identifier);

            var mediaItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == identifier);
            var currentUser = await _userManager.GetUserAsync(User);

            await CreateNewUserMediaItem(mediaItem, currentUser.Id, searchInputModel.CategoryName);

            return RedirectToAction("Index", "Home");
        }

        private async Task CreateNewMediaItem(string identifier)
        {
            Console.WriteLine($"[DEBUG] CreateNewMediaItem called for: {identifier}");

            var existingItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == identifier);

            if (existingItem == null)
            {
                Console.WriteLine($"[DEBUG] Item does not exist, fetching metadata...");

                string mediaType = "Unknown";
                string title = identifier;
                string? thumbnailUrl = $"https://archive.org/services/img/{identifier}";
                string? tmdbPosterUrl = null;
                int? tmdbId = null;
                string? tmdbMediaType = null;

                try
                {
                    var metadataUrl = $"https://archive.org/metadata/{identifier}";
                    var response = await _httpClient.GetAsync(metadataUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(json);
                        var metadata = doc.RootElement.GetProperty("metadata");

                        mediaType = metadata.GetProperty("mediatype").GetString() ?? "Unknown";
                        title = metadata.GetProperty("title").GetString() ?? identifier;

                        Console.WriteLine($"[DEBUG] Archive Title extracted: '{title}'");
                        Console.WriteLine($"[DEBUG] Fetching TMDB data for: '{title}'");

                        var tmdbResult = await _tmdbService.SearchByTitleAsync(title);

                        if (tmdbResult != null)
                        {
                            tmdbPosterUrl = tmdbResult.Value.posterUrl;
                            tmdbId = tmdbResult.Value.tmdbId;
                            tmdbMediaType = tmdbResult.Value.mediaType;

                            if (!string.IsNullOrEmpty(tmdbPosterUrl))
                            {
                                thumbnailUrl = tmdbPosterUrl;
                                Console.WriteLine($"[DEBUG] SUCCESS: TMDB poster found and applied!");
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] TMDB found the show, but no poster image was available.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] TMDB search returned null. Falling back to Archive.org thumbnail.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch metadata for {identifier}: {ex.Message}");
                }

                var newMediaItem = new MediaItem
                {
                    Identifier = identifier,
                    Title = title,
                    MediaType = mediaType,
                    ThumbnailUrl = thumbnailUrl,
                    TMDBPosterUrl = tmdbPosterUrl,
                    TMDBId = tmdbId,
                    TMDBMediaType = tmdbMediaType
                };

                _context.MediaItems.Add(newMediaItem);
                Console.WriteLine($"[DEBUG] Saved new MediaItem: {identifier}");
                await _context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"[DEBUG] Item already exists in database.");

                // AUTO-UPGRADE: If it exists but lacks TMDB data, fetch it now!
                if (string.IsNullOrEmpty(existingItem.TMDBPosterUrl) && existingItem.TMDBId == null)
                {
                    Console.WriteLine($"[DEBUG] Existing item lacks TMDB data. Fetching and upgrading now...");

                    var tmdbResult = await _tmdbService.SearchByTitleAsync(existingItem.Title);

                    if (tmdbResult != null)
                    {
                        existingItem.TMDBPosterUrl = tmdbResult.Value.posterUrl;
                        existingItem.TMDBId = tmdbResult.Value.tmdbId;
                        existingItem.TMDBMediaType = tmdbResult.Value.mediaType;

                        if (!string.IsNullOrEmpty(existingItem.TMDBPosterUrl))
                        {
                            existingItem.ThumbnailUrl = existingItem.TMDBPosterUrl;
                            Console.WriteLine($"[DEBUG] SUCCESS: Upgraded existing item with TMDB poster!");
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Item already has TMDB data, skipping fetch.");
                }
            }
        }

        private async Task CreateNewUserMediaItem(MediaItem? mediaItem, string userId, string categoryName)
        {
            var newUserMediaItem = new UserMediaItem
            {
                UserId = userId,
                MediaItemId = mediaItem!.Id,
                CategoryName = string.IsNullOrWhiteSpace(categoryName) ? "Uncategorized" : categoryName
            };

            _context.UserMediaItems.Add(newUserMediaItem);

            await _context.SaveChangesAsync();
        }
    }
}