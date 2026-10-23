using ArchivePlayer.Models;
using ArchivePlayer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ArchivePlayer.Controllers
{
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
                return RedirectToAction("Login", "Account"); // Or wherever your login is
            }

            // Fetch library, ordered by Category then by Title
            var library = await _context.UserMediaItems
                .Include(umi => umi.MediaItem)
                .Where(umi => umi.UserId == user.Id)
                .OrderBy(umi => umi.CategoryName ?? "Uncategorized")
                .ThenBy(umi => umi.MediaItem.Title)
                .ToListAsync();

            // Group the items by CategoryName
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
