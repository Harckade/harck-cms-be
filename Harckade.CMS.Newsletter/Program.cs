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
          service.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
          service.AddSingleton(queueClient);
          service.AddScoped<ISettingsRepository, SettingsRepository>();
          service.AddScoped<INewsletterRepository, NewsletterRepository>();
          service.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
          service.AddScoped<INewsletterSubscriptionTemplateRepository, NewsletterSubscriptionTemplateRepository>();
          service.AddScoped<INewsletterService, NewsletterService>();
          service.AddScoped<IEmailService, EmailService>();
          service.AddScoped<INewsletterSubscriptionTemplateService, NewsletterSubscriptionTemplateService>();
          service.AddScoped<INewsletterSubscriberService, NewsletterSubscriberService>();
          service.AddScoped<IDtoNewsletterMapper, DtoNewsletterMapper>();
          service.AddScoped<IDtoNewsletterSubscriberMapper, DtoNewsletterSubscriberMapper>();
          service.AddScoped<IFileService, FileService>();
          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
