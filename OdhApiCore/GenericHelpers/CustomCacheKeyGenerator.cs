﻿using AspNetCore.CacheOutput;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OdhApiCore
{
    public class CustomCacheKeyGenerator : DefaultCacheKeyGenerator //ICacheKeyGenerator
    {
        public override string MakeCacheKey(ActionExecutingContext context, string mediaType, bool excludeQueryString = false)
        {
            var key = base.MakeCacheKey(context, mediaType, excludeQueryString);

            var userIdentity = FormatUserIdentity(context);

            return $"{key}:{userIdentity}";

            //var baseKey = MakeBaseCacheKey(context.Controller., context.ActionDescriptor);
            //var parameters = FormatParameters(context, excludeQueryString);
            //var userIdentity = FormatUserIdentity(context);

            //return string.Format("{0}{1}:{2}:{3}", baseKey, parameters, userIdentity, mediaType);
        }

        protected virtual string FormatUserIdentity(ActionExecutingContext context)
        {
            var username = "anonymous";

            if (context.HttpContext.User.Identity != null)
            {
                if (context.HttpContext.User.Identity.IsAuthenticated)
                {
                    if(!String.IsNullOrEmpty(context.HttpContext.User.Identity.Name))
                        username = context.HttpContext.User.Identity.Name.ToLower();
                }
            }

            return username;
        }
       
    }
}