using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Enums;
using Newtonsoft.Json;
using System.Linq;
using static Microsoft.Graph.Constants;

namespace Harckade.CMS.Azure.Domain
{
    public class Article
    {
        public Guid Id { get; private set; }
        public IReadOnlyDictionary<Language, string> Author { get { return _Author; } }
        public IReadOnlyDictionary<Language, string> Name { get { return _Name; } }
        public IReadOnlyDictionary<Language, string> NameNoDiacritics { get { return _NameNoDiacritics; } }
        public IReadOnlyDictionary<Language, string> Description { get { return _Description; } }
        public IReadOnlyDictionary<Language, string> ContentHash { get { return _ContentHash; } }
        public IReadOnlyDictionary<Language, string> ImageUrl { get { return _ImageUrl; } }
        public IReadOnlyDictionary<Language, string> ImageDescription { get { return _ImageDescription; } }
        public IReadOnlyDictionary<Language, IEnumerable<string>> Tags { get { return _Tags; } }
        public DateTime Timestamp { get; private set; }
        public DateTime PublishDate { get; private set; }
        public bool Published { get; private set; }
        public bool MarkedAsDeleted { get; private set; }
        public DateTime MarkedAsDeletedDate { get; private set; }

        private Dictionary<Language, string> _Author;
        private Dictionary<Language, string> _Name;
        private Dictionary<Language, string> _NameNoDiacritics;
        private Dictionary<Language, string> _Description;
        private Dictionary<Language, string> _ContentHash;
        private Dictionary<Language, string> _ImageUrl;
        private Dictionary<Language, string> _ImageDescription;
        private Dictionary<Language, IEnumerable<string>> _Tags;

        private IList<Language> cyrillicLanguages = new List<Language>() {
            Language.Ru, Language.Uk, Language.Be, Language.Bg, Language.Kk, Language.Ky, Language.Mk, Language.Mn, Language.Sr, Language.Tg, Language.Uz, Language.Ab, Language.Av, Language.Ce, Language.Cv, Language.Os, Language.Tt
        };

        private bool isNullDate(DateTime date)
        {
            return date == default || date == new DateTime(1601, 1, 1);
        }

        private void setArticle(Dictionary<Language, string> name, Dictionary<Language, string> description, Dictionary<Language, string> imageUrl, Dictionary<Language, string> imageDescription, Dictionary<Language, IEnumerable<string>> tags, Dictionary<Language, string> author, Dictionary<Language, string> contentHash = null)
        {
            Dictionary<Language, string> _name = new Dictionary<Language, string>();
            Dictionary<Language, string> _description = new Dictionary<Language, string>();
            Dictionary<Language, string> _author = new Dictionary<Language, string>();
            Dictionary<Language, string> _imageUrl = new Dictionary<Language, string>();
            Dictionary<Language, string> _imageDescription = new Dictionary<Language, string>();
            Dictionary<Language, IEnumerable<string>> _tags = new Dictionary<Language, IEnumerable<string>>();
            Dictionary<Language, string> _contentHash = new Dictionary<Language, string>();

            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang == Language.None)
                {
                    continue;
                }
                _name.Add(lang, !name.ContainsKey(lang) ? string.Empty : name[lang]);
                _description.Add(lang, !description.ContainsKey(lang) ? string.Empty : description[lang]);
                _author.Add(lang, !author.ContainsKey(lang) ? string.Empty : author[lang]);
                _imageUrl.Add(lang, !imageUrl.ContainsKey(lang) ? string.Empty : imageUrl[lang]);
                _imageDescription.Add(lang, !imageDescription.ContainsKey(lang) ? string.Empty : imageDescription[lang]);
                _tags.Add(lang, !tags.ContainsKey(lang) ? new List<string>() : tags[lang]);
                if (contentHash != null)
                {
                    _contentHash.Add(lang, !contentHash.ContainsKey(lang) ? string.Empty : contentHash[lang]);
                }

            }

