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
            string userIp = httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            string userToken = httpContext.Request.Headers["Access-Token"];

            //check if token contains only letters and numbers 
            if (userToken != null && !userToken.All(char.IsLetterOrDigit))
                httpContext.Response.StatusCode = 406; //not acceptable

            IMapper mapper = new Mapper(dl.cassandra);
            Access_Count accessCount = null;
            Access_Time accessTime = null;
            Access_Tokens accessToken = null;

            //get data
            try
            {
                 accessCount = mapper.Single<Access_Count>("SELECT * FROM access_count WHERE ip = ?", userIp);
                 accessTime  = mapper.Single<Access_Time>("SELECT * FROM access_time WHERE ip = ?", userIp);
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
                    //httpContext.Abort();
                    //check if you have access token and it's valid
                    try
                    {
                        accessToken = mapper.Single<Access_Tokens>("SELECT * FROM access_tokens WHERE token_name = ?", userToken);
                        
                        //is token valid
                        if (!accessToken.valid)
                        {
                            httpContext.Response.StatusCode = 403;
                            httpContext.Response.Body.WriteByte(0);
                        }
                            //check if the client quota is out of range
                        if (accessToken.quota < accessCount.hits)
                        {
                            //quota is over user limited forbidden
                            httpContext.Response.StatusCode = 403;
                            httpContext.Response.Body.WriteByte(0);
                        }    
                    }
                    catch
                    {
                        //set unauthorized so the key is invalid and it's already out of quota
                        httpContext.Response.StatusCode = 401;
                        httpContext.Response.Body.WriteByte(0);
                    }
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
