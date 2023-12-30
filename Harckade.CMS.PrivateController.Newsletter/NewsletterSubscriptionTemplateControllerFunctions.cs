using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace Harckade.CMS.PrivateController.Newsletter
{
    [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Administrator })]
    public class NewsletterSubscriptionTemplateControllerFunctions : BaseController
    {

        private INewsletterSubscriptionTemplateService _newsletterSubscriptionTemplateService;
        private IDtoNewsletterSubscriptionTemplateMapper _dtoNewsletterSubscriptionTemplateMapper;
        private ILogger<NewsletterSubscriptionTemplateControllerFunctions> _appInsights;
        private IJournalService _journalService;
        private ObservabilityId _oid;

        public NewsletterSubscriptionTemplateControllerFunctions(INewsletterSubscriptionTemplateService newsletterSubscriptionTemplateService, IDtoNewsletterSubscriptionTemplateMapper dtoNewsletterSubscriptionTemplateMapper, ILogger<NewsletterSubscriptionTemplateControllerFunctions> appInsights, IJournalService journalService)
        {
            _oid = new ObservabilityId();
            _journalService = journalService;
            _journalService.UpdateOid(_oid);
            _newsletterSubscriptionTemplateService = newsletterSubscriptionTemplateService;
            _newsletterSubscriptionTemplateService.UpdateOid(_oid);
            _dtoNewsletterSubscriptionTemplateMapper = dtoNewsletterSubscriptionTemplateMapper;
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
                _appInsights.LogError($"CMS: NewsletterControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Function("AddOrUpdateNewsletterSubscriptionTemplate")]
        public async Task<HttpResponseData> AddOrUpdateNewsletterSubscriptionTemplate([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cms/subscription-template")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function AddOrUpdateNewsletterSubscriptionTemplate executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                Azure.Dtos.NewsletterSubscriptionTemplateDto tmpNewsletter = JsonConvert.DeserializeObject<Azure.Dtos.NewsletterSubscriptionTemplateDto>(body);

                string lang = string.Empty;
                Language language = default;
                var queryDictionary = HttpUtility.ParseQueryString(req.Url.Query);
                if (queryDictionary["lang"] != null)
                {
                    lang = queryDictionary["lang"];
                    lang = lang.ToUpper();
                    lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                    Enum.TryParse(lang, true, out language);
                }
                await _journalService.AddEntryToQueue(context, $"update new subscription template  | ${language}");

                var result = await _newsletterSubscriptionTemplateService.AddOrUpdateNewsletterSubscriptionTemplate(context, tmpNewsletter, language);
                if (result.Success)
                {
                    return JsonResponse.Get(_dtoNewsletterSubscriptionTemplateMapper.DocumentToDto(result.Value), req);
                }
                return FailResponse(result, req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetNewsletterSubscriptionTemplate")]
        public async Task<HttpResponseData> GetNewsletterSubscriptionTemplate([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/subscription-template")] HttpRequestData req)
        {
            _appInsights.LogInformation($"CMS: Function GetNewsletterSubscriptionTemplate executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _newsletterSubscriptionTemplateService.GetNewsletterSubscriptionTemplate();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return JsonResponse.Get(_dtoNewsletterSubscriptionTemplateMapper.DocumentToDto(result.Value), req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetNewsletterContent")]
        public async Task<HttpResponseData> GetNewsletterSubscriptionTemplateContent([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/subscription-template/content")] HttpRequestData req)
        {
            _appInsights.LogInformation($"CMS: Function GetNewsletterSubscriptionTemplateContent executed.", _oid);
            return await ExecuteMethod(async () =>
            {
                string lang = string.Empty;
                Language language;
                var queryDictionary = HttpUtility.ParseQueryString(req.Url.Query);
                if (queryDictionary["lang"] != null)
                {
                    lang = queryDictionary["lang"];
                    lang = lang.ToUpper();
                    lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
                }
                if (string.IsNullOrWhiteSpace(lang) || !Enum.TryParse(lang, true, out language))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                var result = await _newsletterSubscriptionTemplateService.GetNewsletterSubscriptionTemplateContent(language);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var newsletter = result.Value;
                if (newsletter == null)
                {
                    return JsonResponse.Get("", req);
                }
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Body = newsletter;
                return response;
            });
        }
    }
}
