using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Microsoft.Azure.Functions.Worker;

namespace Harckade.CMS.Services.Abstractions
{
    public interface INewsletterSubscriptionTemplateService: IServiceBase
    {
        Task<Result<NewsletterSubscriptionTemplate>> GetNewsletterSubscriptionTemplate();
        Task<Result<Stream>> GetNewsletterSubscriptionTemplateContent(Language language);

        Task<Result<NewsletterSubscriptionTemplate>> AddOrUpdateNewsletterSubscriptionTemplate(FunctionContext context, NewsletterSubscriptionTemplateDto tmpNewsletterTemplate, Language language = default);
        Task<Result> UploadBinary(NewsletterSubscriptionTemplate newsletter, Stream binary, Language language);
    }
}
