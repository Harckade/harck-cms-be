using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IAdminService : IServiceBase
    {
        /// <summary>
        /// List all system's users.
        /// </summary>
        /// <returns>A collection of users</returns>
        Task<Result<IEnumerable<User>>> GetUsers();
        /// <summary>
        /// Register and send an invite to a new user.
        /// The invite will be automatically sent by Azure Active Directory
        /// </summary>
        /// <param name="user">Mandatory fields: Email, role, name</param>
        /// <returns>User</returns>
        /// <exception cref="Exception">
        /// T: Exception("Invitation was not send")
        /// </exception>
        Task<Result<User>> InviteUser(User user);
        /// <summary>
        /// Update user information.
        /// E.g.: Change user role.
        /// </summary>
        /// <param name="user">User that will be updated</param>
        /// <returns>Updated user</returns>
        Task<Result<User>> EditUser(User user);
        /// <summary>
        /// Delete an registred user
        /// </summary>
        /// <param name="user">User that will be deleted</param>
        /// <returns>Result.Ok when user is deleted. Result.Fail in case of failure</returns>
        Task<Result> DeleteUser(User user);
        /// <summary>
        /// Delete an registred user by providing user's identifier
        /// </summary>
        /// <param name="userId">User's identifier</param>
        /// <returns>Result.Ok when user is deleted. Result.Fail in case of failure</returns>
        Task<Result> DeleteUserById(Guid userId);
        /// <summary>
        /// Update system settings: Available languages; default language
        /// </summary>
        /// <param name="settings">Mandatory fields: available languages. When no default language is provided, a language from available's list is used instead.</param>
        /// <returns>Updated settings</returns>
        Task<Result> UpdateSettings(Settings settings);
        /// <summary>
        /// Retrieve system's settings.
        /// Settings include: Available languages; default language
        /// </summary>
        /// <returns>System settings</returns>
        Task<Result<Settings>> GetSettings();
    }
}
