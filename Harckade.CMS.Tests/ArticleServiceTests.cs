using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;
using Harckade.CMS.Azure.Enums;
using Harckade.CMS.Azure.Mappers;
using Harckade.CMS.Azure.Repository;
using Harckade.CMS.Services;
using Harckade.CMS.Services.Abstractions;
using Harckade.CMS.Utils;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Harckade.CMS.Tests
{
    [TestClass]
    public class ArticleServiceTests
    {
        private IArticleService _articleService;
        private IArticleBackupService _articleBackupService;
        private FunctionContext _functionContext;
        private string _prefix;
        private IDtoArticleMapper _dtoArticleMapper;

        [TestInitialize]
        public void init()
        {
            var connectionString = "UseDevelopmentStorage=true";
            var services = new ServiceCollection();
            var path = Path.Combine(Environment.CurrentDirectory, "local.settings.json");
            var configurationFile = System.IO.File.ReadAllText(path);
            var fileJson = JsonConvert.DeserializeObject<LocalSettings>(configurationFile);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(fileJson.Values).Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IBlobRepository, BlobRepository>(s => new BlobRepository(connectionString));
            services.AddScoped<IArticleRepository, ArticleRepository>();
            services.AddScoped<IArticleBackupRepository, ArticleBackupRepository>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<IArticleHelperRepository, ArticleHelperRepository>();
            services.AddScoped<IJournalRepository, JournalRepository>();
            services.AddScoped<IJournalService, JournalService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IArticleBackupService, ArticleBackupService>();
            services.AddScoped<IArticleService, ArticleService>();
            services.AddScoped<IDtoJournalMapper, DtoJournalMapper>();
            services.AddScoped<IDtoArticleMapper, DtoArticleMapper>();
            services.AddScoped<IDtoArticleBackupMapper, DtoArticleBackupMapper>();
            services.AddScoped<IDtoUserMapper, DtoUserMapper>();
            services.AddScoped<IDtoFileObjectMapper, DtoFileObjectMapper>();
            services.AddScoped<IDtoSettingsMapper, DtoSettingsMapper>();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            _articleService = serviceProvider.GetService<IArticleService>();
            _articleBackupService = serviceProvider.GetService<IArticleBackupService>();
            _dtoArticleMapper = serviceProvider.GetService<IDtoArticleMapper>();
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "|")}";
            var context = new Mock<FunctionContext>();
            _functionContext = context.Object;
        }

        private async Task<Article> AddNewArticle()
        {
            ArticleDto article = new ArticleDto();
            article.Name = new Dictionary<Language, string>() { { Language.En, $"{_prefix}_English title" }, { Language.Pt, $"{_prefix}_Título em Português" }, { Language.Ru, $"{_prefix}_Название на Русском" } };
            var newArticle = await _articleService.AddOrUpdateArticle(_functionContext, article);
            Assert.IsTrue(newArticle.Success);
            Assert.IsNotNull(newArticle.Value);
            Assert.IsTrue(newArticle.Value.Id != default);
            return newArticle.Value;
        }

        private async Task<Article> UpdateArticle(Article article)
        {
            var updatedArticleDto = _dtoArticleMapper.DocumentToDto(article);
            var updatedName = $"{_prefix}_English title updated";
            updatedArticleDto.Name = new Dictionary<Language, string>() { { Language.En, updatedName }, { Language.Pt, $"{_prefix}_Título em Português" }, { Language.Ru, $"{_prefix}_Название на Русском" } };
            var updatedArticle = await _articleService.AddOrUpdateArticle(_functionContext, updatedArticleDto, Language.En);
            Assert.IsTrue(updatedArticle.Success);
            Assert.IsNotNull(updatedArticle.Value);
            Assert.AreEqual(article.Id, updatedArticle.Value.Id);
            Assert.AreNotEqual(article.Name[Language.En], updatedArticle.Value.Name[Language.En]);
            Assert.AreEqual(article.Name[Language.Pt], updatedArticle.Value.Name[Language.Pt]);
            Assert.AreEqual(updatedArticle.Value.Name[Language.En], updatedName);
            return updatedArticle.Value;
        }

        [TestMethod]
        public async Task TestAddOrUpdateArticle()
        {
            //Create
            var newArticle = await AddNewArticle();
            Thread.Sleep(3600);
            //Update
            await UpdateArticle(newArticle);
        }

        [TestMethod]
        public async Task RestoreBackupArticle()
        {
            var newArticle = await AddNewArticle();
            Thread.Sleep(3600);
            var article = await UpdateArticle(newArticle);
            var updatedArticleDto = _dtoArticleMapper.DocumentToDto(article);
            updatedArticleDto.Name = new Dictionary<Language, string>() { { Language.En, $"{_prefix}_English title updated 2" }, { Language.Pt, $"{_prefix}_Título em Português" }, { Language.Ru, $"{_prefix}_Название на Русском" } };
            Thread.Sleep(3600);
            var updatedArticle = await _articleService.AddOrUpdateArticle(_functionContext, updatedArticleDto, Language.En);
            Assert.IsTrue(updatedArticle.Success);
            Assert.IsNotNull(updatedArticle.Value);
            Assert.AreEqual(article.Id, updatedArticle.Value.Id);

            var restorationPointsResult = await _articleBackupService.GetById(newArticle.Id, Language.En);
            Assert.IsTrue(restorationPointsResult.Success);
            var restorationPoints = restorationPointsResult.Value;
            Assert.IsNotNull(restorationPoints);
            Assert.AreEqual(restorationPoints.Count(), 3);
            var result = await _articleService.RestoreBackupArticle(_functionContext, newArticle.Id, Language.En, restorationPoints.OrderBy(r => r.ModificationDate).FirstOrDefault().ModificationDate);
            Assert.IsTrue(result.Success);
            var restoredArticle = result.Value;
            Assert.IsNotNull(restoredArticle);
            Assert.AreEqual(newArticle.Name[Language.En], restoredArticle.Name[Language.En]);
        }

        [TestMethod]
        public async Task GetAll()
        {
            var newArticle = await AddNewArticle();
            Thread.Sleep(3600);
            var result = await _articleService.GetAll();
            Assert.IsTrue(result.Success);
            var articles = result.Value;
            Assert.IsNotNull(articles);
            Assert.IsTrue(articles.Any() && articles.Count() >= 1);
            Assert.IsNotNull(articles.FirstOrDefault(art => art.Id == newArticle.Id));
        }

        [TestMethod]
        public async Task GetLast()
        {
            await AddNewArticle();
            Thread.Sleep(3600);
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "|")}";
            var newArticle = await AddNewArticle();
            Thread.Sleep(3600);
            var result = await _articleService.GetLast(1);
            Assert.IsTrue(result.Success);
            var lastArticles = result.Value;
            Assert.IsNotNull(lastArticles);
            Assert.IsTrue(lastArticles.Any());
            var lastArticle = lastArticles.FirstOrDefault();
            Assert.AreEqual(lastArticle.Id, newArticle.Id);
        }

        [TestMethod]
        public async Task GetById()
        {
            var article = await AddNewArticle();
            Thread.Sleep(3600);
            var result = await _articleService.GetById(article.Id);
            Assert.IsTrue(result.Success);
            var sameArticle = result.Value;
            Assert.IsNotNull(sameArticle);
            Assert.AreEqual(sameArticle.Id, article.Id);
        }

        [TestMethod]
        public async Task GetArticleByTitle()
        {
            var article = await AddNewArticle();
            Thread.Sleep(3600);
            var title = article.NameNoDiacritics[Language.En];
            Assert.IsTrue(!string.IsNullOrWhiteSpace(title));
            var result = await _articleService.GetByTitle(title, Language.En);
            Assert.IsTrue(result.Success);
            var sameArticle = result.Value;
            Assert.IsNotNull(sameArticle);
            Assert.AreEqual(sameArticle.Id, article.Id);
        }

        [TestMethod]
        public async Task PublishUnpublish()
        {
            var article = await AddNewArticle();
            Assert.IsFalse(article.Published);
            Thread.Sleep(3600);
            await _articleService.PublishUnpublish(article.Id);
            Thread.Sleep(3600);
            var result = await _articleService.GetById(article.Id);
            Assert.IsTrue(result.Success);
            var updatedArticle = result.Value;
            Assert.IsNotNull(updatedArticle);
            Assert.AreEqual(updatedArticle.Id, article.Id);
            Assert.IsTrue(updatedArticle.Published);
        }

        [TestMethod]
        public async Task ListPublishedArticles()
        {
            var article = await AddNewArticle();
            Thread.Sleep(3600);
            await _articleService.PublishUnpublish(article.Id);
            Thread.Sleep(3600);
            _prefix = $"unit_test_{DateTime.UtcNow.ToUniversalTime().ToString().Replace("/", "|")}";
            var otherArticle = await AddNewArticle();
            Thread.Sleep(3600);
            await _articleService.PublishUnpublish(otherArticle.Id);

            var result = await _articleService.GetAvailableArticles();
            Assert.IsTrue(result.Success);
            var articles = result.Value;
            Assert.IsNotNull(articles);
            Assert.IsTrue(articles.Any());
            Assert.IsTrue(articles.Where(art => art.Id == article.Id).Any());
            Assert.IsTrue(articles.Where(art => art.Id == otherArticle.Id).Any());
        }

        [TestMethod]
        public async Task UploadAndDownloadArticleBinary()
        {
            var article = await AddNewArticle();
            Thread.Sleep(3600);
            var path = Path.Combine(Environment.CurrentDirectory, "test_files\\article.txt");
            var content = System.IO.File.ReadAllText(path);
            Assert.IsNotNull(content);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(content));
            using (var stream = Html.GenerateStreamFromString(content))
            {
                await _articleService.UploadArticleBinary(article, stream, Language.En);
            }
            Thread.Sleep(3600);
            var result = await _articleService.DownloadArticleBinary(article, Language.En);
            Assert.IsTrue(result.Success);
            var binary = result.Value;
            Assert.IsNotNull(binary);
            Assert.IsTrue(binary.Length > 0);
            string text = string.Empty;
            using (var reader = new StreamReader(binary))
            {
                text = reader.ReadToEnd();
            }
            Assert.IsTrue(text.Length > 0);
        }

        [TestMethod]
        public async Task<Article> MarkArticleAsDeletedById()
        {
            var article = await AddNewArticle();
            Thread.Sleep(3600);
            await _articleService.MarkArticleAsDeletedById(article.Id);
            Thread.Sleep(3600);
            var result = await _articleService.GetById(article.Id);
            Assert.IsTrue(result.Success);
            var updatedArticle = result.Value;
            Assert.IsNotNull(updatedArticle);
            Assert.IsTrue(updatedArticle.MarkedAsDeleted);
            Assert.IsTrue(updatedArticle.MarkedAsDeletedDate != default);
            return updatedArticle;
        }

        [TestMethod]
        public async Task GetArticlesMarkedAsDeleted()
        {
            await MarkArticleAsDeletedById();
            Thread.Sleep(3600);
            var result = await _articleService.GetArticlesMarkedAsDeleted();
            Assert.IsTrue(result.Success);
            var articlesMarkedAsDeleted = result.Value;
            Assert.IsNotNull(articlesMarkedAsDeleted);
            Assert.IsTrue(articlesMarkedAsDeleted.Any());
            foreach (var article in articlesMarkedAsDeleted)
            {
                Assert.IsTrue(article.MarkedAsDeleted);
                Assert.IsTrue(article.MarkedAsDeletedDate != default);
            }
        }

        [TestMethod]
        public async Task RecoverArticleFromDeletedById()
        {
            var article = await MarkArticleAsDeletedById();
            Thread.Sleep(3600);
            await _articleService.RecoverArticleFromDeletedById(article.Id);
            Thread.Sleep(3600);
            var result = await _articleService.GetById(article.Id);
            Assert.IsTrue(result.Success);
            var updatedArticle = result.Value;
            Assert.IsNotNull(updatedArticle);
            Assert.IsFalse(updatedArticle.MarkedAsDeleted);
        }

        [TestMethod]
        public async Task DeleteArticletById()
        {
            var article = await MarkArticleAsDeletedById();
            Thread.Sleep(3600);
            await _articleService.DeleteArticleById(article.Id);
            Thread.Sleep(3600);
            var deletedArticle = await _articleService.GetById(article.Id);
            Assert.IsTrue(deletedArticle.Failed);
        }

        [TestMethod]
        public async Task ClearAfterTests()
        {
            var result = await _articleService.GetAll();
            Assert.IsTrue(result.Success);
            var articles = result.Value;
            Thread.Sleep(3600);
            foreach (var article in articles.Where(a => a.Name[Language.En].StartsWith("unit_test_")))
            {
                await _articleService.MarkArticleAsDeleted(article);
                await _articleService.DeleteArticle(article);
            }
        }
    }
}
