using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PTZ.PhotoOrder.Models;
using PTZ.PhotoOrder.Services;

namespace PTZ.PhotoOrder.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOptions<PhotoOrderConfig> config;
        private readonly PhotoAlbumService photoAlbumSvc;

        public HomeController(IOptions<PhotoOrderConfig> config)
        {
            this.config = config;
            this.photoAlbumSvc = new PhotoAlbumService(this.config.Value);
        }

        public IActionResult Index(int page = 1, int pageSize = 2000, bool clearLocalStorage = false)
        {
            var photoAlbum = this.photoAlbumSvc.GetAlbum(page, pageSize);
            ViewData["Title"] = this.config.Value.Title;
            ViewData["clearLocalStorage"] = clearLocalStorage;
            
            return View(photoAlbum);
        }

        public IActionResult Encomendar()
        {
            ViewData["Title"] = this.config.Value.Title;
            ViewData["UnitPrice"] = this.config.Value.FotoPrice;
            return View();
        }

        [HttpPost]
        public IActionResult Encomendar(EncomendaRequest form)
        {
            if (this.photoAlbumSvc.ValidateForm(form))
            {
                this.photoAlbumSvc.CreateOrderRequest(form);
            }

            return this.RedirectToAction("Index", new { page = 1, pageSize = 2000, clearLocalStorage = true });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
