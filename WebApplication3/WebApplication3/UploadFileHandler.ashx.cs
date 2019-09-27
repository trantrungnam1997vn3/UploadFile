using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication3
{
    /// <summary>
    /// Summary description for UploadFileHandler
    /// </summary>
    public class UploadFileHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string filePath = "/file-uploaded/";

            //write your handler implementation here.
            if (context.Request.Files.Count <= 0)
            {
                context.Response.Write("No file uploaded");
            }
            else
            {
                for (int i = 0; i < context.Request.Files.Count; ++i)
                {
                    HttpPostedFile file = context.Request.Files[i];
                    string mappedPath = context.Server.MapPath(filePath + file.FileName);
                    file.SaveAs(mappedPath);
                    context.Response.Write(file.FileName + " --> Upload Successful");
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}