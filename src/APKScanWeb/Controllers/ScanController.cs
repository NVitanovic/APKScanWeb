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
        [HttpGet("{hash}")]
        public JsonResult Get(string hash)
        {
            var error =  new { error = "Invalid md5 hash!" };

            //check the validity of the md5 hash
            if (!hash.All(char.IsLetterOrDigit))
                return Json(error);

            //get the scan result
            var result = scanModel.getScanResult(hash);
            return Json(result);
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        // upload the file to the server for scanning
        // POST api/scan
        [HttpPost]
        public async Task<JsonResult> Post()
        {
            var error = new { error = "Error while uploading the file!" };

            //get the "file" from the body
            var uploadedFile = Request.Form.Files["file"];

            //var uploadIp = Request.Headers["X-Forwarded-For"]; //not good should be different should check this when add HAProxy or Nginx in front
            var uploadIp = Request.Host.Host; //TODO: returns localhost and probably SERVER host we need users IP

            //check if the uploaded file is actually sent
            if (uploadedFile == null)
                return await Task.FromResult<JsonResult>(Json(error));
            //throw new Exception("Error while uploading the file NULL!");

            //now lets read the file data
            var fileData = new byte[uploadedFile.Length];
            uploadedFile.OpenReadStream().Read(fileData, 0, Convert.ToInt32(uploadedFile.Length));

            //generate md5 hash and write the file to the specific directory
            var md5 = Helpers.GetMD5HashFromBytes(fileData);
            if(!scanModel.uploadToDirectory(config.fileuploadpath, md5, fileData))
                return await Task.FromResult<JsonResult>(Json(error));

            //put the file to the redis queue
            var num = scanModel.addFileToRedisSendQueue(uploadedFile.FileName, md5, uploadIp);

            //show the result and queue number
            return await Task.FromResult<JsonResult>(Json(new { success = "File uploaded successfully!", queue = num, hash = md5 }));
        }
        //-------------------------------------------------------------------------------------------------------------------------------
    }
}
