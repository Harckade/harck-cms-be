using Harckade.CMS.Azure.Domain;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface ISettingsRepository
    {
        Task InsertOrUpdate(Settings settings, bool ignoreDeploymentInfo = true);
        Task<Settings> Get();
    }
}
