using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MDGov.MDE.Common.Model;
using MDGov.MDE.Common.ServiceLayer;
using MDGov.MDE.Permitting.Model;

namespace MDGov.MDE.WSIPS.Portal.InternalWeb.Controllers
{
    public partial class CommentController : BaseController
    {
        private readonly IUpdatableService<Comment> _commentService;
        private readonly IService<LU_TableList> _tableListService;

		// constructor
        public CommentController(
            IService<LU_TableList> tableListService,
            IUpdatableService<Comment> commentService)
        {
            _commentService = commentService;
            _tableListService = tableListService;
        }

		// this controller will return the html code
        public virtual ActionResult LoadCommentsView(string refTable, int refId, int permitStatusId)
        {
            var tableList = _tableListService.GetRange(0, 1, "", new DynamicFilter[] { new DynamicFilter(String.Format("Description==\"{0}\"", refTable)) });

            // populate the viewbag items
            ViewBag.RefTableId = tableList.First().Id;
            ViewBag.RefId = refId;
            ViewBag.PermitStatusId = permitStatusId;

            return PartialView(MVC.Comment.Views.Comment);
        }

		// used for jqGrid 
		// will get a data and return it in json form
        [HttpGet]
        public virtual JsonResult Data(int id, int permitStatus)
        {
            List<DynamicFilter> filter = new List<DynamicFilter>();

            if (permitStatus == -1)
            {
                filter.Add(new DynamicFilter(String.Format("refId=={0}", id)));
            }
            else
            {
                filter.Add(new DynamicFilter(String.Format("refId=={0} && PermitStatusId=={1}", id, permitStatus)));
            }


            var comments = _commentService.GetRange(0, int.MaxValue, "PermitStatusId", filter, "LU_PermitStatus").Select(
                x => new
                {
                    Comment1 = x.Comment1.Length > 50 ? x.Comment1.Substring(0, 50) + "..." : x.Comment1,
                    LastModifiedDate = x.LastModifiedDate,
                    LastModifiedBy = x.LastModifiedBy,
                    CreatedBy = x.CreatedBy,
                    PermitStatusId = x.LU_PermitStatus != null ? x.LU_PermitStatus.Description : "",
                    Id = x.Id,
                    StatusId = x.PermitStatusId ?? -1
                }
            );


            var temp = Json(new
            {
                Data = comments
            }, JsonRequestBehavior.AllowGet);

            return temp;
        }

		// save a comment to the database
        [HttpGet]
        public virtual void SaveComment(Comment comment)
        {
            if (comment.PermitStatusId < 1)
                comment.PermitStatusId = null;

            _commentService.Save(comment);
        }

		// deletes a comment from the database
        public virtual JsonResult Delete(int id)
        {
            try
            {
                _commentService.Delete(id);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return Json(new { success = false });
            }
        }

		// returns a comment retrivied by id
        public virtual JsonResult GetComment(int id)
        {
            string userName = System.Web.HttpContext.Current.User.Identity.Name;//System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            var comment = _commentService.GetById(id);

            return Json(new { userName = userName, comment = comment }, JsonRequestBehavior.AllowGet);
        }
    }
}
