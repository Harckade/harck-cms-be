using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoSettingsMapper
    {
        SettingsDto DocumentToDto(Settings settings);
        Settings DtoToDocument(SettingsDto settings);
    }
}
