using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Progress : BaseEntity
    {
        public string TemporarySession { get; set; } //Pro neprihlasene uzivatele
        public int LessonOrder { get; set; }
        public int SectionOrder { get; set; }
        public int ExerciseOrder { get; set; }
        public int? ExerciseSuborder { get; set; } //Cviceni Vocabulary ma jeste podcviceni
        public DateTime Created { get; set; }
        public int Points { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
