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
          if (string.IsNullOrWhiteSpace(connectionString))
          {
              throw new ArgumentNullException(connectionString);
          }
          service.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
          service.AddScoped<IFileService, FileService>();
          service.AddScoped<IDtoFileObjectMapper, DtoFileObjectMapper>();
          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();