using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APKScanWeb.Models;
using Microsoft.Extensions.Options;

namespace APKScanWeb.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ConfigController
    {
        public ValuesController(IOptions<Configuration> settings) : base(settings) {}

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
           
            return new string[] { "value1", "value2", config.cassandra.keyspace, config.cassandra.servers[0] };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public Dictionary<string,string> Get(int id)
        {
            Dictionary<string, string> outp = new Dictionary<string, string>();
            outp.Add("test", id.ToString());

            return outp;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
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
