using System.Web.Mvc;

namespace Sample.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(IndexModel model)
        {
            if (ModelState.IsValid)
            {
                ModelState.Clear();
                ViewBag.Posted = true;
            }

            return View();
        }
    }
}
