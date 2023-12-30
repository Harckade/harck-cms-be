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

namespace Harckade.CMS.NewsletterController
{
    [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Administrator })]
    public class NewsletterControllerFunctions : BaseController
    {
        private INewsletterService _newsletterService;
        private IDtoNewsletterMapper _dtoNewsletterMapper;
        private ILogger<NewsletterControllerFunctions> _appInsights;
        private IJournalService _journalService;
        private ObservabilityId _oid;

        public NewsletterControllerFunctions(INewsletterService newsletterService, IDtoNewsletterMapper dtoNewsletterMapper, ILogger<NewsletterControllerFunctions> appInsights, IJournalService journalService)
        {
            _oid = new ObservabilityId();
            _newsletterService = newsletterService;
            _newsletterService.UpdateOid(_oid);
            _journalService = journalService;
            _journalService.UpdateOid(_oid);
            _dtoNewsletterMapper = dtoNewsletterMapper;
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

        [Function("AddUpdateNewsletter")]
        public async Task<HttpResponseData> AddUpdateNewsletter([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cms/newsletters")] HttpRequestData req, FunctionContext context)
        {
            _appInsights.LogInformation("CMS: Function AddUpdateNewsletter executed", _oid);
            return await ExecuteMethod(async () =>
            {
                string body = new StreamReader(req.Body).ReadToEnd();
                Azure.Dtos.NewsletterDto tmpNewsletter = JsonConvert.DeserializeObject<Azure.Dtos.NewsletterDto>(body);

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
                if (language == default)
                {
                    await _journalService.AddEntryToQueue(context, $"create new newsletter");
                }
                else
                {
                    await _journalService.AddEntryToQueue(context, $"{tmpNewsletter.Id} | {tmpNewsletter.Name[language]} | ${language}");
                }
                var result = await _newsletterService.AddOrUpdateNewsletter(context, tmpNewsletter, language);
                if (result.Success)
                {
                    return JsonResponse.Get(_dtoNewsletterMapper.DocumentToDto(result.Value), req);
                }
                return FailResponse(result, req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("ListAllNewsletters")]
        public async Task<HttpResponseData> ListAllNewsletters([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/newsletters")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function ListAllNewsletters executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _newsletterService.GetAll();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                return JsonResponse.Get(entries.Select(e => _dtoNewsletterMapper.DocumentToDto(e)), req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetNewsletterById")]
        public async Task<HttpResponseData> GetNewsletterById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/newsletters/{newsletterId:guid}")] HttpRequestData req, Guid newsletterId)
        {
            _appInsights.LogInformation($"CMS: Function GetNewsletterById executed. NewsletterId: {newsletterId}", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _newsletterService.GetById(newsletterId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return JsonResponse.Get(_dtoNewsletterMapper.DocumentToDto(result.Value), req);
            });
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("GetNewsletterContentById")]
        public async Task<HttpResponseData> GetNewsletterContentById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/newsletters/{newsletterId:guid}/content")] HttpRequestData req, Guid newsletterId)
        {
            _appInsights.LogInformation($"CMS: Function GetNewsletterContentById executed. NewsletterId: {newsletterId}", _oid);
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

                var result = await _newsletterService.DownloadNewsletterBinaryById(newsletterId, language);
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

        [Function("DeleteNewsletterById")]
        public async Task<HttpResponseData> DeleteNewsletterById([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/newsletters/{newsletterId:guid}")] HttpRequestData req, Guid newsletterId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function DeleteNewsletterById executed. NewsletterId: {newsletterId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{newsletterId}");
                var result = await _newsletterService.DeleteNewsletterById(newsletterId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }

        [Function("SendNewsletterToQueue")]
        public async Task<HttpResponseData> SendNewsletterToQueue([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/newsletters/{newsletterId:guid}/send")] HttpRequestData req, Guid newsletterId)
        {
            _appInsights.LogInformation("CMS: Function ListAllNewsletters executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _newsletterService.SendNewsletterToQueue(newsletterId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
            });
        }
    }
}
