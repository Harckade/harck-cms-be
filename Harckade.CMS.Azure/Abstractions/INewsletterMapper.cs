namespace Harckade.CMS.Azure.Abstractions
{
    public interface INewsletterMapper
    {
        Entities.NewsletterEntity DomainToEntity(Domain.Newsletter newsletter);
        Domain.Newsletter EntityToDomain(Entities.NewsletterEntity newsletter);
    }
}
