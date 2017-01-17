using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using APKScanSharedClasses;
namespace APKScanWeb.Models
{
    public class Configuration : SharedConfiguration
    {
        public string fileuploadpath { get; set; }
        public int maxhourlyrequests { get; set; }
    }
}
