using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.FunctionsBase;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Web;

namespace Harckade.CMS.PublicController.Articles
{
    public class ArticlesControllerFunctions : BaseController
    {
        private IArticleService _articleService;
        private IDtoArticleMapper _dtoArticleMapper;
        private ILogger<ArticlesControllerFunctions> _appInsights;
        private ObservabilityId _oid;

        public ArticlesControllerFunctions(IArticleService articleService, IDtoArticleMapper dtoArticleMapper, ILogger<ArticlesControllerFunctions> appInsights)
        {
            _oid = new ObservabilityId();
            _articleService = articleService;
            _articleService.UpdateOid(_oid);
            _dtoArticleMapper = dtoArticleMapper;
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
                _appInsights.LogError($"CMS: ArticlesControllerFunctions Other exception: {e.InnerException} | {e.Message}", _oid);
                throw new ApplicationException("Internal Server Error");
            }
        }

        [Function("ListArticles")]
        public async Task<HttpResponseData> ListArticles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles")] HttpRequestData req)
        {
            _appInsights.LogInformation("Function ListArticles executed", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _articleService.GetAvailableArticles();
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var entries = result.Value;
                return JsonResponse.Get(entries.Select(e => _dtoArticleMapper.DocumentToDto(e)), req);
            });
        }

        [Function("GetPublishedArticleById")]
        public async Task<HttpResponseData> GetPublishedArticleById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{articleId:guid}")] HttpRequestData req, Guid articleId)
        {
            _appInsights.LogInformation($"Function GetPublishedArticleById executed. ArticleId: {articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _articleService.GetById(articleId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                else if (result.Value.Published == false)
                {
                    var response = req.CreateResponse(HttpStatusCode.NotFound);
                    return response;
                }
                return JsonResponse.Get(_dtoArticleMapper.DocumentToDto(result.Value), req);
            });
        }

        private async Task<HttpResponseData> AuxGetArticleContetById(HttpRequestData req, Azure.Domain.Article art)
        {
            string lang = "En";
            var queryDictionary = HttpUtility.ParseQueryString(req.Url.Query);
            if (queryDictionary["lang"] != null)
            {
                lang = queryDictionary["lang"];
                lang = lang.ToUpper();
                lang = lang.ElementAt(0) + lang.Substring(1).ToLower();
            }
            var result = await _articleService.DownloadArticleBinaryById(art.Id, (Language)Enum.Parse(typeof(Language), lang));
            if (result.Failed)
            {
                return FailResponse(result, req);
            }
            var article = result.Value;
            if (article == null)
            {
                var tmpString = new MemoryStream(Encoding.UTF8.GetBytes("\"\"" ?? ""));
                tmpString.Position = 0;
                article = tmpString;
            }
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Body = article;
            return response;
        }

        [Function("GetPublishedArticleContentById")]
        public async Task<HttpResponseData> GetPublishedArticleContentById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/{articleId:guid}/content")] HttpRequestData req, Guid articleId, FunctionContext executionContext)
        {
            _appInsights.LogInformation($"Function GetPublishedArticleContentById executed. ArticleId:{articleId}", _oid);
            return await ExecuteMethod(async () =>
            {
                var result = await _articleService.GetById(articleId);
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                if (result.Value.Published == false)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                return await AuxGetArticleContetById(req, result.Value);
            });
        }

        [Function("GetPublishedArticleByTitle")]
        public async Task<HttpResponseData> GetPublishedArticleByTitle([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/title/{lang}/{title}")] HttpRequestData req, string lang, string title)
        {
            _appInsights.LogInformation($"Function GetPublishedArticleByTitle executed. Title: {title + "_" + lang}", _oid);
            return await ExecuteMethod(async () =>
            {
                if (string.IsNullOrWhiteSpace(title.Trim()))
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                var result = await _articleService.GetByTitle(title, (Language)Enum.Parse(typeof(Language), lang, true));
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                var article = result.Value;
                if (article.Published == false)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                return JsonResponse.Get(_dtoArticleMapper.DocumentToDto(article), req);
            });
        }

        [Function("GetPublishedArticleContentByTitle")]
        public async Task<HttpResponseData> GetArticleContentByTitle([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/title/{lang}/{title}/content")] HttpRequestData req, string lang, string title, FunctionContext executionContext)
        {
            _appInsights.LogInformation($"Function GetPublishedArticleContentByTitle executed. Title: {title}", _oid);
            return await ExecuteMethod(async () =>
            {
                if (string.IsNullOrWhiteSpace(title.Trim()))
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                var result = await _articleService.GetByTitle(title, (Language)Enum.Parse(typeof(Language), lang, true));
                if (result.Failed)
                {
                    return FailResponse(result, req);
                }
                if (result.Value.Published == false)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
                return await AuxGetArticleContetById(req, result.Value);
            });
        }

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
