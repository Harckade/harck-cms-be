using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class NewsletterSubscriptionTemplate
    {
        public IReadOnlyDictionary<Language, string> Subject { get { return _Subject; } }
        public IReadOnlyDictionary<Language, string> Author { get { return _Author; } }
        public DateTime Timestamp { get; private set; }

        private Dictionary<Language, string> _Subject;
        private Dictionary<Language, string> _Author;

        private void setNewsletterSubscriptionTemplate(Dictionary<Language, string> subject, Dictionary<Language, string> author)
        {
            Dictionary<Language, string> _Subject = new Dictionary<Language, string>();
            Dictionary<Language, string> _author = new Dictionary<Language, string>();

            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang == Language.None)
                {
                    continue;
                }
                _Subject.Add(lang, subject == null || !subject.ContainsKey(lang) ? string.Empty : subject[lang]);
                _author.Add(lang, author == null || !author.ContainsKey(lang) ? string.Empty : author[lang]);
            }
        }

        public NewsletterSubscriptionTemplate(NewsletterSubscriptionTemplateEntity subscriptionTemplate)
        {
            var _entitySubject = string.IsNullOrWhiteSpace(subscriptionTemplate.Subject) ? new Dictionary<Language, string>() : JsonConvert.DeserializeObject<Dictionary<Language, string>>(subscriptionTemplate.Subject);
            var _entityAuthor = string.IsNullOrWhiteSpace(subscriptionTemplate.Author) ? new Dictionary<Language, string>() : JsonConvert.DeserializeObject<Dictionary<Language, string>>(subscriptionTemplate.Author);
            setNewsletterSubscriptionTemplate(_entitySubject, _entityAuthor);
            Timestamp = subscriptionTemplate.Timestamp.Value.UtcDateTime;
        }

        public NewsletterSubscriptionTemplate()
        {
            setNewsletterSubscriptionTemplate(null, null);
        }

        private Dictionary<Language, string> fitlerText(Dictionary<Language, string> dict, IEnumerable<Language> languages)
        {
            return dict.Where(text => languages.Contains(text.Key) && !string.IsNullOrWhiteSpace(text.Value)).ToDictionary(i => i.Key, i => i.Value);
        }

        public NewsletterSubscriptionTemplate FilterResultsBySettings(Settings settings)
        {
            if (settings == null || !settings.Languages.Any())
            {
                throw new ArgumentNullException(nameof(settings));
            }
            _Subject = fitlerText(_Subject, settings.Languages);
            _Author = fitlerText(_Author, settings.Languages);
            return this;
        }

        public NewsletterSubscriptionTemplate(Dictionary<Language, string> Subject, Dictionary<Language, string> Author)
        {
            _Subject = Subject;
            _Author = Author;
        }

        public NewsletterSubscriptionTemplate(NewsletterSubscriptionTemplateDto newsletterSubscriptionTemplate)
        {
            var Subject = newsletterSubscriptionTemplate.Subject == null ? new Dictionary<Language, string>() : newsletterSubscriptionTemplate.Subject;
            var Author = newsletterSubscriptionTemplate.Author == null ? new Dictionary<Language, string>() : newsletterSubscriptionTemplate.Author;
            setNewsletterSubscriptionTemplate(Subject, Author);
        }


        public void UpdateSubjectForLanguage(string Subject, Language lang)
        {
            if (_Subject == null)
            {
                _Subject = new Dictionary<Language, string>();
            }
            _Subject[lang] = Subject;
        }

        public void UpdateAuthorForLanguage(string Author, Language lang)
        {
            if (_Author == null)
            {
                _Author = new Dictionary<Language, string>();
            }
            _Author[lang] = Author;
        }

        public string GetTitles()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Subject != null && _Subject.Any())
            {
                foreach (var subject in _Subject.Where(n => n.Value != null && !string.IsNullOrWhiteSpace(n.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), subject.Key), subject.Value == null ? "" : subject.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetAuthor()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Author != null && _Author.Any())
            {
                foreach (var author in _Author.Where(n => n.Value != null && !string.IsNullOrWhiteSpace(n.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), author.Key), author.Value == null ? "" : author.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }
    }
}
