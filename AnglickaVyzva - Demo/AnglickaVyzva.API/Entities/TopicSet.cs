using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnglickaVyzva.API.Entities
{
    public class TopicSet : BaseEntity
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public string Icon { get; set; }
        public bool? IsHidden { get; set; }

        [ForeignKey("TopicSection")]
        public int TopicSectionId { get; set; }
        public TopicSection TopicSection { get; set; }

        public List<TopicItem> TopicItems { get; set; }
    }
}
