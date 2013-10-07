using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using MDGov.MDE.Common.Model;
using MDGov.MDE.Common.ServiceLayer;
using MDGov.MDE.Permitting.Model;
using MDGov.MDE.WSIPS.Portal.InternalWeb.Controllers;

namespace MDGov.MDE.WSIPS.Portal.InternalWeb.Areas.Permitting.Controllers
{
    public partial class UploadController : BaseController
    {
        private readonly IService<LU_TableList> _tableListService;
        private readonly IUpdatableService<Document> _documentService;
        private readonly IUpdatableService<DocumentByte> _documentByteService;
        private readonly IService<Permit> _permitService;
        private readonly IUpdatableService<ConditionCompliance> _conditionComplianceService;

        public UploadController(
            IService<LU_TableList> tableListService,
            IUpdatableService<Document> documentService,
            IUpdatableService<DocumentByte> documentByteService,
            IService<Permit> permitService,
            IUpdatableService<ConditionCompliance> conditionComplianceService)
        {
            _tableListService = tableListService;
            _documentService = documentService;
            _documentByteService = documentByteService;
            _permitService = permitService;
            _conditionComplianceService = conditionComplianceService;
        }

        public virtual ActionResult Document(string refTable, int refId, int? permitStatusId, bool includeRefIdDocs = false)
        {
            var tableList = _tableListService.GetRange(0, 1, "", new DynamicFilter[] { new DynamicFilter(String.Format("Description==\"{0}\"", refTable)) });

            // populate the viewbag items
            ViewBag.RefTableId = tableList.First().Id;
            ViewBag.RefId = refId;
            ViewBag.PermitStatusId = permitStatusId;
            ViewBag.IncludeRefIdDocs = includeRefIdDocs;

            // set permissions for deletion and upload
            // 1= View Only
            // 2= Upload and View
            // 3= upload, view, and delete
            ViewBag.PermissionLevel = 3;

            // get documents from database
            //var docs = _documentService.GetRange(0, 99999, "", new DynamicFilter[] { new DynamicFilter(String.Format("refId=={0} and refTableId=={1} and permitStatusId{2}", refId, ViewBag.RefTableId, permitStatusId == null ? " == " + System.Data.SqlTypes.SqlInt32.Null : " == " + permitStatusId)) });

            // pass documents to the view
            return PartialView(MVC.Upload.Views.UploadDocument);
        }

        private string GetDirectory(Document doc)
        {
            string path = WebConfigurationManager.AppSettings["UploadFolder"].ToString();

            if (doc.PermitStatusId == null)
            {

                if (!Directory.Exists(path + doc.RefTableId + "\\" + doc.RefId))
                    Directory.CreateDirectory(path + doc.RefTableId + "\\" + doc.RefId);
                return doc.RefTableId + "\\" + doc.RefId;
            }
            else
            {
                if (!Directory.Exists(path + doc.RefTableId + "\\" + doc.RefId + "\\" + doc.PermitStatusId))
                    Directory.CreateDirectory(path + doc.RefTableId + "\\" + doc.RefId + "\\" + doc.PermitStatusId);
                return doc.RefTableId + "\\" + doc.RefId + "\\" + doc.PermitStatusId;
            }
        }

        [HttpGet]
        public virtual FileResult GetFile(int id)
        {
            var temp = _documentService.GetRange(0, 1, "", new DynamicFilter[] { new DynamicFilter(String.Format("Id=={0}", id)) }, "DocumentByte").First();
            var file = temp.DocumentByte;

            /*Response.AppendHeader("content-disposition", "inline; filename=" + temp.DocumentTitle);*/

            return File(file.Document, file.MimeType, temp.DocumentTitle);

        }

        [HttpGet]
        public virtual string GetPermitDocument(int permitId)
        {
            const int docType = 3; // Final Permit
            var temp = _documentService.GetRange(0, int.MaxValue, null, new[] { new DynamicFilter(String.Format("RefId=={0} and DocumentTypeId=={1}", permitId, docType)) }).ToList();
            if (temp.Any()) 
                return WebConfigurationManager.AppSettings["DocumentGetPath"] + temp.Last().Id;
                
            return String.Empty;
        }


