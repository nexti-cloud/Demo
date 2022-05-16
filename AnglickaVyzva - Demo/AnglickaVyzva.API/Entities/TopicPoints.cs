using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnglickaVyzva.API.Entities
{
    public class TopicPoints : BaseEntity
    {
        public int Points { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime Created { get; set; }
    }
}
