using System.Diagnostics;
using System.Web.Mvc;

namespace AutoSuggestWeb
{
    public class StopwatchAttribute : ActionFilterAttribute
    {
        private Stopwatch _stopwatch;

        public StopwatchAttribute()
        {
            _stopwatch = new Stopwatch();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            _stopwatch.Start();
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            _stopwatch.Stop();

            var httpContext = filterContext.HttpContext;
            var response = httpContext.Response;

            response.AddHeader("X-Stopwatch", _stopwatch.Elapsed.ToString());
        }
    }
}