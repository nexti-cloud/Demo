using System.Collections.Generic;

namespace AnglickaVyzva.API.DTOs.Topic
{
    public class Topic_Dto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public List<TopicSection_Dto> TopicSections { get; set; }

        public double? KnownItems { get; set; }
    }
}
