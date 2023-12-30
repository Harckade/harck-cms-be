using Azure.Identity;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Harckade.CMS.Tests
{
    [TestClass]
    public class AdminServiceTests
    {
        IAdminService _adminService;
        IDtoUserMapper _dtoUserMapper;
        private string _prefix;

        [TestInitialize]
        public void init()
        {
            var connectionString = "UseDevelopmentStorage=true";

            var path = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            var configurationFile = System.IO.File.ReadAllText(path);
            var fileJson = JsonConvert.DeserializeObject<LocalSettings>(configurationFile);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(fileJson.Values).Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();

            #region Build a Microsoft Graph client application.
            var clientId = fileJson.Values["ClientId"];
            var clientSecret = fileJson.Values["ClientSecretValue"];
            var tenantId = fileJson.Values["TenantId"];
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
            services.AddSingleton(graphClient);
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IDtoUserMapper, DtoUserMapper>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            _adminService = serviceProvider.GetService<IAdminService>();
            _dtoUserMapper = serviceProvider.GetService<IDtoUserMapper>();
            _adminService.UpdateOid(new ObservabilityId());
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "_").Replace(":", "_").Replace(" ", "_")}";
        }

        [TestMethod]
        public async Task<Settings> GetSettings()
        {
            var result = await _adminService.GetSettings();
            Assert.IsTrue(result.Success);
            var settings = result.Value;
            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.DefaultLanguage);
            return settings;
        }

        [TestMethod]
        public async Task UpdateSettings()
        {
            var settings = await GetSettings();
            var defaultLanguage = settings.DefaultLanguage;
            settings.UpdateDefaultLanguage(settings.DefaultLanguage == Azure.Enums.Language.Pt ? Azure.Enums.Language.En : Azure.Enums.Language.Pt);
            Thread.Sleep(3600);
            var updateSettingsResult = await _adminService.UpdateSettings(settings);
            Assert.IsTrue(updateSettingsResult.Success);
            Thread.Sleep(3600);
            var updatedSettings = await _adminService.GetSettings();
            Assert.AreNotEqual(updatedSettings.Value.DefaultLanguage, defaultLanguage);
            settings.UpdateDefaultLanguage(Azure.Enums.Language.En);
            await _adminService.UpdateSettings(settings);
        }

        [TestMethod]
        public async Task GetUsers()
        {
            var result = await _adminService.GetUsers();
            Assert.IsTrue(result.Success);
            var users = result.Value;
            Assert.IsNotNull(users);
            Assert.IsNotNull(users.Any());
        }

        [TestMethod]
        public async Task<Azure.Domain.User> InviteUser()
        {
            var email = $"{_prefix}@harckade.com";
            var userToInvite = new Azure.Domain.User(new Azure.Dtos.UserDto()
            {
                Email = email,
                Name = _prefix,
                Role = Enum.GetName(UserRoles.Viewer)
            });
            var result = await _adminService.InviteUser(userToInvite);
            Assert.IsTrue(result.Success);
            var users = await _adminService.GetUsers();
            Assert.IsTrue(users.Success);
            var user = users.Value.FirstOrDefault(u => u.Name == _prefix);
            Assert.IsNotNull(user);
            Assert.IsNotNull(user.Id);
            Assert.IsTrue(user.Id != default);
            Assert.AreEqual(user.Name, _prefix);
            Assert.AreEqual(user.Email, email);
            return user;
        }

        [TestMethod]
        public async Task DeleteUser()
        {
            var user = await InviteUser();
            Thread.Sleep(3600);
            var deleteUserResult = await _adminService.DeleteUser(user);
            Assert.IsTrue(deleteUserResult.Success);
        }

        [TestMethod]
        public async Task DeleteUserById()
        {
            var user = await InviteUser();
            Thread.Sleep(3600);
            var deleteUserResult = await _adminService.DeleteUserById(user.Id);
            Assert.IsTrue(deleteUserResult.Success);
        }

        [TestMethod]
        public async Task EditUser()
        {
            var user = await InviteUser();
            Thread.Sleep(3600);
            UserDto tmpUser = _dtoUserMapper.DocumentToDto(user);
            tmpUser.Role = "Editor";
            var updateResult = await _adminService.EditUser(_dtoUserMapper.DtoToDocument(tmpUser));
            Assert.IsTrue(updateResult.Success);
            var updatedUser = updateResult.Value;
            Assert.IsTrue(updatedUser.Roles.Contains(UserRoles.Editor));
        }

        [TestMethod]
        public async Task ClearAfterTests()
        {
            var result = await _adminService.GetUsers();
            Assert.IsTrue(result.Success);
            var users = result.Value;
            Thread.Sleep(3600);
            foreach (var user in users.Where(a => a.Name.StartsWith("unit_test_")))
            {
                await _adminService.DeleteUser(user);
            }
        }
    }
}
