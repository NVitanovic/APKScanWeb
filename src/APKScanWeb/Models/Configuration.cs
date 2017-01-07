using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
namespace APKScanWeb.Models
{
    public class Configuration
    {
        public Redis redis { get; set; }
        public Cassandra cassandra { get; set; }
        
    }
    public class Redis
    {
        public List<string> masters { get; set; }
        public List<string> slaves { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
    public class Cassandra
    {
        public List<string> servers { get; set; }
        public string keyspace { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
}
