using System.ComponentModel.DataAnnotations.Schema;

namespace AnglickaVyzva.API.Entities
{
    public class TopicItem_User : BaseEntity
    {
        public override string ToString()
        {
            return $"TopicItemId: {TopicItemId}, UserId: {UserId}, DontKnow: {DontKnow}, Score: {Score}";
        }

        [ForeignKey("TopicItem")]
        public int TopicItemId { get; set; }
        public TopicItem TopicItem { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        /// <summary>
        /// Tohle slovicko neumi
        /// </summary>
        public bool DontKnow { get; set; }
        public double Score { get; set; }
    }
}
