using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseRows : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "rows";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemRows> ItemList { get; set; }


        public class ItemRows
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public List<string> Words { get; set; }
        }

        public ExerciseRows(DataTable sheet, Section section, bool includeLocked)
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
            ItemList = new List<ItemRows>();

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


            string rowTitle = null;

            for (int i = 1; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                

                if (i > 2 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                if(Convert.ToString(row.ItemArray[1]) == "Title")
                {
                    rowTitle = Convert.ToString(row.ItemArray[2]);
                    continue;
                }

                ItemRows item = new ItemRows();
                item.Words = new List<string>();
                item.Title = rowTitle;
                item.ID = Convert.ToInt32(row.ItemArray[0]);
                for (int j =1; j < row.ItemArray.Length; j++)
                {
                    string option = Convert.ToString(row.ItemArray[j]);

                    if (!String.IsNullOrWhiteSpace(option))
                        item.Words.Add(option);
                }

                ItemList.Add(item);
            }
        }
    }
}
