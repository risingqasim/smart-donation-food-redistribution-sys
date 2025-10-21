using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.Services;

namespace SmartDonationSystem.Controllers
{
    [Authorize]
    public class MLController : Controller
    {
        private readonly MLService _mlService;

        public MLController(MLService mlService)
        {
            _mlService = mlService;
        }

        // GET: ML/Analytics
        [Authorize(Roles = "Admin")]
        public IActionResult Analytics()
        {
            return View();
        }
    }
}
