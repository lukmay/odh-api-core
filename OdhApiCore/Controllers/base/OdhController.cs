﻿using Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

namespace OdhApiCore.Controllers
{
    [ApiController]
    [Route("v1")]
    [FormatFilter]
    public abstract class OdhController : ControllerBase
    {
        private readonly IWebHostEnvironment env;
        private readonly ISettings settings;

        public OdhController(IWebHostEnvironment env, ISettings settings, ILogger<OdhController> logger, QueryFactory queryFactory)
        {
            this.env = env;
            this.settings = settings;
            this.Logger = logger;
            this.QueryFactory = queryFactory;
        }

        protected ILogger<OdhController> Logger { get; }

        protected QueryFactory QueryFactory { get; }

        protected bool FilterCC0License => FilterClosedData;

        protected bool FilterClosedData
        {
            get
            {
                var roles = new[] {
                    "DataReader",
                    $"{this.ControllerContext.ActionDescriptor.ControllerName}Reader"
                };
                return !roles.Any(User.IsInRole);
            }
        }
        
        protected IEnumerable<string> UserRolesList
        {
            get
            {
                var roleclaims = User.Claims.Where(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(claim => claim.Value).ToList();

                return roleclaims ?? new List<string>();
            }
        }

        protected Func<string, string> UrlGenerator
        {
            get
            {
                return self =>
                {
                    var chunks = self.Split('/', 2);
                    if (chunks.Length < 2)
                        return self;
                    var (controller, id) = (chunks[0], chunks[1]);
                    return Url.Link($"Single{controller}", new { id })!;
                };
            }
        }

        protected async Task<IActionResult> DoAsync(Func<Task<IActionResult>> f)
        {
            try
            {
                return await f();
            }
            catch (PostGresSQLHelperException ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
            catch (JsonPathException ex)
            {
                return this.BadRequest(new
                {
                    error = "Invalid JSONPath selection",
                    path = ex.Path,
                    details = env.IsDevelopment() ? ex.ToString() : ex.Message
                });
            }
            catch (Exception ex)
            {
                if (ex.Message == "Request Error")
                    return this.BadRequest(new { error = env.IsDevelopment() ? ex.ToString() : ex.Message });
                else
                    return this.StatusCode(StatusCodes.Status500InternalServerError, new { error = env.IsDevelopment() ? ex.ToString() : ex.Message });
            }
        }

        protected Task<IActionResult> DoAsyncReturn(Func<Task<object?>> f)
        {
            return DoAsync(async () =>
            {
                object? result = await f();
                if (result == null)
                    return this.NotFound();
                else
                    return this.Ok(result);
            });
        }
    }
}