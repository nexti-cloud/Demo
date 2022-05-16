using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnglickaVyzva.API.Data
{
    public class EFTopicItemRepo : BaseRepo<TopicItem>
    {
        public EFTopicItemRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
