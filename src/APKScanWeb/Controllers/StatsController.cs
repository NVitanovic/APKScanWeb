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
        public JsonResult Index()
        {
            return statsModel.getStats();
        }
        private JsonResult addToCollection(string collection)
        {
            var userIp = Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            var stream = Request.Body;
            StreamReader reader = new StreamReader(stream);
            string data = reader.ReadToEnd();

            if (collection == "scan" && !statsModel.addToScanCollection(data,userIp))
                return Json(new { success = false });
            else if (collection == "result" && !statsModel.addToResultCollection(data,userIp))
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
