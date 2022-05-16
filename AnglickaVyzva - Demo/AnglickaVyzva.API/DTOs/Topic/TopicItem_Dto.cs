using AnglickaVyzva.API.Entities;
using System;
using System.Collections.Generic;

namespace AnglickaVyzva.API.DTOs.Topic
{
    public class TopicItem_Dto
    {
        public override string ToString()
        {
            return Cz;
        }

        public int Id { get; set; }
        public int TopicSetId { get; set; }
        public List<TopicItem_En> EnList { get; set; }
        public string Cz { get; set; }
        public string NoteCz { get; set; }
        public List<string> PronunciationEnList { get; set; }
        public List<string> PronunciationCzList { get; set; }

        // Pomocne - Pro kazdeho uzivatele je to jine
        public bool? DontKnow { get; set; }
        public double? Score { get; set; }

        public DateTime? LastUpdatedDate { get; set; }
    }
}
