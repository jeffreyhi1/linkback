using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using LinkbackNet;

namespace MvcDemo
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("Trackback-Send", "trackback/{id}",
                new { Controller = "Linkback", Action = "Send", Linkback = new Trackback(), Id = 0 },
                new { HttpMethod = new HttpMethodConstraint(new[] { "GET", "POST" }) });

            routes.MapRoute("Trackback-Receive", "post/{id}/trackback",
                new { Controller = "Linkback", Action = "Receive", Linkback = new Trackback(), Id = 0 },
                new { HttpMethod = new HttpMethodConstraint(new[] { "POST" }) });

            routes.MapRoute("Pingback-Send", "pingback/{id}",
                new { Controller = "Linkback", Action = "Send", Linkback = new Pingback(), Id = 0 },
                new { HttpMethod = new HttpMethodConstraint(new[] { "GET", "POST" }) });

            routes.MapRoute("Pingback-Receive", "post/pingback",
                new { Controller = "Linkback", Action = "Receive", Linkback = new Pingback(), Id = 0 },
                new { HttpMethod = new HttpMethodConstraint(new[] { "POST" }) });

            routes.MapRoute("Post", "Post/Details/{id}",
                new { controller = "Post", action = "Details", id = 0 },
                new { HttpMethod = new HttpMethodConstraint(new[] { "GET" }) });

            routes.MapRoute(
                "Default",                                              // Route name
                "{controller}/{action}/{id}",                           // URL with parameters
                new { controller = "Home", action = "Index", id = "" }  // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            RegisterRoutes(RouteTable.Routes);
        }
    }
}