using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFChestPointsUsesRepo : BaseRepo<ChestPointsUse>
    {
        public EFChestPointsUsesRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }

        public async Task<List<ChestPointsUse>> GetChestPointsUses_Month(int userId, DateTime date)
        {
            var today = date.Date;

            var thisMonth_firstDay = new DateTime(today.Year, today.Month, 1);
            var nextMonth_firstDay = thisMonth_firstDay.AddMonths(1);

            var chestPointsUses = await All.Where(x => x.UserId == userId && x.Created >= thisMonth_firstDay && x.Created <= nextMonth_firstDay).ToListAsync();
            return chestPointsUses;
        }
    }
}
