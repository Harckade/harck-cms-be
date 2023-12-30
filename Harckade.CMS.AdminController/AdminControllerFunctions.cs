using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Web;

namespace Harckade.CMS.AdminController
{
    [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Administrator })]
    public class AdminControllerFunctions : BaseController
    {
        private readonly IAdminService _adminService;
        private readonly IJournalService _journalService;
        private readonly IDtoUserMapper _dtoUserMapper;
        private readonly IDtoSettingsMapper _dtoSettingsMapper;
        private readonly IDtoJournalMapper _dtoJournalMapper;
        private readonly ILogger<AdminControllerFunctions> _appInsights;
        private ObservabilityId _oid;

        public AdminControllerFunctions(IAdminService adminService, IJournalService journalService, IDtoUserMapper dtoUserMapper, IDtoSettingsMapper dtoSettingsMapper, IDtoJournalMapper dtoJournalMapper, ILogger<AdminControllerFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _adminService = adminService;
            _adminService.UpdateOid(_oid);
            _journalService = journalService;
            _journalService.UpdateOid(_oid);
            _dtoUserMapper = dtoUserMapper;
            _dtoSettingsMapper = dtoSettingsMapper;
            _dtoJournalMapper = dtoJournalMapper;
            _appInsights = appInsights;
        }

        private async Task<HttpResponseData> ExecuteMethod(Func<Task<HttpResponseData>> func)
        {
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                _appInsights.LogError($"CMS: AdminControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Function("ListUsers")]
        public async Task<HttpResponseData> GetUsers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/users")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function GetUsers executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _adminService.GetUsers();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                return JsonResponse.Get(entries.Select(e => _dtoUserMapper.DocumentToDto(e)), req);
            });
        }

        [Function("InviteUser")]
        public async Task<HttpResponseData> InviteUser([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/users")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function InviteUser executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                UserDto tmpUser = JsonConvert.DeserializeObject<UserDto>(body);
                await _journalService.AddEntryToQueue(context, $"{tmpUser.Email}");
                var result = await _adminService.InviteUser(_dtoUserMapper.DtoToDocument(tmpUser));
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var user = result.Value;
                return JsonResponse.Get(_dtoUserMapper.DocumentToDto(user), req);
            });
        }

        [Function("EditUser")]
        public async Task<HttpResponseData> EditUser([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "cms/users")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function EditUser executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                UserDto tmpUser = JsonConvert.DeserializeObject<UserDto>(body);
                await _journalService.AddEntryToQueue(context, $"{tmpUser.Email} | {tmpUser.Id} | {tmpUser.Role}");
                var result = await _adminService.EditUser(_dtoUserMapper.DtoToDocument(tmpUser));
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var user = result.Value;
                return JsonResponse.Get(_dtoUserMapper.DocumentToDto(user), req);
            });
        }

        [Function("DeleteUser")]
        public async Task<HttpResponseData> DeleteUser([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/users/{userId:guid}")] HttpRequestData req, Guid userId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function DeleteUser executed: {userId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{userId}");
                var result = await _adminService.DeleteUserById(userId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("GetJournal")]
        public async Task<HttpResponseData> GetJournal([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/journal")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function GetJournal executed", _oid);
            return await ExecuteMethod(async () =>
            {
                DateTimeOffset startDate = default;
                DateTimeOffset endDate = default;
                var queryDictionary = HttpUtility.ParseQueryString(req.Url.Query);
                if (queryDictionary["startDate"] != null && queryDictionary["endDate"] != null)
                {
                    startDate = DateTime.Parse(queryDictionary["startDate"]);
                    endDate = DateTime.Parse(queryDictionary["endDate"]);
                }
                var result = await _journalService.GetEntries(startDate, endDate);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                return JsonResponse.Get(entries.Select(e => _dtoJournalMapper.DocumentToDto(e)), req);
            });
        }

        [Function("UpdateSettings")]
        public async Task<HttpResponseData> UpdateSettings([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/settings")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function UpdateSettings executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context);
                string body = new StreamReader(req.Body).ReadToEnd();
                SettingsDto tmpSettings = JsonConvert.DeserializeObject<SettingsDto>(body);

                var result = await _adminService.UpdateSettings(_dtoSettingsMapper.DtoToDocument(tmpSettings));
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return JsonResponse.Get(tmpSettings, req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetSettings")]
        public async Task<HttpResponseData> GetSettings([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/settings")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function GetSettings executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _adminService.GetSettings();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var settings = result.Value;
                return JsonResponse.Get(_dtoSettingsMapper.DocumentToDto(settings), req);
            });
        }

        [Authorize(IsPublic = true)]
        [Function("GetLanguages")]
        public async Task<HttpResponseData> GetLanguages([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "languages")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function GetLanguages executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _adminService.GetSettings();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var settings = result.Value;
                return JsonResponse.Get(_dtoSettingsMapper.DocumentToDto(settings).Languages, req);
            });
        }

        [Authorize(IsPublic = true)]
        [Function("GetDefaultLanguage")]
        public async Task<HttpResponseData> GetDefaultLanguage([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "languages/default")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function GetDefaultLanguage executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _adminService.GetSettings();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var settings = result.Value;
                return JsonResponse.Get(_dtoSettingsMapper.DocumentToDto(settings).DefaultLanguage, req);
            });
        }

        [Authorize(IsPublic = true)]
        [Function("RobotsTxt")]
        public async Task<string> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "robots.txt")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function GetRobotsTxt executed", _oid);
            var sb = new StringBuilder();
            sb.AppendLine("user-agent: *");
            sb.AppendLine("Disallow: /");
            return await Task.FromResult(sb.ToString());
        }
    }
}
