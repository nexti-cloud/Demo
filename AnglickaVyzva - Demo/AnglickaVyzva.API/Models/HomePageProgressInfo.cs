using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class HomePageProgressInfo
    {
        public List<LessonInfo> LessonInfos { get; set; } = new List<LessonInfo>();
        public List<TopicInfo> TopicInfos { get; set; } = new List<TopicInfo>();
        public List<ChallengeInfo> ChallengeInfos { get; set; } = new List<ChallengeInfo>();

        public class LessonInfo
        {
            public int Order { get; set; }
            public int PercentageDone { get; set; }
            public bool IsOpen { get; set; }
            public string ContentVocabulary { get; set; }
            public string ContentGrammar { get; set; }
            public string[] ContentVocabularyLines { get; set; }
            public string[] ContentGrammarLines { get; set; }
        }

        public class TopicInfo
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public bool IsOpen { get; set; }
        }

        public class ChallengeInfo
        {
            public string Name { get; set; }
            public int Id { get; set; }

        }
    }
}