        public virtual JsonResult Upload(Document doc)
        {
            if (doc.RefId == 0)
            {
                return Json(new { IsSuccess = false }, "text/html", System.Text.Encoding.UTF8, JsonRequestBehavior.AllowGet);
            }

            string docName = Path.GetFileName(Request.Files[0].FileName);

            var temp = new DocumentByte();
            //// set the title
            doc.DocumentTitle = Uri.EscapeDataString(Path.GetFileName(Request.Files[0].FileName));

            if (!doc.DocumentTypeId.HasValue && doc.DocumentTitle.Length > 10 && doc.DocumentTitle.Substring(0, 11).ToLower() == "draftpermit")
            {
                doc.DocumentTypeId = 1; // 1 = Draft Permit
            }

            int docId = 0;

            HttpPostedFileBase file = Request.Files[0];
            if (file.ContentLength > 0)
            {

                MemoryStream target = new MemoryStream();
                file.InputStream.CopyTo(target);
                byte[] data = target.ToArray();
                temp.Document = data;
                temp.MimeType = file.ContentType;


                docId = _documentByteService.Save(temp);
                doc.DocumentByteId = docId;
                docId = _documentService.Save(doc);

            }

            if (docId == 0)
            {
                return null;
            }

            // If upload is Condition Compliance Report, set Condition Compliance status to "Submitted"
            if (doc.RefTableId == 66)
            {
                if (doc.ConditionComplianceId != null) {
                    var conditionCompliance = _conditionComplianceService.GetById((int) doc.ConditionComplianceId);
                    conditionCompliance.PermitConditionComplianceStatusId = 5;
                    _conditionComplianceService.Save(conditionCompliance);
                }
            }
            return Json(new { IsSuccess = true }, "text/html", System.Text.Encoding.UTF8,
                        JsonRequestBehavior.AllowGet
            );
        }


        [HttpGet]
        public virtual JsonResult Data(int id, int refTableId, int permitStatusId, bool includeRefIdDocs = false)
        {
            List<DynamicFilter> filter = new List<DynamicFilter>();
            List<int> combinedPermitStatuses = new List<int>();
            List<int> combinedRefIds = new List<int>();
            combinedPermitStatuses.AddRange(new int[] { 22, 23, 32, 33, 41, 42, 59, 62, 65 });

            if (includeRefIdDocs)
            {
                var permit = _permitService.GetById(id);
                if (permit != null && permit.RefId.HasValue)
                {
                    combinedRefIds.AddRange(new int[] { id, permit.RefId.Value });
                }
                else
                {
                    combinedRefIds.Add(id);
                }
            }
            else
            {
                combinedRefIds.Add(id);
            }


            if (permitStatusId == -1)
            {
                filter.Add(new DynamicFilter("@0.Contains(outerIt.RefId)", combinedRefIds));
            }
            else if (permitStatusId == 22 || permitStatusId == 23 || permitStatusId == 32 || permitStatusId == 33
                    || permitStatusId == 41 || permitStatusId == 42 || permitStatusId == 59 || permitStatusId == 62
                    || permitStatusId == 65)
            {
                filter.Add(new DynamicFilter("@0.Contains(outerIt.PermitStatusId)", combinedPermitStatuses));
                filter.Add(new DynamicFilter("@0.Contains(outerIt.RefId)", combinedRefIds));
            }
            else
            {
                filter.Add(new DynamicFilter("@0.Contains(outerIt.RefId) && PermitStatusId==@1", combinedRefIds, permitStatusId));
            }



            var docs = _documentService.GetRange(0, 99999, "PermitStatusId", filter, "LU_PermitStatus").Select(
                x => new
                {
                    DocumentTitle = Uri.UnescapeDataString(x.DocumentTitle),
                    Description = x.Description,
                    CreatedDate = x.CreatedDate,
                    CreatedBy = x.CreatedBy,
                    PermitStatusId = x.LU_PermitStatus != null ? x.LU_PermitStatus.Description : "",
                    Id = x.Id,
                    StatusId = x.PermitStatusId ?? -1
                }
            );

            return Json(new
            {
                Data = docs,
                DocumentGetPath = System.Web.Configuration.WebConfigurationManager.AppSettings["DocumentGetPath"].ToString()
            },
                        JsonRequestBehavior.AllowGet
            );


        }


        public virtual JsonResult Delete(int id)
        {
            try
            {
                var documentByteId = _documentService.GetById(id).DocumentByteId ?? 0;
                _documentService.Delete(id);
                _documentByteService.Delete(documentByteId);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }



    }
}
