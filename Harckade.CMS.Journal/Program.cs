using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
var queueClient = new QueueClient(connectionString, "journal");


var host = new HostBuilder()
      .ConfigureServices(service =>
      {
          service.AddSingleton(queueClient);
          service.AddScoped<IJournalRepository, JournalRepository>();
          service.AddScoped<IJournalService, JournalService>();
          service.AddScoped<IDtoJournalMapper, DtoJournalMapper>();
          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
