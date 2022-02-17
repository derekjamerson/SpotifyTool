using Microsoft.AspNet.Identity;
using SpotifyTool.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SpotifyTool.MVC.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private static LibraryService _service;

        public LibraryController()
        {
            _service = new LibraryService();
        }

        public ActionResult Index()
        {
            ViewBag.LastFetch = _service.GetLastFetch(User.Identity.GetUserId());
            return View();
        }

        public ActionResult ViewStats()
        {
            var stats = _service.GetStats(User.Identity.GetUserId());
            return View(stats);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> FetchLibrary()
        {
            await _service.FetchLibrary(User.Identity.GetUserId());

            return RedirectToAction("Index", "Library");
        }

        public ActionResult TracksByArtist(string id)
        {
            var model = _service.GetTracksByArtistInLibrary(id, User.Identity.GetUserId());
            ViewBag.Artist = _service.GetArtistNameById(id);
            return View(model);
        }
    }
}