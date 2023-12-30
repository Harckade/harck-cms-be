using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Harckade.CMS.Services
{
    public class NewsletterSubscriptionTemplateService : ServiceBase, INewsletterSubscriptionTemplateService
    {
        private IBlobRepository _blobRepository;
        private INewsletterSubscriptionTemplateRepository _newsletterSubscriptionTemplateRepository;
        private readonly ILogger<NewsletterSubscriptionTemplateService> _appInsights;
        public NewsletterSubscriptionTemplateService(INewsletterSubscriptionTemplateRepository newsletterSubscriptionTemplateRepository, IBlobRepository blobRepository, ILogger<NewsletterSubscriptionTemplateService> appInsights)
        {
            _newsletterSubscriptionTemplateRepository = newsletterSubscriptionTemplateRepository;
            _blobRepository = blobRepository;
            _appInsights = appInsights;
            _oidIsSet = false;
        }
        public async Task<Result<NewsletterSubscriptionTemplate>> AddOrUpdateNewsletterSubscriptionTemplate(FunctionContext context, NewsletterSubscriptionTemplateDto tmpNewsletterTemplate, Language language = Language.None)
        {
            _appInsights.LogInformation($"AddOrUpdateNewsletterSubscriptionTemplate", _oid);
            if (language == default)
            {
                return Result.Fail<NewsletterSubscriptionTemplate>(Failure.UndefinedLanguage);
            }
            NewsletterSubscriptionTemplate newsletter;
            var retrievedNewsletterTemplate = await GetNewsletterSubscriptionTemplate();
            if (retrievedNewsletterTemplate.Failed && retrievedNewsletterTemplate.FailureReason == Failure.NewsletterNotFound)
            {
                newsletter = new NewsletterSubscriptionTemplate(tmpNewsletterTemplate);
            }
            else
            {
                newsletter = retrievedNewsletterTemplate.Value;
            }


            if (newsletter.Author == null || newsletter.Author[language] != tmpNewsletterTemplate.Author[language])
            {
                newsletter.UpdateAuthorForLanguage(tmpNewsletterTemplate.Author[language], language);
            }
            if (newsletter.Subject == null || newsletter.Subject[language] != tmpNewsletterTemplate.Subject[language])
            {
                newsletter.UpdateSubjectForLanguage(tmpNewsletterTemplate.Subject[language], language);
            }

            string htmlCode = tmpNewsletterTemplate.HtmlContent == null || string.IsNullOrWhiteSpace(tmpNewsletterTemplate.HtmlContent[language]) ? string.Empty : JsonConvert.SerializeObject(tmpNewsletterTemplate.HtmlContent[language]);
            if (htmlCode != null && !string.IsNullOrWhiteSpace(htmlCode))
            {
                if (!htmlCode.Contains("{{confirmationUrl}}"))
                {
                    return Result.Fail<NewsletterSubscriptionTemplate>(Failure.ConfirmationUrlIsMissing);
                }
                using (var stream = Utils.Html.GenerateStreamFromString(htmlCode))
                {
                    await UploadBinary(newsletter, stream, language);
                }
            }
            await _newsletterSubscriptionTemplateRepository.InsertOrUpdateTemplate(newsletter);
            return Result.Ok<NewsletterSubscriptionTemplate>(newsletter);
        }

        public async Task<Result<NewsletterSubscriptionTemplate>> GetNewsletterSubscriptionTemplate()
        {
            var newsletter = await _newsletterSubscriptionTemplateRepository.GetTemplate();
            if (newsletter == null)
            {
                newsletter = new NewsletterSubscriptionTemplate();
                await _newsletterSubscriptionTemplateRepository.InsertOrUpdateTemplate(newsletter);
            }
            return Result.Ok(newsletter);
        }

        public async Task<Result<Stream>> GetNewsletterSubscriptionTemplateContent(Language language)
        {
            _appInsights.LogDebug($"GetNewsletterSubscriptionTemplateContent | {language}", _oid);
            if (language == default)
            {
                return Result.Fail<Stream>(Failure.InvalidInput);
            }
            return Result.Ok(await _blobRepository.DownloadFileAsync(new BlobId($"newsletter_subscription_confirmation_{language}")));
        }

        public async Task<Result> UploadBinary(NewsletterSubscriptionTemplate newsletter, Stream binary, Language language)
        {
            _appInsights.LogDebug($"NewsletterSubscriptionTemplateService - UploadBinary: {language}", _oid);
            if (language == default)
            {
                return Result.Fail(Failure.InvalidInput);
            }
            await _blobRepository.UploadBinary(new BlobId($"newsletter_subscription_confirmation_{language}"), binary);
            return Result.Ok();
        }
    }
}
