using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Language = Harckade.CMS.Azure.Enums.Language;

namespace Harckade.CMS.Services
{
    public class NewsletterSubscriberService : ServiceBase, INewsletterSubscriberService
    {
        private INewsletterSubscriberRepository _newsletterSubscriberRepository;
        private INewsletterSubscriptionTemplateService _newsletterSubscriptionTemplateService;
        private IEmailService _emailService;
        private ISettingsRepository _settingsRepository;
        private QueueClient _queueClient;
        private readonly ILogger<NewsletterSubscriberService> _appInsights;
        private IConfiguration _configuration;

        public NewsletterSubscriberService(INewsletterSubscriberRepository newsletterSubscriberRepository, INewsletterSubscriptionTemplateService newsletterSubscriptionTemplateService, ISettingsRepository settingsRepository, IEmailService emailService, QueueClient queueClient, ILogger<NewsletterSubscriberService> appInsights, IConfiguration configuration)
        {
            _queueClient = queueClient;
            _queueClient.CreateIfNotExists();
            _newsletterSubscriberRepository = newsletterSubscriberRepository;
            _newsletterSubscriptionTemplateService = newsletterSubscriptionTemplateService;
            _settingsRepository = settingsRepository;
            _emailService = emailService;
            _appInsights = appInsights;
            _oidIsSet = false;
            _configuration = configuration;
        }

        public async Task<Result<NewsletterSubscriber>> AddSubscriber(string email, Language language = Language.None)
        {
            _appInsights.LogInformation($"AddSubscriber | email: {email}", _oid);
            if (string.IsNullOrWhiteSpace(email) || !Utils.Validations.IsValidEmail(email))
            {
                return Result.Fail<NewsletterSubscriber>(Failure.InvalidEmail);
            }
            var settings = await _settingsRepository.Get();
            if (language == Language.None || !settings.Languages.Contains(language))
            {
                return Result.Fail<NewsletterSubscriber>(Failure.UndefinedLanguage);
            }
            var retrieveSubscriber = await FindByEmail(email, language);
            if (retrieveSubscriber.Success)
            {
                return Result.Fail<NewsletterSubscriber>(Failure.UserAlreadyExists);
            }
            var subscriber = new NewsletterSubscriber(email, language);
            await _newsletterSubscriberRepository.InsertOrUpdateSubscriber(subscriber);
           
            var retrieveTemplate = await _newsletterSubscriptionTemplateService.GetNewsletterSubscriptionTemplate();
            if (!retrieveTemplate.Success)
            {
                return Result.Fail<NewsletterSubscriber>(retrieveTemplate.FailureReason);
            }
            var emailTemplate = retrieveTemplate.Value;

            var retrieveTemplateContent = await _newsletterSubscriptionTemplateService.GetNewsletterSubscriptionTemplateContent(language);
            if (!retrieveTemplateContent.Success)
            {
                return Result.Fail<NewsletterSubscriber>(retrieveTemplateContent.FailureReason);
            }
            var emailContent = Utils.Html.GenerateStringFromStream(retrieveTemplateContent.Value);
            emailContent = JsonConvert.DeserializeObject<string>(emailContent);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new NewsletterSubscriberDto
            {
                EmailAddress = email,
                PersonalToken = subscriber.PersonalToken
            }));
            var languageAsString = $"{language}".ToLower();
            var confirmationUrl = $"{_configuration["RedirectUrl"]}/{languageAsString}/confirm?token={Convert.ToBase64String(plainTextBytes)}";

