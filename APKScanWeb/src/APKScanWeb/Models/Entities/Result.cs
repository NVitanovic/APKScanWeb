using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;
namespace APKScanWeb.Models.Entities
{
    public class Result : ScanResult
    {
        public DateTime last_scan { get; set; }
        public DateTime first_scan { get; set; }
    }
}
