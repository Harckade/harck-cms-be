using Harckade.CMS.Azure.Domain;
using Harckade.CMS.Azure.Dtos;

namespace Harckade.CMS.Azure.Abstractions
{
    public interface IDtoFileObjectMapper
    {
        FileObjectDto DocumentToDto(FileObject file);
    }
}
