using System.Collections.Generic;

namespace AnglickaVyzva.API.DTOs.Topic
{
    public class TopicSection_Dto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public int TopicId { get; set; }

        public List<TopicSet_Dto> TopicSets { get; set; }

        public double? KnownItems { get; set; }
    }
}
