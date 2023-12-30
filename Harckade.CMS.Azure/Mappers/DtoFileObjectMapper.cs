using Harckade.CMS.Azure.Abstractions;
using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Mappers
{
    public class DtoFileObjectMapper : IDtoFileObjectMapper
    {
        public FileObjectDto DocumentToDto(FileObject file)
        {
            return new FileObjectDto()
            {
                FileType = Enum.GetName(file.FileType).ToLower(),
                Name = file.Name,
                Id = GetFolderDtoPath(file),
                Size = file.Size,
                Timestamp = file.Timestamp
            };
        }

        private string GetFolderDtoPath(FileObject file)
        {
            var folderParts = file.Id.ToString().Split('/');
            var filePath = $"{Enum.GetName(file.FileType).ToLower()}_{file.Name}";
            if (folderParts.Length > 1)
            {
                for (var i = folderParts.Length - 2; i >= 0; i--)
                {
                    filePath = $"{folderParts[i]}/{filePath}";
                }
            }
            return filePath;
        }
    }
}
