﻿using Aiursoft.Developer.SDK.Services.ToDeveloperServer;
using Aiursoft.DocGenerator.Attributes;
using Aiursoft.Gateway.Data;
using Aiursoft.Gateway.Models;
using Aiursoft.Gateway.Models.ThirdPartyAddressModels;
using Aiursoft.Gateway.Models.ThirdPartyViewModels;
using Aiursoft.Gateway.SDK.Models;
using Aiursoft.Gateway.Services;
using Aiursoft.Handler.Attributes;
using Aiursoft.Handler.Exceptions;
using Aiursoft.Identity;
using Aiursoft.Identity.Services.Authentication;
using Aiursoft.XelNaga.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.Gateway.Controllers
{
    [GenerateDoc]
    [LimitPerMin]
    [Route("Third-party")]
    public class ThirdPartyController : Controller
    {
        private readonly IEnumerable<IAuthProvider> _authProviders;
        private readonly GatewayDbContext _dbContext;
        private readonly UserAppAuthManager _authManager;
        private readonly DeveloperApiService _apiService;
        private readonly UserManager<GatewayUser> _userManager;
        private readonly SignInManager<GatewayUser> _signInManager;
        private readonly AuthLogger _authLogger;

        public ThirdPartyController(
            IEnumerable<IAuthProvider> authProviders,
            GatewayDbContext dbContext,
            UserAppAuthManager authManager,
            DeveloperApiService apiService,
            UserManager<GatewayUser> userManager,
            SignInManager<GatewayUser> signInManager,
            AuthLogger authLogger)
        {
            _authProviders = authProviders;
            _dbContext = dbContext;
            _authManager = authManager;
            _apiService = apiService;
            _userManager = userManager;
            _signInManager = signInManager;
            _authLogger = authLogger;
        }

        [Route("sign-in/{providerName}")]
        public async Task<IActionResult> SignIn(SignInAddressModel model)
        {
            var provider = _authProviders.SingleOrDefault(t => t.GetName().ToLower() == model.ProviderName.ToLower());
            if (provider == null)
            {
                return NotFound();
            }
            var oauthModel = model.BuildOAuthInfo();
            IUserDetail info;
            try
            {
                info = await provider.GetUserDetail(model.Code);
            }
            catch (AiurAPIModelException)
            {
                var refreshLink = provider.GetSignInRedirectLink(new AiurUrl("", new FinishAuthInfo
                {
                    AppId = oauthModel.AppId,
                    RedirectUri = oauthModel.RedirectUri,
                    State = oauthModel.State,
                }));
                return Redirect(refreshLink);
            }
            var account = await _dbContext
                .ThirdPartyAccounts
                .Include(t => t.Owner)
                .ThenInclude(t => t.Emails)
                .Where(t => t.Owner != null)
                .Where(t => t.OpenId != null)
                .FirstOrDefaultAsync(t => t.OpenId == info.Id);
            var app = (await _apiService.AppInfoAsync(oauthModel.AppId)).App;
            if (account != null)
            {
                await _authLogger.LogAuthRecord(account.OwnerId, HttpContext, true, app.AppId);
                await _signInManager.SignInAsync(account.Owner, true);
                return await _authManager.FinishAuth(account.Owner, oauthModel, app.ForceConfirmation, app.TrustedApp);
            }
            var viewModel = new SignInViewModel
            {
                RedirectUri = oauthModel.RedirectUri,
                State = oauthModel.State,
                AppId = oauthModel.AppId,
                UserDetail = info,
                ProviderName = model.ProviderName,
                AppImageUrl = app.IconPath,
                CanFindAnAccountWithEmail = await _dbContext.UserEmails.AnyAsync(t => t.EmailAddress.ToLower() == info.Email.ToLower()),
                Provider = provider
            };
            return View(viewModel);
        }

        [HttpPost]
        [Route("create-account-and-bind/{providerName}")]
        public async Task<IActionResult> CreateAccountAndBind(SignInViewModel model)
        {
            var app = (await _apiService.AppInfoAsync(model.AppId)).App;
            var exists = _dbContext.UserEmails.Any(t => t.EmailAddress == model.UserDetail.Email.ToLower());
            if (exists)
            {
                ModelState.AddModelError(string.Empty, $"An user with email '{model.UserDetail.Email}' already exists!");
                model.AppImageUrl = app.IconPath;
                model.CanFindAnAccountWithEmail = false;
                model.Provider = _authProviders.SingleOrDefault(t => t.GetName().ToLower() == model.ProviderName.ToLower());
                return View(nameof(SignIn), model);
            }
            var user = new GatewayUser
            {
                UserName = model.UserDetail.Email + $".from.{model.ProviderName}.com",
                Email = model.UserDetail.Email,
                NickName = model.UserDetail.Name,
                PreferedLanguage = model.PreferedLanguage,
                IconFilePath = AuthValues.DefaultImagePath,
                RegisterIPAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                var primaryMail = new UserEmail
                {
                    EmailAddress = model.UserDetail.Email.ToLower(),
                    OwnerId = user.Id,
                    ValidateToken = Guid.NewGuid().ToString("N")
                };
                await _dbContext.UserEmails.AddAsync(primaryMail);
                await _dbContext.SaveChangesAsync();

                var link = new ThirdPartyAccount
                {
                    OwnerId = user.Id,
                    ProviderName = model.ProviderName,
                    OpenId = model.UserDetail.Id,
                    Name = model.UserDetail.Name,
                };
                await _dbContext.ThirdPartyAccounts.AddAsync(link);
                await _dbContext.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: true);
                await _authLogger.LogAuthRecord(user.Id, HttpContext, true, model.AppId);
                return await _authManager.FinishAuth(user, model, app.ForceConfirmation, app.TrustedApp);
            }
            else
            {
                model.AppImageUrl = app.IconPath;
                model.CanFindAnAccountWithEmail = await _dbContext.UserEmails.AnyAsync(t => t.EmailAddress.ToLower() == model.UserDetail.Email.ToLower());
                model.Provider = _authProviders.SingleOrDefault(t => t.GetName().ToLower() == model.ProviderName.ToLower());
                ModelState.AddModelError(string.Empty, result.Errors.First().Description);
                return View(nameof(SignIn), model);
            }
        }

        [Authorize]
        [Route("bind-account/{providerName}")]
        public async Task<IActionResult> BindAccount(BindAccountAddressModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user.ThirdPartyAccounts.Any(t => t.ProviderName == model.ProviderName))
            {
                var toDelete = await _dbContext.ThirdPartyAccounts
                    .Where(t => t.OwnerId == user.Id)
                    .Where(t => t.ProviderName == model.ProviderName)
                    .ToListAsync();
                _dbContext.ThirdPartyAccounts.RemoveRange(toDelete);
                await _dbContext.SaveChangesAsync();
            }
            var provider = _authProviders.SingleOrDefault(t => t.GetName().ToLower() == model.ProviderName.ToLower());
            if (provider == null)
            {
                return NotFound();
            }
            IUserDetail info;
            try
            {
                info = await provider.GetUserDetail(model.Code, true);
            }
            catch (AiurAPIModelException)
            {
                var refreshLink = provider.GetBindRedirectLink();
                return Redirect(refreshLink);
            }
            if (await _dbContext.ThirdPartyAccounts.AnyAsync(t => t.OpenId == info.Id))
            {
                // The third-party account already bind an account.
                return View(viewName: "BindFailed", model: new BindAccountViewModel
                {
                    UserDetail = info,
                    Provider = provider,
                    User = user
                });
            }
            var link = new ThirdPartyAccount
            {
                OwnerId = user.Id,
                OpenId = info.Id,
                ProviderName = provider.GetName(),
                Name = info.Name
            };
            await _dbContext.ThirdPartyAccounts.AddAsync(link);
            await _dbContext.SaveChangesAsync();
            // Complete
            var viewModel = new BindAccountViewModel
            {
                UserDetail = info,
                Provider = provider,
                User = user
            };
            return View(viewModel);
        }

        private Task<GatewayUser> GetCurrentUserAsync()
        {
            return _dbContext
                .Users
                .Include(t => t.ThirdPartyAccounts)
                .SingleOrDefaultAsync(t => t.UserName == User.Identity.Name);
        }
    }
}
