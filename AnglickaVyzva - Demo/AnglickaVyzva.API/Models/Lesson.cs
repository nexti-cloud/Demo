using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class Lesson
    {
        public override string ToString()
        {
            return $"Order: {Order}";
        }

        public int Order { get; set; }
        public string ContentVocabulary { get; set; }
        public string ContentGrammar { get; set; }

        public List<Section> Sections { get; set; }
        public List<Test> Tests { get; set; } = new List<Test>();


        public Lesson()
        {
            Sections = new List<Section>();
        }
    }
}
