using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using LinkbackNet;
using MvcDemo.Models;
using System.Text.RegularExpressions;

namespace MvcDemo.Controllers
{
    public class LinkbackController : Controller
    {
        LinkbacksEntities _db;

        public LinkbackController()
        {
            _db = new LinkbacksEntities();
        }

        Regex _regex_url = new Regex(".*?[[].+?[]].*?[(]([^\"]+?)[)].*?", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public ActionResult Send(Linkback linkback, int id)
        {
            var post = _db.Post.First(x => x.Id == id);

            var urls = _regex_url.Matches(post.Content).Cast<Match>().Select(x => x.Groups[1].ToString());

            ViewData["Linkback-Name"] = linkback.Name;

            return View(urls);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Send(ILinkback linkback, int id, string url, bool? autodiscovery)
        {
            var post = _db.Post.First(x => x.Id == id);

            string source_url = Url.AbsoluteRouteUrl("Post", new { id });

            var parameters = new LinkbackSendParameters
            {
                // Trackback
                Title = post.Title,
                Excerpt = post.Content,
                Url = new Uri(source_url),
                BlogName = "Linkback.NET Demo",
                AutoDiscovery = autodiscovery,
                
                // Linkback
                SourceUrl = new Uri(source_url),
                TargetUrl = new Uri(url)
            };

            var result = linkback.Send(new Uri(url), parameters);

            TempData["Linkback-Send-Result"] = result.Success
                ? String.Format("{0} for {1} sent", linkback.Name, url)
                : String.Format("Error: {0}({1})", result.Message, result.Code);

            return RedirectToRoute(String.Format("{0}-Send", linkback.Name), new { id });
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Receive(ILinkback linkback, int id)
        {
            Uri target_url = linkback is Pingback ? null : new Uri(Url.AbsoluteRouteUrl("Post", new { id }));

            IReceiveResult context = linkback.Receive(Request, target_url);

            if (context.Valid)
            {
                var comment = new Comment
                {
                    Created = DateTime.Now,
                    From = String.Format("{0} from {1}", linkback.Name, context.Url),
                    Content = context.Excerpt ?? context.Title
                };

                if (linkback is Pingback)
                {
                    id = Int32.Parse(context.TargetUri.ToString().Substring(context.TargetUri.ToString().LastIndexOf("/") + 1));
                }

                var post = _db.Post.First(x => x.Id == id);
                post.Comments.Add(comment);
                _db.SaveChanges();
            }

            linkback.SendResponse(Response);

            return new EmptyResult();
        }

    }
}