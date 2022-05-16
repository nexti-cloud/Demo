using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnglickaVyzva.API.Data
{
    public class EFTopicSetRepo : BaseRepo<TopicSet>
    {
        public EFTopicSetRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
