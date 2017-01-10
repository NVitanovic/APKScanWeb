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

        public ScanController(IOptions<Configuration> settings) : base(settings)
        {
        }

        [HttpGet("{hash}")]
        public JsonResult Get(string hash)
        {
            var error =  new { error = "Invalid md5 hash!" };
            if (!hash.All(char.IsLetterOrDigit))
                return Json(error);
            var result = scanModel.getScanResult(hash);
            return Json(result);
        }
        // upload the file to the server for scanning
        // POST api/values
        [HttpPost]
        public async Task<JsonResult> Post()
        {
            Console.WriteLine("0");
            //get the file from the body
            var uploadedFile = Request.Form.Files["file"];
            if (uploadedFile == null)
                throw new Exception("Error while uploading the file NULL!");

            Console.WriteLine("1");
            //now lets read the file data
            var fileData = new byte[uploadedFile.Length];
            uploadedFile.OpenReadStream().Read(fileData, 0, Convert.ToInt32(uploadedFile.Length));

            Console.WriteLine("2");
            //generate md5 hash and write the file to the specific directory
            var md5 = Helpers.GetMD5HashFromBytes(fileData);
            if(!scanModel.uploadToDirectory(Program.config.fileuploadpath, md5, fileData))
            {
                var error = new { error = "Error while uploading the file!" };
                return await Task.FromResult<JsonResult>(Json(error));
            }

            Console.WriteLine("3");
            //Put the file to the redis queue
            var num = scanModel.addFileToRedisQueue(uploadedFile.FileName, md5);

            Console.WriteLine("4");
            return await Task.FromResult<JsonResult>(Json(new { success = "File uploaded successfully!", queue = num }));
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
