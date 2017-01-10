using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APKScanWeb.Models
{
    public class Result
    {
        public string hash { get; set; }
        public List<string> filename { get; set; } = new List<string>();
        //public List<string> uploadIp { get; set; }
        public int hits { get; set; }
        public Dictionary<string,string> av { get; set; }
    }
}
