using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Mappers
{
    public class SettingsMapper : ISettingsMapper
    {
        public SettingsEntity DomainToEntity(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            return new SettingsEntity()
            {
                RowKey = "0",
                PartitionKey = "0",
                Languages = JsonConvert.SerializeObject(settings.Languages.Select(lang => Enum.GetName(lang))),
                DefaultLanguage = JsonConvert.SerializeObject(Enum.GetName(settings.DefaultLanguage)),
                LastDeploymentDate = settings.LastDeploymentDate == default ? new DateTime(1601, 1, 1).ToUniversalTime() : settings.LastDeploymentDate.ToUniversalTime(),
                RequiresDeployment = settings.RequiresDeployment
            };
        }

        public Settings EntityToDomain(SettingsEntity settings)
        {
            return new Settings(settings);
        }
    }
}
