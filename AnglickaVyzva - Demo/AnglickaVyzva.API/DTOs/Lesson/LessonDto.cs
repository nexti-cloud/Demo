using AnglickaVyzva.API.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs
{
    public class LessonDto
    {
        public int Order { get; set; }
        public string ContentVocabulary { get; set; }
        public string ContentGrammar { get; set; }

        public List<SectionDto> Sections { get; set; }
        public List<TestDto> Tests { get; set; }

        // Dopocitavane veci, nejsou v DB
        public bool? IsOpen { get; set; }
        public string[] ContentVocabularyLines { get; set; }
        public string[] ContentGrammarLines { get; set; }
        public int? PercentageDone { get; set; }
        //
    }
}
