﻿#nullable enable
using System;
using Aiursoft.Scanner.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.Archon.SDK.Services
{
    /// <summary>
    /// For storaging other apps with appid and appsecret for current app.
    /// </summary>
    public class AppsContainer : ISingletonDependency
    {
        public static string? CurrentAppId;
        public static string? CurrentAppSecret;
        private readonly List<AppContainer> _allApps;
        private readonly IServiceScopeFactory _scopeFactory;

        public AppsContainer(
            IServiceScopeFactory scopeFactory)
        {
            _allApps = new List<AppContainer>();
            _scopeFactory = scopeFactory;
        }

        public async Task<string> AccessToken()
        {
            return await AccessToken(
                CurrentAppId ?? throw new NullReferenceException($"{nameof(CurrentAppId)} is null!"), 
                CurrentAppSecret ?? throw new NullReferenceException($"{nameof(CurrentAppSecret)} is null!"));
        }

        public async Task<string> AccessToken(string appId, string appSecret)
        {
            var app = GetApp(appId, appSecret);
            return await app.AccessToken(_scopeFactory);
        }

        private AppContainer GetApp(string appId, string appSecret)
        {
            var exists = _allApps.FirstOrDefault(t => t.AppId == appId);
            if (exists != null)
            {
                return exists;
            }

            var newContainer = new AppContainer(appId, appSecret);
            _allApps.Add(newContainer);
            exists = newContainer;
            return exists;
        }
    }
}