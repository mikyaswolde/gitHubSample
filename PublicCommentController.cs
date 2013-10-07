using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MDGov.MDE.Permitting.Model;
using MDGov.MDE.Common.ServiceLayer;
using System.Web.Mvc;
using MDGov.MDE.Common.Model;

namespace MDGov.MDE.WSIPS.Portal.InternalWeb.Controllers
{
    public partial class PublicCommentController : BaseController
    {
        private readonly IUpdatableService<PublicComment> _publicCommentService;

        public PublicCommentController(
            IUpdatableService<PublicComment> publicCommentService)
        {
            _publicCommentService = publicCommentService;
        }

        public virtual ActionResult LoadCommentsView()
        {

            return PartialView(MVC.Permitting.Permit.Views.DetailsPartials._PublicComment);
        }


        [HttpGet]
        public virtual JsonResult Data(int id)
        {
            //List<PublicComment> commentList = new List<PublicComment>();


            var comments = _publicCommentService.GetRange(0, 99999, "", new DynamicFilter[] { new DynamicFilter("PermitId==@0", id) }, null).Select(
                x => new 
                {
                    Comment = x.Comment.Length > 50 ? x.Comment.Substring(0, 50) + "..." : x.Comment,
                    LastModifiedDate = Convert.ToDateTime(x.LastModifiedDate).ToShortDateString(),
                    Name = x.Name,
                    Id = x.Id
                }

             );

            /*foreach (var item in comments)
            {
                if (item.Comment.Length > 50)
                {
                    item.Comment = item.Comment.Substring(0, 50) + "...";
                }

                item.CreatedDate

                commentList.Add(item);
            }
            */
            var temp = Json(new
            {
                Data = comments
            },
                        JsonRequestBehavior.AllowGet
            );

            return temp;
        }

        public virtual JsonResult Delete(int id)
        {
            try
            {
                _publicCommentService.Delete(id);
                return Json(new { success = true },
                        JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return Json(new { success = false });
            }
        }

        public virtual JsonResult GetComment(int id)
        {
            var comment = _publicCommentService.GetById(id);

            return Json(new {  comment = comment }, JsonRequestBehavior.AllowGet);

        }


        [HttpGet]
        public virtual void SaveComment(PublicComment publicComment)
        {

            _publicCommentService.Save(publicComment);
        }

    }
}