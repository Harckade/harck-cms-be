using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Harckade.CMS.Services
{
    public class NewsletterService : ServiceBase, INewsletterService
    {
        private INewsletterRepository _newsletterRepository;
        private IFileService _fileService;
        private IBlobRepository _blobRepository;
        private INewsletterSubscriberService _newsletterSubscriberService;
        private IEmailService _emailService;
        private readonly ILogger<NewsletterService> _appInsights;
        private QueueClient _queueClient;
        private IConfiguration _configuration;

        public NewsletterService(INewsletterRepository newsletterRepository, INewsletterSubscriberService newsletterSubscriberService, QueueClient queueClient, IEmailService emailService, IFileService fileService, IBlobRepository blobRepository, ILogger<NewsletterService> appInsights, IConfiguration configuration)
        {
            _newsletterRepository = newsletterRepository;
            _fileService = fileService;
            _blobRepository = blobRepository;
            _newsletterSubscriberService = newsletterSubscriberService;
            _emailService = emailService;
            _queueClient = queueClient;
            _queueClient.CreateIfNotExists();
            _appInsights = appInsights;
            _oidIsSet = false;
            _configuration = configuration;
        }

        public async Task<Result> DeleteNewsletter(Newsletter newsletter)
        {
            _appInsights.LogDebug($"DeleteNewsletter: {newsletter.Id}", _oid);
            return await DeleteNewsletterById(newsletter.Id);
        }

        public async Task<Result> DeleteNewsletterById(Guid newsletterId)
        {
            _appInsights.LogDebug($"DeleteNewsletterById: {newsletterId}", _oid);
            var tmpNewsletter = await GetById(newsletterId);
            if (!tmpNewsletter.Success)
            {
                return Result.Fail(Failure.NewsletterNotFound);
            }
            var newsletter = tmpNewsletter.Value;

            var contetExistForLanguageList = newsletter.ContentHash.Where(hash => !string.IsNullOrWhiteSpace(hash.Value)).Select(hash => hash.Key);
            foreach (var lang in contetExistForLanguageList)
            {
                await _fileService.DeleteFileById(new BlobId($"newsletter_{newsletter.Id}_{lang}"));
            }
            await _newsletterRepository.DeleteNewsletter(newsletterId);
            return Result.Ok();
        }

        public async Task<Result<Stream>> DownloadNewsletterBinary(Newsletter newsletter, Language lang)
        {
            _appInsights.LogDebug($"DownloadNewsletterBinary: {newsletter.Id} | {lang}", _oid);
            return await DownloadNewsletterBinaryById(newsletter.Id, lang);
        }

        public async Task<Result<Stream>> DownloadNewsletterBinaryById(Guid newsletterId, Language lang)
        {
            _appInsights.LogDebug($"DownloadNewsletterBinaryById: {newsletterId} | {lang}", _oid);
            if (newsletterId == default || lang == default)
            {
                return Result.Fail<Stream>(Failure.InvalidInput);
            }
            return Result.Ok(await _blobRepository.DownloadFileAsync(new BlobId($"newsletter_{newsletterId}_{lang}")));
        }

        public async Task<Result<IEnumerable<Newsletter>>> GetAll()
        {
            _appInsights.LogDebug($"GetAll", _oid);
            var entries = await _newsletterRepository.GetAll();
            return Result.Ok<IEnumerable<Newsletter>>(entries.OrderByDescending(a => a.Timestamp));
        }

        public async Task<Result<Newsletter>> GetById(Guid newsletterId)
        {
            _appInsights.LogDebug($"GetById: {newsletterId}", _oid);
            if (newsletterId == default)
            {
                return Result.Fail<Newsletter>(Failure.InvalidInput, nameof(newsletterId));

            }
            var newsletter = await _newsletterRepository.FindById(newsletterId);
            if (newsletter == null)
            {
                return Result.Fail<Newsletter>(Failure.NewsletterNotFound);
            }
            return Result.Ok(newsletter);
        }

        public async Task<Result> SendNewsletterToQueue(Guid newsletterId)
        {
            _appInsights.LogDebug($"SendNewsletterToQueue: {newsletterId}", _oid);
            var newsletter = await _newsletterRepository.FindById(newsletterId);
            if (newsletter == null)
            {
                return Result.Fail<Newsletter>(Failure.NewsletterNotFound);
            }
            var contetExistForLanguageList = newsletter.ContentHash.Where(hash => !string.IsNullOrWhiteSpace(hash.Value)).Select(hash => hash.Key);
            foreach (var lang in contetExistForLanguageList)
            {
                if (string.IsNullOrWhiteSpace(newsletter.Name[lang]))
                {
                    continue;
                }
                var newsletterContent = await _blobRepository.DownloadFileAsync(new BlobId($"newsletter_{newsletterId}_{lang}"));
                if (newsletterContent.Length == 0)
                {
                    continue;
                }
                var getSubscribers = await _newsletterSubscriberService.GetSubscribersByLanguage(lang);
                if (getSubscribers.Failed)
                {
                    return Result.Fail<Newsletter>(getSubscribers.FailureReason);
                }
                var subscribers = getSubscribers.Value;
                if (subscribers.Any())
                {
                    foreach (var subscriber in subscribers.Where(sub => sub.Confirmed == true))
                    {
                        var entry = new NewsletterEntryQueue(newsletterId, lang, subscriber.EmailAddress);
                        var serializedEntry = JsonConvert.SerializeObject(entry);
                        var encodedEntry = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedEntry));
                        await _queueClient.SendMessageAsync(encodedEntry);
                    }
                }
            }
            newsletter.UpdateSendDate();
            await _newsletterRepository.InsertOrUpdateNewsletter(newsletter);
            return Result.Ok();
        }

        public async Task ProcessEntryFromQueue(string message)
        {
            _appInsights.LogDebug($"JournalService | FromQueueToStorage", _oid);
            var newsletter = JsonConvert.DeserializeObject<NewsletterEntryQueue>(message);
            var subscriber = await _newsletterSubscriberService.FindByEmail(newsletter.EmailTo, newsletter.Language);
            if (subscriber.Success)
            {
                var plainTextBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new NewsletterSubscriberDto
                {
                    EmailAddress = subscriber.Value.EmailAddress,
                    PersonalToken = subscriber.Value.PersonalToken
                }));
                var languageWithCamelCase = $"{newsletter.Language}";
                languageWithCamelCase = Char.ToLowerInvariant(languageWithCamelCase[0]) + languageWithCamelCase.Substring(1);
                var unsubscribeUrl = $"{_configuration["RedirectUrl"]}/{languageWithCamelCase}/unsubscribe?token={Convert.ToBase64String(plainTextBytes)}";

                var replacementList = new Dictionary<string, string>();
                replacementList.Add("{{email}}", subscriber.Value.EmailAddress);
                replacementList.Add("{{unsubscribeUrl}}", $"{unsubscribeUrl}");

                await SendNewsletter(newsletter.NewsletterId, newsletter.Language, subscriber.Value.EmailAddress, replacementList);
            }
        }

        public async Task<Result> SendNewsletter(Guid newsletterId, Language language, string to, Dictionary<string, string> replacementList)
        {
            _appInsights.LogDebug($"SendNewsletter: {newsletterId}", _oid);
            var newsletter = await _newsletterRepository.FindById(newsletterId);
            if (newsletter == null)
            {
                return Result.Fail<Newsletter>(Failure.NewsletterNotFound);
            }
            var newsletterContent = await _blobRepository.DownloadFileAsync(new BlobId($"newsletter_{newsletterId}_{language}"));
            var contentString = Utils.Html.GenerateStringFromStream(newsletterContent);

            foreach (var word in replacementList)
            {
                contentString = contentString.Replace($"{word.Key}", word.Value);
            }
            //Taken from: https://stackoverflow.com/questions/16692371/replacing-escape-characters-from-json
            contentString = JsonConvert.DeserializeObject<string>(contentString);
            if (string.IsNullOrWhiteSpace(contentString))
            {
                return Result.Fail(Failure.EmailMessageIsEmpty);
            }
            if (!contentString.StartsWith("<html>"))
            {
                contentString = @$"<html>{contentString}</html>";
            }
            return await _emailService.SendNewsletterEmailAsync(newsletter, language, contentString, to);
        }

        public async Task<Result> UploadBinary(Newsletter newsletter, Stream binary, Language lang)
        {
            _appInsights.LogDebug($"UploadBinary: {newsletter.Id} {lang}", _oid);
            if (newsletter.Id == default || lang == default)
            {
                return Result.Fail(Failure.InvalidInput);
            }
            await _blobRepository.UploadBinary(new BlobId($"newsletter_{newsletter.Id}_{lang}"), binary);
            return Result.Ok();
        }

        public async Task<Result<Newsletter>> AddOrUpdateNewsletter(FunctionContext context, NewsletterDto tmpNewsletter, Language lang = Language.None)
        {
            _appInsights.LogInformation($"AddOrUpdateNewsletter | newsletter: {tmpNewsletter.Id}", _oid);
            bool isNewNewsletter = false;
            Newsletter newsletter;

            if (tmpNewsletter.Id != default)
            {
                var retrievedNewsletter = await GetById(tmpNewsletter.Id);
                if (retrievedNewsletter.Failed)
                {
                    return Result.Fail<Newsletter>(retrievedNewsletter.FailureReason);
                }
                newsletter = retrievedNewsletter.Value;
                if (newsletter.SendDate != default && newsletter.SendDate.Year != 1601)
                {
                    return Result.Fail<Newsletter>(Failure.NewsletterAlreadySent);
                }
                if (lang == default)
                {
                    return Result.Fail<Newsletter>(Failure.UndefinedLanguage);
                }
            }
            else
            {
                isNewNewsletter = true;
                newsletter = new Newsletter(tmpNewsletter);
            }

            if (!isNewNewsletter)
            {
                if (newsletter.Name[lang] != tmpNewsletter.Name[lang])
                {
                    newsletter.UpdateNameForLanguage(tmpNewsletter.Name[lang], lang);
                }
                if (newsletter.Author[lang] != tmpNewsletter.Author[lang])
                {
                    newsletter.UpdateAuthorForLanguage(tmpNewsletter.Author[lang], lang);
                }

                string htmlCode = tmpNewsletter.HtmlContent == null || string.IsNullOrWhiteSpace(tmpNewsletter.HtmlContent[lang]) ? string.Empty : JsonConvert.SerializeObject(tmpNewsletter.HtmlContent[lang]);
                var newsletterHash = Utils.Hash.Sha256(htmlCode);
                if (newsletter.ContentHash == null || !newsletter.ContentHash.Any() || newsletterHash != newsletter.ContentHash[lang])
                {
                    using (var stream = Utils.Html.GenerateStreamFromString(htmlCode))
                    {
                        await UploadBinary(newsletter, stream, lang);
                    }
                }
                newsletter.UpdateContentHashForLanguage(newsletterHash, lang);
            }

            await _newsletterRepository.InsertOrUpdateNewsletter(newsletter);
            return Result.Ok<Newsletter>(newsletter);
        }
    }
}
