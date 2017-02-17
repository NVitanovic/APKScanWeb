using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APKScanWeb.Models;
using System.IO;

namespace APKScanWeb.Controllers
{
    [Route("api/[controller]")]
    public class StatsController : Controller
    {
        private Stats statsModel = new Stats();
        public String Index()
        {
            return "";
        }
        private JsonResult addToCollection(string collection)
        {
            var stream = Request.Body;
            StreamReader reader = new StreamReader(stream);
            string data = reader.ReadToEnd();
            if (!statsModel.addToScanCollection(data))
                return Json(new { success = false });
            return Json(new { success = true });
        }
        [HttpPost("scan")]
        public JsonResult addToScan()
        {
            return addToCollection("scan");
        }
        [HttpPost("result")]
        public JsonResult addToResult()
        {
            return addToCollection("result");
        }
    }
}
