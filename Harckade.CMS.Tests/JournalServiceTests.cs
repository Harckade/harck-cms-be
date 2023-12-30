using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Harckade.CMS.Tests
{
    [TestClass]
    public class JournalServiceTests
    {
        IJournalService _journalService;
        private FunctionContext _functionContext;
        private string _prefix;
        private QueueClient _queueClient;

        [TestInitialize]
        public void init()
        {
            var connectionString = "UseDevelopmentStorage=true";
            var path = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            var configurationFile = System.IO.File.ReadAllText(path);
            var fileJson = JsonConvert.DeserializeObject<LocalSettings>(configurationFile);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(fileJson.Values).Build();

            var queueClient = new QueueClient(connectionString, "journal");
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(queueClient);
            services.AddScoped<IJournalRepository, JournalRepository>();
            services.AddScoped<IJournalService, JournalService>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            var context = new Mock<FunctionContext>();
            _functionContext = context.Object;
            _journalService = serviceProvider.GetService<IJournalService>();
            _queueClient = serviceProvider.GetService<QueueClient>();
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "_").Replace(":", "_").Replace(" ", "_")}";
        }

        [TestMethod]
        public async Task AddAndReadEntry()
        {
            await _journalService.AddEntryToQueue(_functionContext, _prefix);
            Thread.Sleep(3600);
            var message = await _queueClient.PeekMessageAsync();
            var messageString64 = message.Value.Body.ToString();
            var base64EncodedBytes = Convert.FromBase64String(messageString64);
            var messageString = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            await _journalService.FromQueueToStorage(messageString, DateTimeOffset.UtcNow.ToUniversalTime());
            var result = await _journalService.GetEntries(DateTimeOffset.UtcNow.ToUniversalTime().AddHours(-2), DateTimeOffset.UtcNow.ToUniversalTime().AddHours(2));
            Assert.IsTrue(result.Success);
            var entries = result.Value;
            Assert.IsNotNull(entries);
            Assert.IsTrue(entries.Any());
            Assert.IsNotNull(entries.FirstOrDefault(entry => entry.Description == _prefix));
        }
    }
}