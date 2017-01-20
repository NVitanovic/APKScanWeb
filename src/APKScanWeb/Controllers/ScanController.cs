using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APKScanWeb.Models;
using Microsoft.Extensions.Options;
// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace APKScanWeb.Controllers
{
    [Route("api/[controller]")]
    public class ScanController : ConfigController
    {
        private Scan scanModel = new Scan();
        //-------------------------------------------------------------------------------------------------------------------------------
        public ScanController(IOptions<Configuration> settings) : base(settings)
        {}
        //-------------------------------------------------------------------------------------------------------------------------------
        [HttpGet("redirect")]
        public void RedirectToWebsite()
        {
            this.Response.Redirect("http://www.apkscan.online/");
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        [HttpGet("{hash}")]
        public JsonResult Get(string hash)
        {
            //check the validity of the md5 hash
            if (!hash.All(char.IsLetterOrDigit))
                return Json(new { error = "Invalid md5 hash!" });

            //get the scan result
            var result = scanModel.getScanResult(hash);

            //check the if the file result is in the DB
            if (result == null)
                return Json(new { error = "File not found!" });

            //allow access from any server
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            return Json(result);
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        // upload the file to the server for scanning
        // POST api/scan
        [HttpPost]
        public async Task<JsonResult> Post()
        {
            //get the "file" from the body
            var uploadedFile = Request.Form.Files["file"];

            //check the filesize if it's over maximum
            if(uploadedFile.Length > config.maxfilesize*1024*1024)
                return await Task.FromResult<JsonResult>(Json(new { error = "File size is greater than allowed!" }));

            //get the uploader IP
            var uploadIp = Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            //check if the uploaded file is actually sent
            if (uploadedFile == null)
                return await Task.FromResult<JsonResult>(Json(new { error = "Error while uploading the file!" }));
            //throw new Exception("Error while uploading the file NULL!");

            //now lets read the file data
            var fileData = new byte[uploadedFile.Length];
            uploadedFile.OpenReadStream().Read(fileData, 0, Convert.ToInt32(uploadedFile.Length));

            //generate md5 hash and write the file to the specific directory
            var md5 = Helpers.GetMD5HashFromBytes(fileData);
            if(!scanModel.uploadToDirectory(config.fileuploadpath, md5, fileData))
                return await Task.FromResult<JsonResult>(Json(new { error = "Can't write to the download directory!" }));

            //put the file to the redis queue
            var num = scanModel.addFileToRedisSendQueue(uploadedFile.FileName, md5, uploadIp);

            //allow access from any server
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            //show the result and queue number
            return await Task.FromResult<JsonResult>(Json(new { success = "File uploaded successfully!", queue = num, hash = md5 }));
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
