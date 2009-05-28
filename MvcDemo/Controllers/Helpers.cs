using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcDemo.Controllers
{
    public static class Helpers
    {
        public static string AbsoluteRouteUrl(this System.Web.Mvc.UrlHelper url, string routeName, object values)
        {
            Uri requestUrl = url.RequestContext.HttpContext.Request.Url;

            string absoluteRouteUrl = String.Format("{0}://{1}{2}", requestUrl.Scheme, requestUrl.Authority, url.RouteUrl(routeName, values));

            return absoluteRouteUrl;
        }
    }
}
