using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;

namespace Harckade.CMS.FunctionsBase
{
    public abstract class BaseController
    {
        protected HttpResponseData FailResponse(Result result, HttpRequestData req)
        {
            var httpCode = HttpStatusCode.BadRequest;
            string errorMessage = FailureMessage.GetString(result.FailureReason);

            if (result.FailureReason == Failure.DuplicateArticleTitle || result.FailureReason == Failure.UserAlreadyExists)
            {
                httpCode = HttpStatusCode.Conflict;
                errorMessage = result.Description;
            }
            else if (result.FailureReason == Failure.NotPossibleEditDeletedArticle || result.FailureReason == Failure.NotMarkedAsDeleted || result.FailureReason == Failure.AdministratorRequired)
            {
                httpCode = HttpStatusCode.Locked;
            }
            else if (result.FailureReason == Failure.ArticleNotFound || result.FailureReason == Failure.BackupNotFound || result.FailureReason == Failure.FolderNotFound || result.FailureReason == Failure.NewsletterNotFound || result.FailureReason == Failure.UserNotFound)
            {
                httpCode = HttpStatusCode.NotFound;
            }
            else if (result.FailureReason == Failure.DeploymentLaunchFailed || result.FailureReason == Failure.FolderNotInitialized)
            {
                httpCode = HttpStatusCode.FailedDependency;
            }
            return FailureResponde(req, errorMessage, httpCode);
        }

        private static HttpResponseData FailureResponde(HttpRequestData req, string errorMessage = "", HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = req.CreateResponse(statusCode);
            response.Body = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
            return response;
        }
    }
}