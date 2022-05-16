using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFNotificationTokenRepo : BaseRepo<NotificationToken>
    {
        public EFNotificationTokenRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
