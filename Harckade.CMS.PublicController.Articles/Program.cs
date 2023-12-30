using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

var host = new HostBuilder()
      .ConfigureServices(service =>
      {
          service.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
          service.AddScoped<IArticleBackupRepository, ArticleBackupRepository>();
          service.AddScoped<IArticleBackupService, ArticleBackupService>();
          service.AddScoped<IArticleHelperRepository, ArticleHelperRepository>();
          service.AddScoped<IArticleRepository, ArticleRepository>();
          service.AddScoped<IArticleService, ArticleService>();
          service.AddScoped<IDtoArticleMapper, DtoArticleMapper>();
          service.AddScoped<IFileService, FileService>();
          service.AddScoped<ISettingsRepository, SettingsRepository>();
          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();


