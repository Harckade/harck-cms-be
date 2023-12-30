using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;

namespace Harckade.CMS.Azure.Domain
{
    public class Newsletter
    {
        public Guid Id { get; private set; }
        public IReadOnlyDictionary<Language, string> Author { get { return _Author; } }
        public IReadOnlyDictionary<Language, string> Name { get { return _Name; } }
        public IReadOnlyDictionary<Language, string> ContentHash { get { return _ContentHash; } }
        public DateTime SendDate { get; private set; }
        public DateTime Timestamp { get; private set; }

        private Dictionary<Language, string> _Author;
        private Dictionary<Language, string> _Name;
        private Dictionary<Language, string> _ContentHash;

        private void setNewsletter(Dictionary<Language, string> name, Dictionary<Language, string> author, Dictionary<Language, string> contentHash = null)
        {
            Dictionary<Language, string> _name = new Dictionary<Language, string>();
            Dictionary<Language, string> _author = new Dictionary<Language, string>();
            Dictionary<Language, string> _contentHash = new Dictionary<Language, string>();

            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang == Language.None)
                {
                    continue;
                }
                _name.Add(lang, !name.ContainsKey(lang) ? string.Empty : name[lang]);
                _author.Add(lang, !author.ContainsKey(lang) ? string.Empty : author[lang]);
                if (contentHash != null)
                {
                    _contentHash.Add(lang, !contentHash.ContainsKey(lang) ? string.Empty : contentHash[lang]);
                }
            }
            _ContentHash = _contentHash;
            Id = Guid.NewGuid();
            _Name = _name;
            _Author = _author;
        }

        public Newsletter(NewsletterEntity newsletter)
        {
            Guid rowKey = Guid.ParseExact(newsletter.RowKey, "N");
            if (rowKey == default)
            {
                throw new ArgumentException(nameof(newsletter.RowKey));
            }
            var _entityName = string.IsNullOrWhiteSpace(newsletter.Name) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(newsletter.Name);
            var _entityAuthor = string.IsNullOrWhiteSpace(newsletter.Author) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(newsletter.Author);
            var _entityContentHash = string.IsNullOrWhiteSpace(newsletter.ContentHash) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(newsletter.ContentHash);
            setNewsletter(_entityName, _entityAuthor, _entityContentHash);
            Id = rowKey;
            Timestamp = newsletter.Timestamp.Value.UtcDateTime;
            SendDate = newsletter.SendDate;
        }

        private Dictionary<Language, string> fitlerText(Dictionary<Language, string> dict, IEnumerable<Language> languages)
        {
            return dict.Where(text => languages.Contains(text.Key) && !string.IsNullOrWhiteSpace(text.Value)).ToDictionary(i => i.Key, i => i.Value);
        }

        public Newsletter FilterResultsBySettings(Settings settings)
        {
            if (settings == null || !settings.Languages.Any())
            {
                throw new ArgumentNullException(nameof(settings));
            }
            _Name = fitlerText(_Name, settings.Languages);
            return this;
        }

        public Newsletter(Dictionary<Language, string> name)
        {
            Id = Guid.NewGuid();
            _Name = name;
        }

        public Newsletter(NewsletterDto message)
        {
            var name = message.Name == null ? new Dictionary<Language, string>() : message.Name;
            var author = message.Author == null ? new Dictionary<Language, string>() : message.Author;

            setNewsletter(name, author);
            if (message.Id != default)
            {
                Id = message.Id;
                Timestamp = message.Timestamp == default ? DateTime.UtcNow.ToUniversalTime() : message.Timestamp;
                SendDate = message.SendDate;
            }
        }

        public void UpdateSendDate()
        {
            SendDate = DateTime.Now.ToUniversalTime();
        }

        public void UpdateNameForLanguage(string name, Language lang)
        {
            _Name[lang] = name;
        }


        public void UpdateAuthorForLanguage(string author, Language lang)
        {
            _Author[lang] = author;
        }

        public void UpdateContentHashForLanguage(string hash, Language lang)
        {
            _ContentHash[lang] = hash;
        }

        public string GetTitles()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Name != null && _Name.Any())
            {
                foreach (var name in _Name.Where(n => n.Value != null && !string.IsNullOrWhiteSpace(n.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), name.Key), name.Value == null ? "" : name.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetAuthor()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Author != null && _Author.Any())
            {
                foreach (var name in _Author.Where(author => author.Value != null && !string.IsNullOrWhiteSpace(author.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), name.Key), name.Value == null ? "" : name.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }
        public string GetContentHash()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_ContentHash != null && _ContentHash.Any())
            {
                foreach (var hash in _ContentHash.Where(hash => hash.Value != null && !string.IsNullOrWhiteSpace(hash.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), hash.Key), hash.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }
    }
}
