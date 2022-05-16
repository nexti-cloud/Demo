using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseDictation : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "dictation";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemDictation> ItemList { get; set; }

        public class ItemDictation
        {
            public int ID { get; set; }
            public string Audio { get; set; }
            public string Reply { get; set; }
            public string Translation { get; set; }
        }

        public ExerciseDictation(DataTable sheet, Section section, bool includeLocked)
        {
            if (sheet.Rows[1].ItemArray[0] != DBNull.Value)
            {
                try
                {
                    Points = Convert.ToInt32(sheet.Rows[1].ItemArray[0]);
                }
                catch
                {

                }
            }

            ItemList = new List<ItemDictation>();

            string fullName = (string)sheet.Rows[0].ItemArray[0];
            NameCZ = fullName;

            // Obsahuje podnadpis
            if (NameCZ.Contains("~"))
            {
                var parts = NameCZ.Split('~');
                NameCZ = parts[0];
                Subtitle = parts[1];
            }

            Title = NameCZ;

            if (includeLocked == false)
                IsLock = sheet.TableName.Contains("{P}") ? true : false;

            if (IsLock)
                return;

            for (int i=2; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                ItemDictation item = new ItemDictation();

                if(i > 3 && row.ItemArray[1] == DBNull.Value) //Parser vzal i prazdny radek
                {
                    break;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);
                item.Audio = Convert.ToString(row.ItemArray[1]).Trim();
                item.Audio = AudioHelper.CreateAudio(item.Audio);

                item.Reply = Convert.ToString(row.ItemArray[2]).Trim();
                item.Translation = Convert.ToString(row.ItemArray[3]);

                ItemList.Add(item);
            }
        }
    }
}
