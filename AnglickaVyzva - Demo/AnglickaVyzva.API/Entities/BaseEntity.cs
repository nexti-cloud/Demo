using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedWith { get; set; }
        public string CreatedIp { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedWith { get; set; }
        public string LastUpdatedIP { get; set; }
        public int EntityState { get; set; }
        [Timestamp]
        public byte[] TimeStamp { get; set; }

        public static class EntityStates
        {
            public static readonly int Deleted = 5;
        }
    }
}