            if (string.IsNullOrWhiteSpace(emailContent))
            {
                emailContent = @$"<html><body><p><strong>You subscribed to</strong> {_configuration["RedirectUrl"]}</p>
            <p><strong>Please confirm your subscription by clicking on the following link:</strong><a href='{confirmationUrl}'{confirmationUrl}</p>
            <p>Otherwise, please ignore this email</p>
            </body></html>";
            }
            else
            {
                emailContent = emailContent.Replace("{{confirmationUrl}}", confirmationUrl);
                emailContent = emailContent.Replace("{{emailAddress}}", subscriber.EmailAddress);
            }
            await SendConfirmationEmail(emailTemplate, subscriber.EmailAddress, subscriber.Language, emailContent);
            return Result.Ok<NewsletterSubscriber>(subscriber);
        }

        public async Task<Result<NewsletterSubscriber>> FindByDto(NewsletterSubscriberDto newsletterSubscriberDto)
        {
            _appInsights.LogInformation($"FindByDto | Id: {newsletterSubscriberDto.Id}", _oid);
            return await FindSubscriberById(newsletterSubscriberDto.Id);
        }

        public async Task<Result<NewsletterSubscriber>> FindByEmail(string email, Language language)
        {
            _appInsights.LogInformation($"FindByEmail | email: {email}", _oid);
            if (!Utils.Validations.IsValidEmail(email))
            {
                return Result.Fail<NewsletterSubscriber>(Failure.InvalidEmail);
            }
            if (language == Language.None)
            {
                return Result.Fail<NewsletterSubscriber>(Failure.UndefinedLanguage);
            }
            var subscriber = await _newsletterSubscriberRepository.FindByEmailAndLanguage(email, language);
            if (subscriber == null)
            {
                return Result.Fail<NewsletterSubscriber>(Failure.UserNotFound);
            }
            return Result.Ok<NewsletterSubscriber>(subscriber);
        }

        public async Task<Result<NewsletterSubscriber>> FindSubscriberById(Guid subscriberId)
        {
            _appInsights.LogInformation($"FindSubscriberById | Id: {subscriberId}", _oid);
            if (subscriberId == default)
            {
                return Result.Fail<NewsletterSubscriber>(Failure.InvalidInput);
            }
            var subscriber = await _newsletterSubscriberRepository.FindById(subscriberId);
            return Result.Ok<NewsletterSubscriber>(subscriber);
        }

        public async Task<Result<NewsletterSubscriber>> FindSubscriberByPersonalToken(string token)
        {
            _appInsights.LogInformation($"FindSubscriberByPersonalToken", _oid);
            if (!Utils.Validations.IsValidSHA512(token))
            {
                return Result.Fail<NewsletterSubscriber>(Failure.InvalidEmail);
            }
            var subscriber = await _newsletterSubscriberRepository.FindByPersonalToken(token);
            if (subscriber == null)
            {
                return Result.Fail<NewsletterSubscriber>(Failure.UserNotFound);
            }
            return Result.Ok<NewsletterSubscriber>(subscriber);
        }

        public async Task<Result<IEnumerable<NewsletterSubscriber>>> GetSubscribers()
        {
            _appInsights.LogInformation($"GetSubscribers", _oid);
            var subscribers = await _newsletterSubscriberRepository.GetAll();
            return Result.Ok<IEnumerable<NewsletterSubscriber>>(subscribers);
        }

        public async Task<Result<IEnumerable<NewsletterSubscriber>>> GetSubscribersByLanguage(Language language)
        {
            _appInsights.LogInformation($"GetSubscribersByLanguage | Language: {language}", _oid);
            if (language == Language.None)
            {
                return Result.Fail<IEnumerable<NewsletterSubscriber>>(Failure.UndefinedLanguage);
            }
            var subscribers = await _newsletterSubscriberRepository.GetAllByLanguage(language);
            return Result.Ok<IEnumerable<NewsletterSubscriber>>(subscribers);
        }

        public async Task<Result> RemoveSubscriberByDto(NewsletterSubscriberDto newsletterSubscriberDto)
        {
            _appInsights.LogInformation($"RemoveSubscriberByDto | Id: {newsletterSubscriberDto.Id}", _oid);
            return await RemoveSubscriberById(newsletterSubscriberDto.Id);
        }

        public async Task<Result> RemoveSubscriberByEmail(string email, Language language)
        {
            _appInsights.LogInformation($"FindByEmail | email: {email} | language: {language}", _oid);
            var subscriber = await FindByEmail(email, language);
             if (subscriber.Failed)
            {
                return Result.Fail(subscriber.FailureReason);
            }
            return await RemoveSubscriberById(subscriber.Value.Id);
        }

        public async Task<Result> RemoveSubscriberById(Guid subscriberId)
        {
            _appInsights.LogInformation($"RemoveSubscriberById | Id: {subscriberId}", _oid);
            var subscriber = await FindSubscriberById(subscriberId);
            if (subscriber.Failed)
            {
                return Result.Fail(subscriber.FailureReason);
            }
            await _newsletterSubscriberRepository.DeleteSubscriber(subscriberId);
            return Result.Ok();
        }

        public async Task<Result> ConfirmEmailAddress(NewsletterSubscriber subscriber)
        {
            _appInsights.LogInformation($"ConfirmEmailAddress | id: {subscriber.Id}", _oid);
            subscriber.UpdateConfirmed();
            await _newsletterSubscriberRepository.InsertOrUpdateSubscriber(subscriber);
            return Result.Ok();
        }

        public async Task<Result> SendConfirmationEmail(NewsletterSubscriptionTemplate template, string email, Language language, string content="")
        {
            _appInsights.LogInformation($"ConfirmEmailAddress | email: {email}", _oid);
            if (string.IsNullOrWhiteSpace(email) || !Utils.Validations.IsValidEmail(email))
            {
                return Result.Fail(Failure.InvalidEmail);
            }
            if (language == default)
            {
                return Result.Fail(Failure.UndefinedLanguage);
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result.Fail(Failure.EmailMessageIsEmpty);
            }

            var result = await _emailService.SendConfirmationEmailAsync(template, language, email, content);
            if (result.Failed)
            {
                return Result.Fail(result.FailureReason);
            }
            return Result.Ok();
        }
    }
}
