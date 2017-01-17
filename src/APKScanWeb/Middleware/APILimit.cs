using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Cassandra.Mapping;
using APKScanWeb.Models.Entities;
namespace APKScanWeb.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class APILimit
    {
        private readonly RequestDelegate _next;
        private DataLayer dl = null;
        public APILimit(RequestDelegate next)
        {
            dl = DataLayer.getInstance();
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            string userIp = httpContext.Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            string userToken
            IMapper mapper = new Mapper(dl.cassandra);
            Access_Count accessCount = null;
            Access_Time accessTime = null;
            //get data
            try
            {
                 accessCount = mapper.Single<Access_Count>("SELECT * FROM access_count WHERE ip = ?", userIp);
                 accessTime  = mapper.Single<Access_Time>("SELECT * FROM access_time WHERE ip = ?", userIp);
                //Access_Tokens accessToken = mapper.Single<Access_Tokens>("SELECT * FROM access_tokens WHERE token", userIp);
            }
            catch
            {
                accessCount = null;
                accessTime = null;
            }

            if(accessCount != null && accessTime != null)
            {
                //if there are more than specified number of requests for one hour
                if (accessCount.hits > Program.config.maxhourlyrequests && (DateTime.UtcNow - accessTime.last_access) < TimeSpan.FromHours(1))
                {
                    httpContext.Abort();
                }
            }
            //write data
            var statementCount = dl.cassandra.Prepare("UPDATE access_count SET hits = hits + 1 WHERE ip = ?").Bind(userIp);
            var statementTime = dl.cassandra.Prepare("UPDATE access_time SET last_access = toTimestamp(now()) WHERE ip = ?").Bind(userIp);

            dl.cassandra.Execute(statementCount);
            dl.cassandra.Execute(statementTime);
            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class APILimitExtensions
    {
        public static IApplicationBuilder UseAPILimit(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<APILimit>();
        }
    }
}
