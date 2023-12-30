using Harckade.CMS.JwtAuthorization.Middleware;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults((context, builder) =>
    {
        builder.UseMiddleware<AuthenticationMiddleware>();
        builder.UseMiddleware<AuthorizationMiddleware>();
    })
    .Build();

host.Run();
