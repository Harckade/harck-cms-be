using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.JwtAuthorization.Authorization;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Harckade.CMS.Services
{
    public class AdminService : ServiceBase, IAdminService
    {
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<AdminService> _appInsights;

        public AdminService(IConfiguration configuration, GraphServiceClient graphServiceClient, ISettingsRepository settingsRepository, ILogger<AdminService> appInsights)
        {
            _graphServiceClient = graphServiceClient;
            _configuration = configuration;
            _settingsRepository = settingsRepository;
            _appInsights = appInsights;
            _oidIsSet = false;
        }

        public async Task<Result> DeleteUser(Azure.Domain.User user)
        {
            checkIfOidIsSet();
            _appInsights.LogDebug("AdminService | DeleteUser", _oid);
            return await DeleteUserById(user.Id);
        }

        public async Task<Result> DeleteUserById(Guid userId)
        {
            checkIfOidIsSet();
            _appInsights.LogInformation($"AdminService | DeleteUserById: {userId}", _oid);
            if (userId == default)
            {
                return Result.Fail(Failure.InvalidInput, nameof(userId));
            }
            var usersListResult = await GetUsers();
            if (usersListResult.Failed)
            {
                return Result.Fail(usersListResult.FailureReason);
            }
            var usersList = usersListResult.Value;
            if (usersList.Where(u => u.Id != userId).Count(u => u.Roles.Contains(UserRoles.Administrator)) == 0 && usersList.Where(u => u.Id == userId).FirstOrDefault().Roles.Contains(UserRoles.Administrator))
            {
                return Result.Fail(Failure.AdministratorRequired);
            }
            await _graphServiceClient.Users[$"{userId}"].Request().DeleteAsync();
            return Result.Ok();
        }

        public async Task<Result<Azure.Domain.User>> EditUser(Azure.Domain.User user)
        {
            checkIfOidIsSet();
            _appInsights.LogInformation($"AdminService | EditUser: {user.Id}", _oid);
            if (user.Id == default)
            {
                return Result.Fail<Azure.Domain.User>(Failure.InvalidInput, nameof(user.Id));
            }
            var auxRes = await GetAppRoles();
            var userAd = auxRes.Users.Where(u => Guid.Parse(u.Id) == user.Id).FirstOrDefault();
            if (userAd == null)
            {
                return Result.Fail<Azure.Domain.User>(Failure.UserNotFound, nameof(user.Id));
            }

            var userRole = auxRes.AppRoles.Where(r => r.Value == Enum.GetName(typeof(UserRoles), user.Roles.FirstOrDefault()).ToLower()).FirstOrDefault();
            if (userRole == null)
            {
                return Result.Fail<Azure.Domain.User>(Failure.InvalidInput, nameof(userRole));
            }

            var rolesToDelete = userAd.AppRoleAssignments.Select(r => r.Id);
            foreach (var role in rolesToDelete)
            {
                await _graphServiceClient.Users[$"{userAd.Id}"].AppRoleAssignments[role].Request().DeleteAsync();
            }

            var appRoleAssignment = new AppRoleAssignment
            {
                PrincipalId = user.Id,
                ResourceId = auxRes.ResourceId,
                AppRoleId = userRole.Id
            };

            await _graphServiceClient.Users[$"{user.Id}"].AppRoleAssignments
                .Request()
                .AddAsync(appRoleAssignment);

            user.Update(user, userAd);
            return Result.Ok(user);
        }

        public async Task<Result<IEnumerable<Azure.Domain.User>>> GetUsers()
        {
            checkIfOidIsSet();
            _appInsights.LogInformation("AdminService | GetUsers", _oid);
            var usersToReturn = new List<Azure.Domain.User>();
            var auxRes = await GetAppRoles();
            foreach (var user in auxRes.Users)
            {
                var tmpUser = new Azure.Domain.User(user, auxRes.AppRoles);
                if (tmpUser == null || string.IsNullOrWhiteSpace(tmpUser.Email))
                {
                    continue;
                }
                usersToReturn.Add(tmpUser);
            }

            return Result.Ok<IEnumerable<Azure.Domain.User>>(usersToReturn);
        }

        public async Task<Result<Azure.Domain.User>> InviteUser(Azure.Domain.User user)
        {
            checkIfOidIsSet();
            _appInsights.LogInformation($"AdminService | InviteUser: {user.Id}", _oid);
            if (!Validations.IsValidEmail(user.Email))
            {
                return Result.Fail<Azure.Domain.User>(Failure.InvalidInput, nameof(user.Email));
            }

            var auxRes = await GetAppRoles();


            if (auxRes.Users.Select(u => Utils.MicrosoftGraph.PrincipalNameToEmail(u.UserPrincipalName)).Contains(user.Email))
            {
                return Result.Fail<Azure.Domain.User>(Failure.UserAlreadyExists);
            }

            var userRole = auxRes.AppRoles.Where(r => r.Value == Enum.GetName(typeof(UserRoles), user.Roles.FirstOrDefault()).ToLower()).FirstOrDefault();
            if (userRole == null)
            {
                return Result.Fail<Azure.Domain.User>(Failure.InvalidInput, nameof(userRole));
            }

            var invitation = new Invitation
            {
                InvitedUserEmailAddress = user.Email,
                InviteRedirectUrl = _configuration["RedirectUrl"],
                SendInvitationMessage = true
            };

            var tmpNewUser = await _graphServiceClient.Invitations
                .Request()
                .AddAsync(invitation);

            if (tmpNewUser == null)
            {
                throw new Exception("Invitation was not send");
            }


            var appRoleAssignment = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(tmpNewUser.InvitedUser.Id),
                ResourceId = auxRes.ResourceId,
                AppRoleId = userRole.Id
            };

            await _graphServiceClient.Users[tmpNewUser.InvitedUser.Id].AppRoleAssignments
                .Request()
                .AddAsync(appRoleAssignment);
            user.Update(user, tmpNewUser);
            return Result.Ok(user);
        }

        public async Task<Result> UpdateSettings(Settings settings)
        {
            checkIfOidIsSet();
            _appInsights.LogInformation($"AdminService | UpdateSettings", _oid);
            if (!settings.Languages.Any())
            {
                return Result.Fail(Failure.LanguageRequired);
            }
            if (!settings.Languages.Contains(settings.DefaultLanguage))
            {
                settings.UpdateDefaultLanguage(settings.Languages.FirstOrDefault());
            }
            await _settingsRepository.InsertOrUpdate(settings);
            return Result.Ok();
        }

        public async Task<Result<Settings>> GetSettings()
        {
            checkIfOidIsSet();
            _appInsights.LogDebug($"AdminService | GetSettings", _oid);
            var settings = await _settingsRepository.Get();
            if (settings == null)
            {
                var newSettings = new Azure.Dtos.SettingsDto()
                {
                    Languages = new List<string>() { Enum.GetName(Language.En) },
                    DefaultLanguage = Enum.GetName(Language.En)
                };
                var updateResult = await UpdateSettings(new Settings(newSettings));
                if (updateResult.Success)
                {
                    return await GetSettings();
                }
                return Result.Fail<Settings>(updateResult.FailureReason, updateResult.Description);
            }
            return Result.Ok(settings);
        }

        /// <summary>
        /// Retrieve application roles from Azure Active Directory
        /// </summary>
        /// <returns>Microsoft Graph's AppRoles list</returns>
        /// <exception cref="Exception"></exception>
        private async Task<AuxiliarResources> GetAppRoles()
        {
            _appInsights.LogDebug($"AdminService | GetAppRoles", _oid);
            checkIfOidIsSet();
            IEnumerable<AppRole> appRoles = new List<AppRole>();
            IEnumerable<Microsoft.Graph.User> users = new List<Microsoft.Graph.User>();
            IEnumerable<Microsoft.Graph.ServicePrincipal> principals = new List<Microsoft.Graph.ServicePrincipal>();

            var resourceId = Guid.Empty;

            var batchRequestContent = new BatchRequestContent();

            var usersRequestId = batchRequestContent.AddBatchRequestStep(_graphServiceClient.Users.Request().Expand(u => u.AppRoleAssignments));
            var allAppRolesId = batchRequestContent.AddBatchRequestStep(_graphServiceClient.Applications[_configuration["ObjectId"]].Request());
            var enterpriseAppId = batchRequestContent.AddBatchRequestStep(_graphServiceClient.ServicePrincipals.Request());
            var returnedResponse = await _graphServiceClient.Batch.Request().PostAsync(batchRequestContent);

            //De-serialize all app-roles
            try
            {
                var response = await returnedResponse.GetResponseByIdAsync<Microsoft.Graph.Application>(allAppRolesId);
                appRoles = response.AppRoles;
            }
            catch (ServiceException ex)
            {
                throw new Exception("Failed to load appRoles", ex);
            }

            //De-serialize users
            try
            {
                var response = await returnedResponse.GetResponseByIdAsync<Microsoft.Graph.GraphServiceUsersCollectionResponse>(usersRequestId);
                users = response.Value;
            }
            catch (ServiceException ex)
            {
                throw new Exception("Failed to load users", ex);
            }

            //De-serialize all service-principles
            try
            {
                var response = await returnedResponse.GetResponseByIdAsync<Microsoft.Graph.GraphServiceServicePrincipalsCollectionResponse>(enterpriseAppId);
                principals = response.Value;
            }
            catch (ServiceException ex)
            {
                throw new Exception("Failed to load appRoles", ex);
            }

            if (!appRoles.Any() || !users.Any() || !principals.Any())
            {
                throw new Exception("Microsoft Graph Exception");
            }


            foreach (var app in principals)
            {
                if (app.AppId == _configuration["ClientId"])
                {
                    resourceId = Guid.Parse(app.Id);
                }
            }
            if (resourceId == default)
            {
                throw new Exception("Enterprise application not found");
            }
            return new AuxiliarResources(appRoles, users, resourceId);
        }

        /// <summary>
        /// A class that is used to parse/store Microsoft Graph results
        /// </summary>
        private class AuxiliarResources
        {
            public IEnumerable<AppRole> AppRoles { get; set; }
            public IEnumerable<Microsoft.Graph.User> Users { get; set; }
            public Guid ResourceId { get; set; }

            public AuxiliarResources(IEnumerable<AppRole> appRoles, IEnumerable<Microsoft.Graph.User> users, Guid resourceId)
            {
                AppRoles = appRoles;
                Users = users;
                ResourceId = resourceId;
            }
        }
    }
}
