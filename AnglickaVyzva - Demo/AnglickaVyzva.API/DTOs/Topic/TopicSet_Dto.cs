using System.Collections.Generic;

namespace AnglickaVyzva.API.DTOs.Topic
{
    public class TopicSet_Dto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public List<TopicItem_Dto> TopicItems { get; set; }

        public double? PerfectKnowledgeCount { get; set; }
        public double? GoodKnowledgeCount { get; set; }
        public double? BadKnowledgeCount { get; set; }
        public double? NotPlayedCount { get; set; }
    }
}
