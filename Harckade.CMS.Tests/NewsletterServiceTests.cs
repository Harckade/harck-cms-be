using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;


namespace Harckade.CMS.Tests
{
    [TestClass]
    public class NewsletterServiceTests
    {
        private INewsletterService _newsletterService;
        private INewsletterSubscriberService _newsletterSubscriberService;
        private FunctionContext _functionContext;
        private string _prefix;
        private IDtoNewsletterMapper _dtoNewsletterMapper;

        [TestInitialize]
        public void init()
        {
            var connectionString = "UseDevelopmentStorage=true";
            var services = new ServiceCollection();
            var path = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            var configurationFile = System.IO.File.ReadAllText(path);
            var fileJson = JsonConvert.DeserializeObject<LocalSettings>(configurationFile);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(fileJson.Values).Build();
            var queueClient = new QueueClient(connectionString, "newsletter");
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(queueClient);
            services.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
            services.AddScoped<INewsletterRepository, NewsletterRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<IDtoJournalMapper, DtoJournalMapper>();
            services.AddScoped<IDtoNewsletterMapper, DtoNewsletterMapper>();
            services.AddScoped<IDtoNewsletterSubscriberMapper, DtoNewsletterSubscriberMapper>();
            services.AddScoped<IDtoFileObjectMapper, DtoFileObjectMapper>();
            services.AddScoped<IDtoSettingsMapper, DtoSettingsMapper>();
            services.AddScoped<INewsletterSubscriberService, NewsletterSubscriberService>();
            services.AddScoped<INewsletterService, NewsletterService>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            _newsletterSubscriberService = serviceProvider.GetService<INewsletterSubscriberService>();
            _newsletterService = serviceProvider.GetService<INewsletterService>();
            _dtoNewsletterMapper = serviceProvider.GetService<IDtoNewsletterMapper>();
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "|")}";
            var context = new Mock<FunctionContext>();
            _functionContext = context.Object;
        }

        private async Task<Newsletter> AddNewNewsletter()
        {
            NewsletterDto newsletter = new NewsletterDto();
            newsletter.Author = new Dictionary<Language, string>() { { Language.En, $"UnitTest" }, { Language.Pt, $"UnitTest" }, { Language.Ru, $"UnitTest" } };
            newsletter.Name = new Dictionary<Language, string>() { { Language.En, $"{_prefix}_English title" }, { Language.Pt, $"{_prefix}_Título em Português" }, { Language.Ru, $"{_prefix}_Название на Русском" } };
            var newNewsletter = await _newsletterService.AddOrUpdateNewsletter(_functionContext, newsletter);
            Assert.IsTrue(newNewsletter.Success);
            Assert.IsNotNull(newNewsletter.Value);
            Assert.IsTrue(newNewsletter.Value.Id != default);
            return newNewsletter.Value;
        }

        private async Task<NewsletterSubscriber> AddSubscriber()
        {
            var subPrefix = _prefix.Replace(" ", "_").Replace("|", "_").Replace(":", "_");
            var subscriber = await _newsletterSubscriberService.AddSubscriber($"{subPrefix}@harckade.com", Language.En);
            Assert.IsTrue(subscriber.Success);
            Assert.IsNotNull(subscriber.Value.PersonalToken);
            Assert.AreEqual(subscriber.Value.EmailAddress, $"{subPrefix}@harckade.com");
            return subscriber.Value;
        }

        [TestMethod]
        public async Task TestAddOrUpdateNewsletter()
        {
            //Create
            var newsletter = await AddNewNewsletter();

            //Update
            var updatedNewsletterDto = _dtoNewsletterMapper.DocumentToDto(newsletter);
            var updatedName = $"{_prefix}_English title updated";
            updatedNewsletterDto.Name = new Dictionary<Language, string>() { { Language.En, updatedName }, { Language.Pt, $"{_prefix}_Título em Português" }, { Language.Ru, $"{_prefix}_Название на Русском" } };
            var updatedNewsletter = await _newsletterService.AddOrUpdateNewsletter(_functionContext, updatedNewsletterDto, Language.En);
            Assert.IsTrue(updatedNewsletter.Success);
            Assert.IsNotNull(updatedNewsletter.Value);
            Assert.AreEqual(newsletter.Id, updatedNewsletter.Value.Id);
            Assert.AreNotEqual(newsletter.Name[Language.En], updatedNewsletter.Value.Name[Language.En]);
            Assert.AreEqual(newsletter.Name[Language.Pt], updatedNewsletter.Value.Name[Language.Pt]);
            Assert.AreEqual(updatedNewsletter.Value.Name[Language.En], updatedName);
        }

        [TestMethod]
        public async Task TestListAllNewsletters()
        {
            var newNewsletter = await AddNewNewsletter();
            Thread.Sleep(3600);
            var result = await _newsletterService.GetAll();
            Assert.IsTrue(result.Success);
            var newsletters = result.Value;
            Assert.IsNotNull(newsletters);
            Assert.IsTrue(newsletters.Any() && newsletters.Count() >= 1);
            Assert.IsNotNull(newsletters.FirstOrDefault(art => art.Id == newNewsletter.Id));
        }

        [TestMethod]
        public async Task TestGetNewsletterById()
        {
            var newsletter = await AddNewNewsletter();
            Thread.Sleep(3600);
            var result = await _newsletterService.GetById(newsletter.Id);
            Assert.IsTrue(result.Success);
            var sameNewsletter = result.Value;
            Assert.IsNotNull(sameNewsletter);
            Assert.AreEqual(sameNewsletter.Id, newsletter.Id);
        }

        [TestMethod]
        public async Task TestGetNewsletterContentById()
        {
            var newsletter = await AddNewNewsletter();
            Thread.Sleep(3600);

            var path = Path.Combine(Environment.CurrentDirectory, "test_files\\article.txt");
            var content = System.IO.File.ReadAllText(path);
            Assert.IsNotNull(content);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(content));
            using (var stream = Html.GenerateStreamFromString(content))
            {
                await _newsletterService.UploadBinary(newsletter, stream, Language.En);
            }
            Thread.Sleep(3600);
            var result = await _newsletterService.DownloadNewsletterBinary(newsletter, Language.En);
            Assert.IsTrue(result.Success);
            var binary = result.Value;
            Assert.IsNotNull(binary);
            Assert.IsTrue(binary.Length > 0);
            string text = string.Empty;
            using (var reader = new StreamReader(binary))
            {
                text = reader.ReadToEnd();
            }
            Assert.IsTrue(text.Length > 0);
        }

        [TestMethod]
        public async Task TestDeleteNewsletterById()
        {
            var newsletter = await AddNewNewsletter();
            Thread.Sleep(3600);
            var result = await _newsletterService.DeleteNewsletterById(newsletter.Id);
            Assert.IsTrue(result.Success);
            Thread.Sleep(3600);
            var deletedArticle = await _newsletterService.GetById(newsletter.Id);
            Assert.IsTrue(deletedArticle.Failed);
        }

        [TestMethod]
        public async Task TestSendNewsletterToQueue()
        {
            var newsletter = await AddNewNewsletter();
            Thread.Sleep(3600);
            var result = await _newsletterService.SendNewsletterToQueue(newsletter.Id);
            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task TestListAllSubscribers()
        {
            var subscribers = await _newsletterSubscriberService.GetSubscribers();
            Assert.IsTrue(subscribers.Success);
        }

        [TestMethod]
        public async Task TestSubscribeToNewsletter()
        {
            await AddSubscriber();
        }

        [TestMethod]
        public async Task TestRemoveSubscriber()
        {
            var subscriber = await AddSubscriber();
            var removeSubscriber = await _newsletterSubscriberService.RemoveSubscriberById(subscriber.Id);
            Assert.IsTrue(removeSubscriber.Success);
        }


        [TestMethod]
        public async Task TestConfirmNewsletterEmail()
        {
            var subscriber = await AddSubscriber();
            var confirmEmail = await _newsletterSubscriberService.ConfirmEmailAddress(subscriber);
            Assert.IsTrue(confirmEmail.Success);
        }

        [TestMethod]
        public async Task TestSendNewsletterToEmail()
        {
            var newsletter = await AddNewNewsletter();
            var path = Path.Combine(Environment.CurrentDirectory, "test_files\\article.txt");
            var content = System.IO.File.ReadAllText(path);
            Assert.IsNotNull(content);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(content));
            using (var stream = Html.GenerateStreamFromString(content))
            {
                await _newsletterService.UploadBinary(newsletter, stream, Language.En);
            }
            var subscriber = await AddSubscriber();
            var replacementList = new Dictionary<string, string>();
            replacementList.Add("{{email}}", subscriber.EmailAddress);
            replacementList.Add("{{id}}", $"{subscriber.Id}");
            replacementList.Add("{{personalToken}}", $"{subscriber.PersonalToken}");
            var sendToEmail = await _newsletterService.SendNewsletter(newsletter.Id, Language.En, subscriber.EmailAddress, replacementList);
            Assert.IsTrue(sendToEmail.Success);
        }

    }
}
