using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using MvcDemo.Models;

namespace MvcDemo.Controllers
{
    public class PostController : Controller
    {
        LinkbacksEntities _db;

        public PostController()
        {
            _db = new LinkbacksEntities();
        }

        //
        // GET: /Post/

        public ActionResult Index()
        {
            var posts = _db.Post.Include("Comments").OrderByDescending(x => x.Created).ToList();

            return View(posts);
        }

        //
        // GET: /Post/Details/5

        public ActionResult Details(int id)
        {
            LinkbackNet.Pingback.DeclareServiceInHttpHeader(Response, new Uri(Url.AbsoluteRouteUrl("Pingback-Receive", new { })));

            var post = _db.Post.Include("Comments").First(x => x.Id == id);

            return View(post);
        }

        //
        // GET: /Post/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /Post/Create

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                var post = new Post { Created = DateTime.Now, Title = collection["Title"], Content = collection["Content"] };

                _db.AddToPost(post);

                _db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Post/Edit/5
 
        public ActionResult Edit(int id)
        {
            var post = _db.Post.First(x => x.Id == id);

            return View(post);
        }

        //
        // POST: /Post/Edit/5

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                var post = _db.Post.First(x => x.Id == id);

                UpdateModel<Post>(post, new string[] {"Title", "Content"}, collection.ToValueProvider());

                _db.SaveChanges();
 
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Post/Delete/5

        public ActionResult Delete(int id)
        {
            if (ControllerContext.HttpContext.Request.RequestType == "POST")
            {
                try
                {
                    var post = _db.Post.First(x => x.Id == id);

                    _db.DeleteObject(post);

                    _db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                var post = _db.Post.First(x => x.Id == id);

                return View(post);
            }
        }
    }
}