using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APKScanWeb.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Net.Http;
using System.Net;
using APKScanSharedClasses;
using Newtonsoft.Json;

namespace APKScanWeb.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ConfigController
    {
        public IFormFile myFile { set; get; }
        
        public ValuesController(IOptions<Configuration> settings) : base(settings) {}

        // GET api/values
        [HttpGet]
        public string Get()
        {
            VMControl ctrl = VMControl.getInstance("https://l11.nikos-hosting.com:8006/", "koma", "komicakomica", VMControl.eAuthMethods.pve);
            RedisReceive rc = new RedisReceive();
            rc.av_results.Add("AV1", "true");
            rc.av_results.Add("AV2", "false");
            rc.av_results.Add("AV3", "true");
            rc.filename = "nekifajl";
            rc.hash = "a5e9112cff236c1410016bae68e05d86";
            rc.master_id = "master1";

            return JsonConvert.SerializeObject(rc); //Program.config.cassandra.keyspace, config.cassandra.servers[0], Helpers.TestLatencyRedis(), Helpers.TestCassandra() };
        }
        // GET api/values/send/{val}
        [HttpGet("send/{val}")]
        public string Send(string val)
        {
            return val + " " + Helpers.WriteToSend(val);
        }
        // GET api/values/test/{val}
        [HttpGet("receive")]
        public string Receive()
        {
            return Helpers.ReadFromReceive();
        }
        // GET api/values/5
        [HttpGet("{id}")]
        public Dictionary<string,string> Get(int id)
        {
            Dictionary<string, string> outp = new Dictionary<string, string>();
            outp.Add("test", id.ToString());

            return outp;
        }
        //PRIMER UPLOADA PREKO POSTa
        // POST api/values
        [HttpPost]
        public async Task<string> Post()
        {
            var uploadedFile = Request.Form.Files["file"];
            if(uploadedFile == null)
                throw new Exception("Error while uploading the file NULL!");

            var x = new byte[uploadedFile.Length];
            uploadedFile.OpenReadStream().Read(x, 0, Convert.ToInt32(uploadedFile.Length));
            Scan sc = new Scan();
            var md5 = Helpers.GetMD5HashFromBytes(x);
            if(sc.uploadToDirectory(config.fileuploadpath, md5 + ".apk", x))
                return await Task.FromResult<string>("true");
            return await Task.FromResult<string>("false");
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
