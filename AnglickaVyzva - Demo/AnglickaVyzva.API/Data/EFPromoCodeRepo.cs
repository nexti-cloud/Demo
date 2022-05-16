using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFPromoCodeRepo : BaseRepo<PromoCode>
    {
        public EFPromoCodeRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
