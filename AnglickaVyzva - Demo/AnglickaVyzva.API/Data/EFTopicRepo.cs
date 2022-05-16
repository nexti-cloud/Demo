using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnglickaVyzva.API.Data
{
    public class EFTopicRepo : BaseRepo<Topic>
    {
        public EFTopicRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
