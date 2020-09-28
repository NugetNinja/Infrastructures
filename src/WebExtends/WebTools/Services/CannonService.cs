﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Aiursoft.Scanner.Interfaces;
using Aiursoft.XelNaga.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.WebTools.Services
{
    public class CannonService : ISingletonDependency
    {
        private readonly ILogger<CannonService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public CannonService(
            ILogger<CannonService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public void Fire<T>(Action<T> bullet, Action<Exception> handler = null)
        {
            Task.Run(() =>
            {
                using var scope = _scopeFactory.CreateScope();
                var dependency = scope.ServiceProvider.GetRequiredService<T>();
                try
                {
                    bullet(dependency);
                }
                catch (Exception e)
                {
                    handler?.Invoke(e);
                }
            });
        }
    }
}
