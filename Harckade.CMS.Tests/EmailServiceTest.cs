using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;


namespace Harckade.CMS.Tests
{
    [TestClass]
    public class EmailServiceTest
    {
        IEmailService _emailService;
        private string _prefix;

        [TestInitialize]
        public void init()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            var configurationFile = System.IO.File.ReadAllText(path);
            var fileJson = JsonConvert.DeserializeObject<LocalSettings>(configurationFile);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(fileJson.Values).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IEmailService, EmailService>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            _emailService = serviceProvider.GetService<IEmailService>();
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "_").Replace(":", "_").Replace(" ", "_")}";
        }

        [TestMethod]
        public async Task SendEmail()
        {
            var message = new Azure.Dtos.ContactDto()
            {
                Email = $"{_prefix}@harckade.com",
                Message = _prefix,
                Name = "Unit test",
                Subject = _prefix
            };
            var result = await _emailService.SendEmailAsync(message);
            Assert.IsTrue(result.Success);
        }
    }
}
