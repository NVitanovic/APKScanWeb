﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;
namespace APKScanWeb.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class RealIP
    {
        private readonly RequestDelegate _next;

        public RealIP(RequestDelegate next)
        {
            _next = next;
        }
        public Task Invoke(HttpContext httpContext)
        {
            try
            {
                httpContext.Request.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(httpContext.Request.Headers["X-Forwarded-For"]);
            }
            catch { }
            return _next(httpContext);
        }
    }
    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class RealIPExtensions
    {
        public static IApplicationBuilder UseRealIP(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RealIP>();
        }
    }
}
