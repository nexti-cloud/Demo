using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnglickaVyzva.AudioDownloader
{
    public class SentenceHelper
    {
        public string Question { get; set; }
        public string Reply { get; set; }
        public List<QuestionPart> QuestionParts { get; set; } = new List<QuestionPart>();
        public List<ReplyPart> ReplyParts { get; set; } = new List<ReplyPart>();

        public SentenceHelper(string question, string reply)
        {
            Question = question;
            Reply = reply;

            ParseQuestion();
            ParseReply();
        }

        void ParseQuestion()
        {
            var qSplits = Regex.Split(Question, @"(_)");
            for(int i=0; i < qSplits.Length; i++)
            {
                if (qSplits[i] == "")
                    continue;
                QuestionParts.Add(new QuestionPart(qSplits[i]));
            }
        }

        void ParseReply()
        {
            var semicolonSplits = Reply.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            for(int i=0; i < semicolonSplits.Length; i++)
            {
                ReplyParts.Add(new ReplyPart(semicolonSplits[i]));
            }
        }
    }
}
