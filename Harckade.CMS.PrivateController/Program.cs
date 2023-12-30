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
var queueClient = new QueueClient(connectionString, "journal");

var host = new HostBuilder()
      .ConfigureServices(service =>
      {
          if (string.IsNullOrWhiteSpace(connectionString))
          {
              throw new ArgumentNullException(connectionString);
          }
          service.AddSingleton(queueClient);
          service.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
          service.AddScoped<IArticleHelperRepository, ArticleHelperRepository>();
          service.AddScoped<IArticleRepository, ArticleRepository>();
          service.AddScoped<ISettingsRepository, SettingsRepository>();
          service.AddScoped<IJournalRepository, JournalRepository>();
          service.AddScoped<IJournalService, JournalService>();
          service.AddScoped<IDtoJournalMapper, DtoJournalMapper>();
          service.AddScoped<IArticleBackupRepository, ArticleBackupRepository>();
          service.AddScoped<IFileService, FileService>();
          service.AddScoped<IArticleBackupService, ArticleBackupService>();
          service.AddScoped<IArticleService, ArticleService>();
          service.AddScoped<IDtoArticleMapper, DtoArticleMapper>();
          service.AddScoped<IDtoArticleBackupMapper, DtoArticleBackupMapper>();
          service.AddScoped<IDtoFileObjectMapper, DtoFileObjectMapper>();

          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults((context, builder) =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
        builder.UseMiddleware<AuthorizationMiddleware>();
    })
    .Build();

host.Run();
