using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AnglickaVyzva.API.Entities
{
    public class TopicItem : BaseEntity
    {
        public override string ToString()
        {
            return Cz;
        }

        public int Order { get; set; }

        public string EnListStr { get; set; }
        [NotMapped]
        public List<TopicItem_En> EnList
        {
            get
            {
                var res = new List<TopicItem_En>();

                    var parts = EnListStr == null ? new List<string>() : EnListStr.Split("~;~", System.StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var part in parts)
                    {
                        var props = part.Split("~_~");

                        var order = int.Parse(props[0]);
                        var en = props[1];
                        var pronEn = props[2];
                        var pronCz = props[3];

                        res.Add(new TopicItem_En
                        {
                            Order = order,
                            En = en,
                            PronunciationEn = pronEn,
                            PronunciationCz = pronCz,
                        });
                    }

                return res;
            }
            set
            {
                var strs = value.Select(x => $"{x.Order}~_~{x.En}~_~{x.PronunciationEn}~_~{x.PronunciationCz}").ToList();

                EnListStr = string.Join("~;~", strs);
            }
        }

        public string Cz { get; set; }
        public string NoteCz { get; set; }

        public bool? IsPlural { get; set; }
        public bool? IsUncountable { get; set; }
        public bool? IsHidden { get; set; }


        public string PronunciationEnListStr { get; set; }
        [NotMapped]
        public List<string> PronunciationEnList
        {
            get
            {
                return PronunciationEnListStr?.Split("~;~").ToList();
            }
            set
            {
                PronunciationEnListStr = string.Join("~;~", value);
            }
        }

        public string PronunciationCzListStr { get; set; }
        [NotMapped]
        public List<string> PronunciationCzList
        {
            get
            {
                return PronunciationCzListStr?.Split("~;~").ToList();
            }
            set
            {
                PronunciationCzListStr = string.Join("~;~", value);
            }
        }

        [ForeignKey("TopicSet")]
        public int TopicSetId { get; set; }
        public TopicSet TopicSet { get; set; }

        public List<TopicItem_User> TopicItems_User { get; set; }
    }

    public class TopicItem_En
    {
        public int Order { get; set; }
        public string En { get; set; }
        public string PronunciationEn { get; set; }
        public string PronunciationCz { get; set; }
    }
}
