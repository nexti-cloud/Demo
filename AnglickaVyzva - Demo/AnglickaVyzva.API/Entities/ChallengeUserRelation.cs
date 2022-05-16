using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class ChallengeUserRelation : BaseEntity
    {
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }


        [ForeignKey("Challenge")]
        public int ChallengeId { get; set; }
        public Challenge Challenge { get; set; }

        public bool IsAdmin { get; set; }
        public bool HasLeft { get; set; }
    }
}
