using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using APKScanWeb.Models;
using Microsoft.Extensions.Options;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace APKScanWeb.Controllers
{
    public class ConfigController : Controller
    {
        public Configuration config { get; set; }

        public ConfigController(IOptions<Configuration> settings)
        {
            config = settings.Value;
        }
    }
}
