using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnglickaVyzva.API.Data
{
    public class EFTopicPointsRepo : BaseRepo<TopicPoints>
    {
        public EFTopicPointsRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
