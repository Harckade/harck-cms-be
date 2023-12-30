using Azure;
using Azure.Data.Tables;
using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Entities;
using Harckade.CMS.Azure.Mappers;
using Microsoft.Extensions.Configuration;

namespace Harckade.CMS.Azure.Repository
{
    public class SettingsRepository : BaseRepository, ISettingsRepository
    {
        private ISettingsMapper _mapper;
        public SettingsRepository(IConfiguration configuration) : base(configuration, "settings")
        {
            _mapper = new SettingsMapper();
        }

        public async Task<Settings> Get()
        {
            try
            {
                var result = await _tableClient.GetEntityAsync<SettingsEntity>("0", "0");
                return _mapper.EntityToDomain((SettingsEntity)result);
            }
            catch(Exception e)
            {
                if (e.Message.StartsWith("The specified resource does not exist"))
                {
                    return null;
                }
                throw e;
            }
            
        }

        public async Task InsertOrUpdate(Settings settings, bool ignoreDeploymentInfo = true)
        {
            var settingsEntity = _mapper.DomainToEntity(settings);
            var existingSettings = await Get();
            if (existingSettings == null)
            {
                await _tableClient.AddEntityAsync(settingsEntity);
            }
            else
            {
                var existingEntinty = _mapper.DomainToEntity(existingSettings);
                if (ignoreDeploymentInfo == true)
                {
                    settingsEntity.RequiresDeployment = existingEntinty.RequiresDeployment;
                    settingsEntity.LastDeploymentDate = existingEntinty.LastDeploymentDate;
                }

                await _tableClient.UpdateEntityAsync(settingsEntity, ETag.All, TableUpdateMode.Replace);
            }
        }
    }
}
