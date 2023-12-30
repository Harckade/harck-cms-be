using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Services.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Harckade.CMS.PublicController.Articles
{
    public class RecycleFunction
    {
        private IArticleService _articleService;
        private ILogger<RecycleFunction> _appInsights;
        private ObservabilityId _oid;

        public RecycleFunction(IArticleService articleService, ILogger<RecycleFunction> appInsights)
        {
            _oid = new ObservabilityId();
            _articleService = articleService;
            _articleService.UpdateOid(_oid);
            _appInsights = appInsights;
        }

        [Function("DeleteExpiredArticlesFunction")]
        public async Task Run([TimerTrigger("0 50 23 * * *")] MyInfo timer)
        {
            var currentTime = DateTime.UtcNow.ToUniversalTime();
            const long expirationDays = 30;
            _appInsights.LogInformation($"Recycle bin cycle executed: {currentTime}", _oid);
            var result = await _articleService.GetArticlesMarkedAsDeleted();
            if (result.Failed)
            {
                _appInsights.LogError($"Cannot retriev articles marked as deleted", _oid);
                return;
            }
            var articles = result.Value;
            foreach (var article in articles.Where(a => a.MarkedAsDeletedDate.AddDays(expirationDays) <= currentTime))
            {
                await _articleService.DeleteArticleById(article.Id);
            }
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
