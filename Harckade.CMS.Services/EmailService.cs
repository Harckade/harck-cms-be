using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;

namespace Harckade.CMS.Services
{
    public class EmailService : ServiceBase, IEmailService
    {
        private IConfiguration _configuration;
        private ILogger<EmailService> _appInsights;
        private int _port = 587;
        private NetworkCredential _credentials;
        private SmtpClient _client;
        private const bool _enableSsl = true;

        private string _configSet = string.Empty;
        private string _from = "sender@example.com";

        /// <summary>
        /// Intialize AWS Simple Email Service client.
        /// </summary>
        /// <param name="appInsights"></param>
        /// <param name="configuration"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EmailService(ILogger<EmailService> appInsights, IConfiguration configuration)
        {
            _configuration = configuration;
            _appInsights = appInsights;
            _oidIsSet = false;
 
            var smtpUsername = _configuration["SmtpUsername"];
            var smtpPassword = _configuration["SmtpPassword"];
            var host = _configuration["EmailHost"];
            var configSet = _configuration["ConfigSet"];
            var port = _configuration["SmtpPort"];
            var from = _configuration["EmailFrom"];

            if (string.IsNullOrWhiteSpace(smtpUsername))
            {
                throw new ArgumentNullException(nameof(smtpUsername));
            }
            if (string.IsNullOrWhiteSpace(smtpPassword))
            {
                throw new ArgumentNullException(nameof(smtpUsername));
            }
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (string.IsNullOrWhiteSpace(from))
            {
                throw new ArgumentNullException(nameof(from));
            }
            if (!string.IsNullOrWhiteSpace(configSet))
            {
                _configSet = configSet;
            }
            if (!string.IsNullOrWhiteSpace(port))
            {
                _port = Int32.Parse(port);
            }
            _credentials = new NetworkCredential(smtpUsername, smtpPassword);
            _client = new SmtpClient(host, _port);
            _from = from;
        }

        private async Task<Result> SendEmail(string from, string to, string subject, string body)
        {
            _appInsights.LogInformation($"EmailService | SendEmail: {subject}", _oid);

            if (string.IsNullOrWhiteSpace(to))
            {
                Result.Fail(Failure.EmailDestinataryIsNull);
            }
            if (string.IsNullOrWhiteSpace(subject))
            {
                Result.Fail(Failure.EmailSubjectIsEmpty);
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                return Result.Fail(Failure.EmailMessageIsEmpty);
            }

            MailMessage mailMessage = new MailMessage();
            mailMessage.IsBodyHtml = true;
            mailMessage.From = new MailAddress(_from, from);
            mailMessage.To.Add(new MailAddress(to));
            mailMessage.Subject = subject;
            mailMessage.Body = body;

            if (!string.IsNullOrWhiteSpace(_configSet))
            {
                mailMessage.Headers.Add("X-SES-CONFIGURATION-SET", _configSet);
            }

            using (var client = _client)
            {
                client.Credentials = _credentials;
                client.EnableSsl = _enableSsl;

                try
                {
                    await client.SendMailAsync(mailMessage);
                    _appInsights.LogInformation("Email sent to: ", to);
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Transaction failed. The server response was: Message rejected: Email address is not verified.") && ex.Message.Contains("unit_test_") && ex.Message.EndsWith(_configuration["RedirectUrl"]))
                    {
                        return Result.Ok();
                    }
                    _appInsights.LogError($"Failed to send an email to: ${to} | Error message:", ex.Message);
                    return Result.Fail(Failure.EmailWasNotSent);
                }
            }
        }

        public async Task<Result> SendEmailAsync(ContactDto message, string to = "")
        {
            _appInsights.LogInformation($"EmailService | SendEmailAsync: {message.Subject}", _oid);
            var subject = "Default subject";
            if (string.IsNullOrWhiteSpace(to))
            {
                to = _configuration["DefaultEmailTo"];
            }
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentNullException(nameof(to));
            }
            if (!string.IsNullOrWhiteSpace(message.Subject))
            {
                subject = message.Subject;
            }
            if (string.IsNullOrWhiteSpace(message.Message))
            {
                return Result.Fail(Failure.EmailMessageIsEmpty);
            }

            var body = @$"<html><body><p><strong>Email:</strong> ${message.Email}</p>
            <p><strong>Website:</strong>{message.Website}</p>
            <p><strong>Message:</strong></p>
            <p>--------------------------</p>
            <p>{message.Message}</p>
            <p>--------------------------</p>
            <p>Sent from Harck-CMS platform</p>
            </body></html>";

            return await SendEmail(message.Name, to, subject, body);
        }

        public async Task<Result> SendNewsletterEmailAsync(Newsletter newsletter, Language lang, string newsletterContent, string to = "")
        {
            _appInsights.LogInformation($"EmailService | SendNewsletterEmailAsync: {newsletter.Id}", _oid);
            return await SendEmail(newsletter.Author[lang], to, newsletter.Name[lang], newsletterContent);
        }

        public async Task<Result> SendConfirmationEmailAsync(NewsletterSubscriptionTemplate template, Language lang, string to = "", string emailContent ="")
        {
            _appInsights.LogInformation($"EmailService | SendConfirmationEmailAsync: {to} | language: {lang}", _oid);

            if (lang == default)
            {
                return Result.Fail(Failure.LanguageRequired);
            }
            if (string.IsNullOrWhiteSpace(emailContent))
            {
                return Result.Fail(Failure.InvalidInput);
            }
            return await SendEmail(template.Author != null ? template.Author[lang]: "Blog", to, template.Subject != null ? template.Subject[lang]: "Confirm your email - Newsletter subscription", emailContent);
        }
    }
}
