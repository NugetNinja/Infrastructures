using Aiursoft.Developer.Data;
using Aiursoft.Developer.SDK.Models.ApiAddressModels;
using Aiursoft.Developer.SDK.Models.ApiViewModels;
using Aiursoft.DocGenerator.Attributes;
using Aiursoft.Handler.Attributes;
using Aiursoft.Handler.Models;
using Aiursoft.WebTools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Aiursoft.Developer.Controllers
{
    [APIExpHandler]
    [APIModelStateChecker]
    [LimitPerMin]
    public class ApiController : ControllerBase
    {
        private readonly DeveloperDbContext _dbContext;

        public ApiController(
            DeveloperDbContext context)
        {
            _dbContext = context;
        }

        public IActionResult Error()
        {
            throw new Exception("This is a test API error for debugging.");
        }

        public async Task<IActionResult> IsValidApp(IsValidateAppAddressModel model)
        {
            var target = await _dbContext.Apps.FindAsync(model.AppId);
            if (target == null)
            {
                return this.Protocol(new AiurProtocol { Message = "Target app did not found.", Code = ErrorType.NotFound });
            }
            else if (target.AppSecret != model.AppSecret)
            {
                return this.Protocol(new AiurProtocol { Message = "Wrong secret.", Code = ErrorType.WrongKey });
            }
            else
            {
                return this.Protocol(new AiurProtocol { Message = "Correct app info.", Code = ErrorType.Success });
            }
        }

        [APIProduces(typeof(AppInfoViewModel))]
        public async Task<IActionResult> AppInfo(AppInfoAddressModel model)
        {
            var target = await _dbContext
                .Apps
                .FirstOrDefaultAsync(t => t.AppId == model.AppId);

            if (target == null)
            {
                return this.Protocol(new AiurProtocol { Message = $"Could find target app with appId: '{model.AppId}'!", Code = ErrorType.NotFound });
            }
            return this.Protocol(new AppInfoViewModel
            {
                Message = "Successfully get target app info.",
                Code = ErrorType.Success,
                App = target
            });
        }
    }
}