using ArchivePlayer.Data;
using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ArchivePlayer.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var library = await _context.UserMediaItems
                .Include(umi => umi.MediaItem)
                .Where(umi => umi.UserId == user.Id)
                .OrderBy(umi => umi.CategoryName ?? "Uncategorized")
                .ThenBy(umi => umi.MediaItem.Title)
                .ToListAsync();

            var recentlyWatched = await _context.UserMediaItems
                .Include(umi => umi.MediaItem)
                .Where(umi => umi.UserId == user.Id && umi.LastWatched != null)
                .OrderByDescending(umi => umi.LastWatched)
                .Take(20)
                .ToListAsync();

            ViewBag.RecentlyWatched = recentlyWatched;

            var groupedLibrary = library.GroupBy(umi => umi.CategoryName ?? "Uncategorized");

            return View(groupedLibrary);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userMediaItem = await _context.UserMediaItems
                .FirstOrDefaultAsync(umi => umi.Id == id && umi.UserId == user.Id);

            if (userMediaItem != null)
            {
                _context.UserMediaItems.Remove(userMediaItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ClearRecentlyWatched(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userMediaItem = await _context.UserMediaItems
                .FirstOrDefaultAsync(umi => umi.Id == id && umi.UserId == user.Id);

            if (userMediaItem != null)
            {
                userMediaItem.LastWatched = null;
                userMediaItem.WatchProgressSeconds = 0;
                userMediaItem.CurrentEpisodeFileName = null;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}