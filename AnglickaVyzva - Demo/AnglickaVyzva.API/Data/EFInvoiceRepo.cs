using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class EFInvoiceRepo : BaseRepo<Invoice>
    {
        public EFInvoiceRepo(DbContext context, SaveParameters saveParameters) : base(context, saveParameters)
        {
        }
    }
}
