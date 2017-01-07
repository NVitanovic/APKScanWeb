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

namespace APKScanWeb.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ConfigController
    {
        public IFormFile myFile { set; get; }

        public ValuesController(IOptions<Configuration> settings) : base(settings) {}

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2", Program.config.cassandra.keyspace, config.cassandra.servers[0], Helpers.TestLatencyRedis(), Helpers.TestCassandra() };
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
            
            return await Task.FromResult<string>(Helpers.GetMD5HashFromBytes(x));
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
