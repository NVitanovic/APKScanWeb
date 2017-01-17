using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APKScanWeb.Models.Entities
{
    public class Access_Count
    {
        public string ip { get; set; }
        public int hits { get; set; }
    }
    public class Access_Time
    {
        public string ip { get; set; }
        public DateTime last_access { get; set; }
    }
    public class Access_Tokens
    {
        public string token_name { get; set; }
        public int quota { get; set; }
        public string description { get; set; }
        public bool valid { get; set; }
    }
}
