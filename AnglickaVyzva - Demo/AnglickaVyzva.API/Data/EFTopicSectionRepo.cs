using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnglickaVyzva.API.Data
{
    public class EFTopicSectionRepo : BaseRepo<TopicSection>
    {
        public EFTopicSectionRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
