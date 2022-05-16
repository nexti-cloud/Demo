using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class ChestPointsUse : BaseEntity
    {
        public DateTime Created { get; set; }
        public int Points { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
