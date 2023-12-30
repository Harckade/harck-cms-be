using Azure.Identity;
using Azure.Storage.Queues;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.JwtAuthorization.Middleware;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Identity.Client;

var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
// Values from app registration
var clientId = Environment.GetEnvironmentVariable("ClientId");
var clientSecret = Environment.GetEnvironmentVariable("ClientSecretValue");
var tenantId = Environment.GetEnvironmentVariable("TenantId");

#region Build a Microsoft Graph client application.
IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder.Create(clientId).Build();
// The client credentials flow requires that you request the
// /.default scope, and preconfigure your permissions on the
// app registration in Azure. An administrator must grant consent
// to those permissions beforehand.
var scopes = new[] { "https://graph.microsoft.com/.default" };

var options = new TokenCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
};


var clientSecretCredential = new ClientSecretCredential(
    tenantId, clientId, clientSecret, options);
#endregion

var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
var queueClient = new QueueClient(connectionString, "journal");


var host = new HostBuilder()
      .ConfigureServices(service =>
      {
          service.AddSingleton(graphClient);
          service.AddSingleton(queueClient);
          service.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
          service.AddScoped<ISettingsRepository, SettingsRepository>();
          service.AddScoped<IJournalRepository, JournalRepository>();
          service.AddScoped<IJournalService, JournalService>();
          service.AddScoped<IDtoJournalMapper, DtoJournalMapper>();
          service.AddScoped<IAdminService, AdminService>();
          service.AddScoped<IDtoUserMapper, DtoUserMapper>();
          service.AddScoped<IDtoFileObjectMapper, DtoFileObjectMapper>();
          service.AddScoped<IDtoSettingsMapper, DtoSettingsMapper>();

          service.AddLogging();
      })
    .ConfigureFunctionsWorkerDefaults((context, builder) =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
        builder.UseMiddleware<AuthorizationMiddleware>();
    })
    .Build();

host.Run();
