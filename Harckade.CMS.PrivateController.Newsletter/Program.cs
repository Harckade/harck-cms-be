using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.JwtAuthorization.Middleware;
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
          service.AddScoped<INewsletterRepository, NewsletterRepository>();
          service.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
          service.AddScoped<INewsletterSubscriptionTemplateRepository, NewsletterSubscriptionTemplateRepository>();

          service.AddScoped<ISettingsRepository, SettingsRepository>();
          service.AddScoped<IJournalRepository, JournalRepository>();
          service.AddScoped<IJournalService, JournalService>();
          service.AddScoped<IDtoJournalMapper, DtoJournalMapper>();

          service.AddScoped<IFileService, FileService>();
          service.AddScoped<IEmailService, EmailService>();
     
          service.AddScoped<INewsletterService, NewsletterService>();
          service.AddScoped<INewsletterSubscriberService, NewsletterSubscriberService>();
          service.AddScoped<IDtoNewsletterMapper, DtoNewsletterMapper>();
          service.AddScoped<IDtoNewsletterSubscriberMapper, DtoNewsletterSubscriberMapper>();
          service.AddScoped<IDtoFileObjectMapper, DtoFileObjectMapper>();

          service.AddScoped<INewsletterSubscriptionTemplateService, NewsletterSubscriptionTemplateService>();
          service.AddScoped<IDtoNewsletterSubscriptionTemplateMapper, DtoNewsletterSubscriptionTemplateMapper>();

          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults((context, builder) =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
        builder.UseMiddleware<AuthorizationMiddleware>();
    })
    .Build();

host.Run();
