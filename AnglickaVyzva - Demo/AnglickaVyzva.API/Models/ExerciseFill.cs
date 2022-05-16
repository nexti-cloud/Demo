using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseFill : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "fill";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemFill> ItemList { get; set; }

        public class ItemFill
        {
            public int ID { get; set; }
            public string Question { get; set; }
            public string Reply { get; set; }
            public string Audio { get; set; }
            public string Translation { get; set; }
        }

        public ExerciseFill(DataTable sheet, Section section, bool includeLocked)
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

            ItemList = new List<ItemFill>();

            NameCZ = (string)sheet.Rows[0].ItemArray[0];

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

            for (int i = 2; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                ItemFill item = new ItemFill();

                if (i > 3 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);
                item.Question = Convert.ToString(row.ItemArray[1]).Trim();
                item.Reply = row.ItemArray[2] == DBNull.Value ? "" : Convert.ToString(row.ItemArray[2]).Trim();
                item.Audio = row.ItemArray[3] == System.DBNull.Value ? item.Reply : Convert.ToString(row.ItemArray[3]);
                item.Audio = AudioHelper.CreateAudio(item.Audio);

                item.Translation = row.ItemArray[4] == DBNull.Value ? "" : Convert.ToString(row.ItemArray[4]).Trim();

                ItemList.Add(item);
            }
        }
    }
}
