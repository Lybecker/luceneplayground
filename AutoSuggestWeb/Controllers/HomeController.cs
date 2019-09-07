using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml;
using Com.Lybecker.AutoSuggest;
using Lucene.Net.Store;

namespace AutoSuggestWeb.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewData["Message"] = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        [Stopwatch]
        [OutputCache(VaryByParam = "term", Duration = 10, Location = OutputCacheLocation.Any)]
        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult GetNames(string term)
        {
            var suggestHelper = Models.SuggestHelper.GetInstance();

            var suggestions = suggestHelper.Suggest(term);

            return Json(suggestions.Select(item => string.Format("{0} ({1})",item.Term, item.Occurrence )), JsonRequestBehavior.AllowGet);
        }
    }
}
