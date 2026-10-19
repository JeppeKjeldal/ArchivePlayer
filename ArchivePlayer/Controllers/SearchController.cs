using ArchivePlayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchivePlayer.Controllers
{
    //[Authorize]
    public class SearchController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddSource(SearchInputModel searchInputModel)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", searchInputModel);
            }

            // TODO: Parse identifier and save to DB

            return RedirectToAction("Index", "Home");
        }
    }
}
