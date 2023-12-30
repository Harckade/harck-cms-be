using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
var queueClient = new QueueClient(connectionString, "newsletter");

var host = new HostBuilder()
      .ConfigureServices(service =>
      {
          if (string.IsNullOrWhiteSpace(connectionString))
          {
              throw new ArgumentNullException(connectionString);
          }
          service.AddSingleton(queueClient);
          service.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
          service.AddScoped<ISettingsRepository, SettingsRepository>();
          service.AddScoped<INewsletterSubscriptionTemplateRepository, NewsletterSubscriptionTemplateRepository>();
          service.AddScoped<INewsletterSubscriptionTemplateService, NewsletterSubscriptionTemplateService>();
          service.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
          service.AddScoped<INewsletterSubscriberService, NewsletterSubscriberService>();
          service.AddScoped<IDtoNewsletterSubscriberMapper, DtoNewsletterSubscriberMapper>();
          service.AddScoped<IEmailService, EmailService>();
          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();


