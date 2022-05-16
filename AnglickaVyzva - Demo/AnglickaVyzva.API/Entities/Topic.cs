using System.Collections.Generic;

namespace AnglickaVyzva.API.Entities
{
    public class Topic : BaseEntity
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public string Icon { get; set; }
        public bool? IsHidden { get; set; }

        public List<TopicSection> TopicSections { get; set; }
    }
}
