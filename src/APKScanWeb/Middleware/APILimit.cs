using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using APKScanWeb.Models.Entities;
using MySql.Data.MySqlClient;

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

            Access_Count accessCount = new Access_Count();
            Access_Time accessTime = new Access_Time();
            Access_Tokens accessToken = new Access_Tokens();

            //get data
            try
            {
                MySqlCommand cmdAccessCount = new MySqlCommand("SELECT ip, COUNT(*) as count FROM access_count WHERE ip = @ip", dl.mysql);
                cmdAccessCount.Parameters.AddWithValue("@ip", userIp);
                var row = cmdAccessCount.ExecuteReader();
                if (row.HasRows)
                {
                    row.Read();
                    accessCount.hits = Convert.ToInt32(row["count"]);
                    accessCount.ip = row["ip"].ToString();
                }
                else
                {
                    accessCount = null;
                }
                row.Close();

                MySqlCommand cmdAccessTime = new MySqlCommand("SELECT created_at FROM access_count WHERE ip = @ip ORDER BY created_at DESC LIMIT 1", dl.mysql);
                cmdAccessTime.Parameters.AddWithValue("@ip", userIp);
                row = cmdAccessTime.ExecuteReader();
                if (row.HasRows)
                {
                    row.Read();
                    accessTime.ip = userIp;
                    accessTime.last_access = Convert.ToDateTime(row["created_at"]);
                }
                else
                {
                    accessTime = null;
                }
                row.Close();
            }
            catch(Exception e)
            {
                accessCount = null;
                accessTime = null;
            }

            if(accessCount != null && accessTime != null)
            {
                //if there are more than specified number of requests for one hour
                if((DateTime.UtcNow - accessTime.last_access) < TimeSpan.FromHours(1))
                {
                    if (accessCount.hits > Program.config.maxhourlyrequests)
                    {
                        //httpContext.Abort();
                        //check if you have access token and it's valid
                        try
                        {
                            MySqlCommand cmdAccessToken = new MySqlCommand("SELECT * FROM access_tokens WHERE token_name = @token LIMIT 1", dl.mysql);
                            cmdAccessToken.Parameters.AddWithValue("@token", userToken);
                            var row = cmdAccessToken.ExecuteReader();
                            if (row.HasRows)
                            {
                                row.Read();
                                accessToken.description = row["description"].ToString();
                                accessToken.token_name = row["token_name"].ToString();
                                accessToken.quota = Convert.ToInt32(row["quota"]);
                                accessToken.valid = Convert.ToBoolean(row["valid"]);
                            }
                            row.Close();

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
                else //clear the access count if time has passed and update the access_time
                {

                    MySqlCommand cmdDeleteAccessCount = new MySqlCommand("DELETE FROM access_count WHERE ip = @ip", dl.mysql);
                    cmdDeleteAccessCount.Parameters.AddWithValue("@ip", userIp);
                    cmdDeleteAccessCount.ExecuteNonQuery();

                    return _next(httpContext);
                }
            }


            //write data increase the hit count
            MySqlCommand cmdAddAccess = new MySqlCommand("INSERT INTO access_count (ip,token) VALUES(@ip,@token)", dl.mysql);
            cmdAddAccess.Parameters.AddWithValue("@ip", userIp);
            cmdAddAccess.Parameters.AddWithValue("@token", userToken ?? "");
            cmdAddAccess.ExecuteNonQuery();

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
