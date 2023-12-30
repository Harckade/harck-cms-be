namespace Harckade.CMS.Azure.Abstractions
{
    public interface ISettingsMapper
    {
        Entities.SettingsEntity DomainToEntity(Domain.Settings settings);
        Domain.Settings EntityToDomain(Entities.SettingsEntity settings);
    }
}
