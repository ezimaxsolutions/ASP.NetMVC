using EDS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace EDS.Services
{
    public static class DocumentManagementService
    {
        public static string filePath = ConfigurationManager.AppSettings["FilePath"].ToString();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static FileStream GetFile(string physicalFileName)
        {
            return System.IO.File.OpenRead(HttpContext.Current.Server.MapPath("~//" + filePath + "//" + physicalFileName));
        }
        /// <summary>
        /// This method is used to create unique file name.
        /// </summary>
        /// <param name="FileName"></param>
        public static string GetUniqueFileName(string fileName)
        {
            return (fileName + "_" + Guid.NewGuid());
        }
        /// <summary>
        /// This method is used to delete a file from a folder.
        /// </summary>
        /// <param name="FileName"></param>
        public static void DeleteFile(string physicalFileName)
        {
            string fullPath = HttpContext.Current.Request.MapPath(filePath + "" + physicalFileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        /// <summary>
        /// Thsi method is used
        /// </summary>
        /// <param name="attachmentFiles"></param>
        /// <returns></returns>
        public static bool ValidateAttachmentSize(IEnumerable<HttpPostedFileBase> attachmentFiles)
        {
            var attachedFilesSize = attachmentFiles.Sum(httpPostedFileBase => httpPostedFileBase.ContentLength / (1024 * 1024));
            return attachedFilesSize <= 20;
        }

        /// <summary>
        /// Thsi method is checks if file exists.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool HasFile(this HttpPostedFileBase file)
        {
            return (file != null && file.ContentLength > 0) ? true : false;
        }
        /// <summary>
        /// This method is used to upload a file to a root directory folder.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileName"></param>
        public static void UploadFile(HttpPostedFileBase file, string physicalFileName)
        {
            var path = HttpContext.Current.Server.MapPath("~//" + filePath + "//" + physicalFileName);
            DeleteFile(physicalFileName);
            file.SaveAs(path);

        }

        /// <summary>
        /// This method is used to get detail of a file and to call file upload method.
        /// </summary>
        /// <param name="attachmentFiles"></param>
        /// <returns></returns>
        public static List<FileDetail> GetFileDetail(IEnumerable<HttpPostedFileBase> attachmentFiles)
        {
            FileDetail fileDetail = null;
            List<FileDetail> fileDetails = new List<FileDetail>();
            foreach (var file in attachmentFiles)
            {
                if (HasFile(file))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var physicalFileName =  GetUniqueFileName(fileName);
                    var fileExt = Path.GetExtension(file.FileName);
                    fileDetail = new FileDetail { PhysicalFileName = physicalFileName, FileExtension = fileExt, OriginalFileName = fileName };
                    fileDetails.Add(fileDetail);
                    UploadFile(file, physicalFileName + "" + fileExt);
                }
            }
            return fileDetails;
        }
    }
}