using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Services.Abstractions
{
    public interface IEmailService : IServiceBase
    {
        /// <summary>
        /// Implemented in accordance with AWS documentation
        /// https://docs.aws.amazon.com/ses/latest/dg/send-using-smtp-programmatically.html
        /// </summary>
        /// <param name="message">Message information sent by the client</param>
        /// <param name="to">By default DefaultEmailTo variable from configuration is used</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task<Result> SendEmailAsync(ContactDto message, string to = "");
        Task<Result> SendNewsletterEmailAsync(Newsletter newsletter, Language lang, string newsletterContent, string to = "");
        Task<Result> SendConfirmationEmailAsync(NewsletterSubscriptionTemplate template, Language lang, string to = "", string emailContent = "");
    }
}
