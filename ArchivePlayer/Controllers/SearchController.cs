using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArchivePlayer.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchivePlayer.Controllers
{
    //[Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddSource(SearchInputModel searchInputModel)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", searchInputModel);
            }

            string identifier;

            if (searchInputModel.InputString.Contains("archive.org/details/"))
            {
                identifier = searchInputModel.InputString.Split("archive.org/details/")[1].TrimEnd('/');
            }
            else
            {
                identifier = searchInputModel.InputString;
            }

            Console.WriteLine($"Parsed Identifier: {identifier}");

            await CreateNewMediaItem(identifier);

            var mediaItem = await _context.MediaItems.FirstOrDefaultAsync(m => m.Identifier == identifier);

            //await CreateNewUserMediaItem(mediaItem);

            return RedirectToAction("Index", "Home");
        }

        private async Task CreateNewMediaItem(string identifier)
        {
            bool exists = await _context.MediaItems.AnyAsync(m => m.Identifier == identifier);

            if (!exists)
            {
                var newMediaItem = new MediaItem
                {
                    Identifier = identifier,
                    Title = identifier,
                    MediaType = "Movie"
                };

                _context.MediaItems.Add(newMediaItem);

                await _context.SaveChangesAsync();
            }
        }

        private async Task CreateNewUserMediaItem(MediaItem? mediaItem)
        {
            var newUserMediaItem = new UserMediaItem
            {
                UserId = "demo-user",
                MediaItemId = mediaItem!.Id,
                CategoryName = "Uncategorized"
            };

            _context.UserMediaItems.Add(newUserMediaItem);

            await _context.SaveChangesAsync();
        }
    }
}
