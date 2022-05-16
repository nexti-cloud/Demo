using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{ 
    public class TopicItem
    {
        public override string ToString()
        {
            return $"{CZ} {EN} Knowledge: {Knowledge}";
        }

        public int ID { get; set; }
        public string EN { get; set; }
        public string CZ { get; set; }
        public string PronunciationEN { get; set; }
        public string PronunciationCZ { get; set; }
        public string Audio { get; set; }
        public bool IsUncountable { get; set; }
        public bool IsPlural { get; set; }
        public string NoteEN { get; set; }
        public string NoteCZ { get; set; }

        // Jak dobre umi toto slovicko (1-100) Pro kazdeho uzivatele je tato hodnota jina
        public int Knowledge { get; set; }
        public bool IsLocked { get; set; }
        public bool WasDone { get; set; } // Uz nekdy toto slovicko zkousel?

        public TopicItem MakeCopy()
        {
            return new TopicItem
            {
                ID = ID,
                EN = EN,
                CZ = CZ,
                PronunciationEN = PronunciationEN,
                PronunciationCZ = PronunciationCZ,
                Audio = Audio,
                IsUncountable = IsUncountable,
                IsPlural = IsPlural,
                NoteEN = NoteEN,
                NoteCZ = NoteCZ
            };
        }

        public TopicItem MakeLockedCopy()
        {
            return new TopicItem
            {
                ID = ID,
                CZ = CZ,

                IsLocked = true
            };
        }
    }

    
}
