using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using MvcDemo.Models;

namespace MvcDemo.Controllers
{
    public class CommentController : Controller
    {
        LinkbacksEntities _db;

        public CommentController()
        {
            _db = new LinkbacksEntities();
        }

        //
        // GET: /Comment/Delete

        public ActionResult Delete(int id)
        {
            if (ControllerContext.HttpContext.Request.RequestType == "POST")
            {
                try
                {
                    var comment = _db.Comment.First(x => x.Id == id);

                    _db.DeleteObject(comment);

                    _db.SaveChanges();

                    return RedirectToAction("Index", "Post");
                }
                catch
                {
                    return View();
                }
            }
            else
            {
                var comment = _db.Comment.First(x => x.Id == id);

                return View(comment);
            }
        }
    }
}
