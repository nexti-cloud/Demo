using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs
{
    public class DailyProgressDto
    {
        public int? Points { get; set; }
        /// <summary>
        /// Slouzi pro odsazeni zacatku mesice nebo pro volne misto na konci mesice. Aby se aplikaci lepe zobrazoval kalendar
        /// </summary>
        public bool? IsPlaceholder { get; set; }
        /// <summary>
        /// Dny, ktere se postup nepocital. Napriklad zacatek mesice jeste pred tim, nez si koupil aplikaci
        /// </summary>
        public bool? IsDisabled { get; set; }
        public bool? IsSuccess { get; set; }
        public bool? IsFail { get; set; }
        public bool? IsFuture { get; set; }
        public bool? IsToday { get; set; }

    }
}
