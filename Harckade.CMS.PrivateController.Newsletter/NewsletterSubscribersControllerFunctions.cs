using System.Net;
using System.Text;
using System.Web;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.NewsletterController;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Harckade.CMS.PrivateController.Newsletter
{
    [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Administrator })]
    public class NewsletterSubscribersControllerFunctions: BaseController
    {
     
        private INewsletterSubscriberService _newsletterSubscriberService;
        private IDtoNewsletterSubscriberMapper _dtoNewsletterSubscriberMapper;
        private ILogger<NewsletterSubscribersControllerFunctions> _appInsights;
        private IJournalService _journalService;
        private ObservabilityId _oid;

        public NewsletterSubscribersControllerFunctions(INewsletterSubscriberService newsletterSubscriberService, IDtoNewsletterSubscriberMapper dtoNewsletterSubscriberMapper, ILogger<NewsletterSubscribersControllerFunctions> appInsights, IJournalService journalService)
        {
            _oid = new ObservabilityId();
            _newsletterSubscriberService = newsletterSubscriberService;
            _newsletterSubscriberService.UpdateOid(_oid);

            _journalService = journalService;
            _journalService.UpdateOid(_oid);
            _dtoNewsletterSubscriberMapper = dtoNewsletterSubscriberMapper;
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
                _appInsights.LogError($"CMS: NewsletterSubscribersControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Authorize(Scopes = new[] { Scopes.Api }, UserRoles = new[] { UserRoles.Viewer, UserRoles.Editor, UserRoles.Administrator })]
        [Function("ListAllSubscribers")]
        public async Task<HttpResponseData> ListAllSubscribers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/subscribers")] HttpRequestData req)
        {
            _appInsights.LogInformation("CMS: Function ListAllSubscribers executed", _oid);
            return await ExecuteMethod(async () =>
            {
                IEnumerable<NewsletterSubscriber> entries;
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
                    var result = await _newsletterSubscriberService.GetSubscribers();
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                    entries = result.Value;
                }
                else
                {
                    var result = await _newsletterSubscriberService.GetSubscribersByLanguage(language);
                    if (result.Failed)
                    {
                        return FailResponse(result, req);
                    }
                    entries = result.Value;
                }

                return JsonResponse.Get(entries.Select(e => _dtoNewsletterSubscriberMapper.DocumentToDto(e)).Distinct(), req);
            });
        }

        [Function("RemoveSubscriber")]
        public async Task<HttpResponseData> RemoveSubscriber([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/subscribers/{subscriberId:guid}")] HttpRequestData req, Guid subscriberId, FunctionContext context)
        {
            _appInsights.LogInformation($"CMS: Function RemoveSubscriber executed | SubscriberId: {subscriberId}", _oid);
            return await ExecuteMethod(async () =>
            {
                await _journalService.AddEntryToQueue(context, $"{subscriberId}");
                var result = await _newsletterSubscriberService.RemoveSubscriberById(subscriberId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                return req.CreateResponse(HttpStatusCode.OK);
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