            Id = Guid.NewGuid();
            _Name = _name;
            InitNoDiacritics();
            _Description = _description;
            _ContentHash = _contentHash;
            _ImageUrl = _imageUrl;
            _ImageDescription = _imageDescription;
            _Tags = _tags;
            MarkedAsDeleted = false;
            _Author = _author;
        }

        public Article(ArticleEntity article)
        {
            Guid rowKey = Guid.ParseExact(article.RowKey, "N");
            if (rowKey == default)
            {
                throw new ArgumentException(nameof(article.RowKey));
            }
            var _entityName = string.IsNullOrWhiteSpace(article.Name) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(article.Name);
            var _entityDescription = string.IsNullOrWhiteSpace(article.Description) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(article.Description);
            var _entityImageUrl = string.IsNullOrWhiteSpace(article.ImageUrl) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(article.ImageUrl);
            var _entityImageDescription = string.IsNullOrWhiteSpace(article.ImageDescription) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(article.ImageDescription);
            var _entityTags = string.IsNullOrWhiteSpace(article.Tags) ? new Dictionary<Language, IEnumerable<string>>() : (Dictionary<Language, IEnumerable<string>>)JsonConvert.DeserializeObject<Dictionary<Language, IEnumerable<string>>>(article.Tags);
            var _entityAuthor = string.IsNullOrWhiteSpace(article.Author) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(article.Author);
            var _entityContentHash = string.IsNullOrWhiteSpace(article.ContentHash) ? new Dictionary<Language, string>() : (Dictionary<Language, string>)JsonConvert.DeserializeObject<Dictionary<Language, string>>(article.ContentHash);
            setArticle(_entityName, _entityDescription, _entityImageUrl, _entityImageDescription, _entityTags, _entityAuthor, _entityContentHash);
            InitNoDiacritics();
            Id = rowKey;
            Timestamp = article.Timestamp.Value.UtcDateTime;
            PublishDate = article.PublishDate;
            Published = article.Published;
            MarkedAsDeleted = article.MarkedAsDeleted;
            MarkedAsDeletedDate = article.MarkedAsDeletedDate;
        }

        private Dictionary<Language, string> fitlerText(Dictionary<Language, string> dict, IEnumerable<Language> languages)
        {
            return dict.Where(text => languages.Contains(text.Key) && !string.IsNullOrWhiteSpace(text.Value)).ToDictionary(i => i.Key, i => i.Value);
        }

        public Article FilterResultsBySettings(Settings settings)
        {
            if (settings == null || !settings.Languages.Any())
            {
                throw new ArgumentNullException(nameof(settings));
            }
            _ImageDescription = fitlerText(_ImageDescription, settings.Languages);
            _Description = fitlerText(_Description, settings.Languages);
            _NameNoDiacritics = fitlerText(_NameNoDiacritics, settings.Languages);
            _Name = fitlerText(_Name, settings.Languages);
            _ImageUrl = fitlerText(_ImageUrl, settings.Languages);
            _Tags = Tags.Where(text => settings.Languages.Contains(text.Key) && text.Value.Any()).ToDictionary(i => i.Key, i => i.Value);
            return this;
        }

        public Article(Dictionary<Language, string> name)
        {
            Id = Guid.NewGuid();
            _Name = name;
            InitNoDiacritics();
        }

        public Article(ArticleDto article)
        {
            var name = article.Name == null ? new Dictionary<Language, string>() : article.Name;
            var description = article.Description == null ? new Dictionary<Language, string>() : article.Description;
            var imageUrl = article.ImageUrl == null ? new Dictionary<Language, string>() : article.ImageUrl;
            var imageDescription = article.ImageDescription == null ? new Dictionary<Language, string>() : article.ImageDescription;
            var tags = article.Tags == null ? new Dictionary<Language, IEnumerable<string>>() : article.Tags;
            var author = article.Author == null ? new Dictionary<Language, string>() : article.Author;

            setArticle(name, description, imageUrl, imageDescription, tags, author, article.HtmlContentIsLoaded ? article.HtmlContent : null);
            if (article.Id != default)
            {
                Id = article.Id;
                Timestamp = article.Timestamp == default ? DateTime.UtcNow.ToUniversalTime() : article.Timestamp;
                PublishDate = article.PublishDate;
                Published = article.Published;
                MarkedAsDeleted = article.MarkedAsDeleted;
                MarkedAsDeletedDate = article.MarkedAsDeleted == true ? article.MarkedAsDeletedDate : default;
            }
        }

        private void CheckIfIsDeleted()
        {
            if (MarkedAsDeleted)
            {
                throw new NotSupportedException("It is not possible to edit a deleted article");
            }
        }
        public void UpdateDescriptionForLanguage(string description, Language lang)
        {
            CheckIfIsDeleted();
            _Description[lang] = description;
        }

        public void UpdateNameForLanguage(string name, Language lang)
        {
            CheckIfIsDeleted();
            _Name[lang] = name;
            InitNoDiacritics();
        }

        public void UpdateImageUrlForLanguage(string imageUrl, Language lang)
        {
            CheckIfIsDeleted();
            _ImageUrl[lang] = imageUrl;
        }

        public void UpdateImageDescriptionForLanguage(string imageDescription, Language lang)
        {
            CheckIfIsDeleted();
            _ImageDescription[lang] = imageDescription;
        }

        public void UpdateTagsForLanguage(IEnumerable<string> tags, Language lang)
        {
            CheckIfIsDeleted();
            _Tags[lang] = tags;
        }

        public void UpdateAuthorForLanguage(string author, Language lang)
        {
            CheckIfIsDeleted();
            _Author[lang] = author;
        }

        public void UpdateContentHashForLanguage(string hash, Language lang)
        {
            CheckIfIsDeleted();
            _ContentHash[lang] = hash;
        }

        public void InitNoDiacritics()
        {
            Dictionary<Language, string> noDiacriticsName = new Dictionary<Language, string>();
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (!Name.ContainsKey(lang))
                {
                    continue;
                }
                var name =  Name[lang];
                var title = cyrillicLanguages.Contains(lang) ? NickBuhro.Translit.Transliteration.CyrillicToLatin(name) : name;
                noDiacriticsName.Add(lang, Diacritics.Extensions.StringExtensions.RemoveDiacritics(title.ToLowerInvariant().Trim()).Replace(" ", "-").Replace("`", "").Replace("?", "").Replace("!", "").Replace("&", "and").Replace(":", "").Replace(".", ""));
            }
            _NameNoDiacritics = noDiacriticsName;
        }

        public void PublishUnpublish()
        {
            if (Published == false && isNullDate(PublishDate))
            {
                PublishDate = DateTime.UtcNow.ToUniversalTime();
            }
            Published = !Published;
        }

        public void MarkAsDeleted()
        {
            if (MarkedAsDeleted == false)
            {
                MarkedAsDeletedDate = DateTime.UtcNow.ToUniversalTime();
                MarkedAsDeleted = true;
            }
        }

        public void UndoMarkAsDeleted()
        {
            if (MarkedAsDeleted == true)
            {
                MarkedAsDeletedDate = new DateTime(1601, 1, 1);
                MarkedAsDeleted = false;
            }
        }

        public string GetImageUrls()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_ImageUrl != null && _ImageUrl.Any())
            {
                foreach (var url in _ImageUrl.Where(url => url.Value != null && !string.IsNullOrWhiteSpace(url.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), url.Key), url.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetImageDescriptions()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_ImageDescription != null && _ImageDescription.Any())
            {
                foreach (var desc in _ImageDescription.Where(desc => desc.Value != null && !string.IsNullOrWhiteSpace(desc.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), desc.Key), desc.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetDescriptions()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Description != null && _Description.Any())
            {
                foreach (var desc in _Description.Where(desc => desc.Value != null && !string.IsNullOrWhiteSpace(desc.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), desc.Key), desc.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetTitles()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Name != null && _Name.Any())
            {
                foreach (var name in _Name.Where(name => name.Value != null && !string.IsNullOrWhiteSpace(name.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), name.Key), name.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetAuthor()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_Author != null && _Author.Any())
            {
                foreach (var name in _Author.Where(name => name.Value != null && !string.IsNullOrWhiteSpace(name.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), name.Key), name.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }

        public string GetTags()
        {
            Dictionary<string, IEnumerable<string>> result = new Dictionary<string, IEnumerable<string>>();
            if (_Tags != null && _Tags.Any())
            {
                foreach (var tag in _Tags.Where(t => t.Value != null && t.Value.Any()))
                {
                    result.Add(Enum.GetName(typeof(Language), tag.Key), tag.Value);
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

        public string GetTitlesNoDiacritics()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (_NameNoDiacritics != null && _NameNoDiacritics.Any())
            {
                foreach (var name in _NameNoDiacritics.Where(name => name.Value != null && !string.IsNullOrWhiteSpace(name.Value)))
                {
                    result.Add(Enum.GetName(typeof(Language), name.Key), name.Value);
                }
            }
            return JsonConvert.SerializeObject(result);
        }
    }
}
