using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
   .ConfigureServices(service =>
   {
       service.AddScoped<IEmailService, EmailService>();
       service.AddLogging();
   })
   .ConfigureFunctionsWorkerDefaults()
   .Build();
host.Run();