using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoSettingsMapper : IDtoSettingsMapper
    {
        public SettingsDto DocumentToDto(Settings settings)
        {
            return new SettingsDto()
            {
                Languages = settings.Languages.Select(lang => Enum.GetName(lang)),
                DefaultLanguage = Enum.GetName(settings.DefaultLanguage),
                LastDeploymentDate = settings.LastDeploymentDate.ToUniversalTime(),
                RequiresDeployment = settings.RequiresDeployment
            };
        }

        public Settings DtoToDocument(SettingsDto settings)
        {
            return new Settings(settings);
        }
    }
}
