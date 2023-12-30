using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Microsoft.Azure.Functions.Worker;

namespace Harckade.CMS.Services.Abstractions
{
    public interface INewsletterService : IServiceBase
    {
        Task<Result<Newsletter>> AddOrUpdateNewsletter(FunctionContext context, NewsletterDto tmpNewsletter, Language lang = default);
        Task<Result> UploadBinary(Newsletter doc, Stream binary, Language lang);
        Task<Result<Stream>> DownloadNewsletterBinary(Newsletter newsletter, Language lang);
        Task<Result<Stream>> DownloadNewsletterBinaryById(Guid newsletterId, Language lang);
        Task<Result<Newsletter>> GetById(Guid newsletterId);
        Task<Result<IEnumerable<Newsletter>>> GetAll();
        Task<Result> DeleteNewsletter(Newsletter newsletter);
        Task<Result> DeleteNewsletterById(Guid newsletterId);
        Task<Result> SendNewsletterToQueue(Guid newsletterId);
        Task ProcessEntryFromQueue(string message);
        Task<Result> SendNewsletter(Guid newsletterId, Language language, string to, Dictionary<string, string> replacementList);
    }
}
