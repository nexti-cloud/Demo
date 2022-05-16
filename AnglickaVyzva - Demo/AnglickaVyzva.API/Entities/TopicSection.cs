using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnglickaVyzva.API.Entities
{
    public class TopicSection : BaseEntity
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public string Icon { get; set; }
        public bool? IsHidden { get; set; }


        [ForeignKey("Topic")]
        public int TopicId { get; set; }
        public Topic Topic { get; set; }

        public List<TopicSet> TopicSets { get; set; }
    }
}
