using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseFillTable : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "fillTable";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemFillTable> ItemList { get; set; }
        public List<string> ColumnNames { get; set; }


        public class ItemFillTable
        {
            public int ID { get; set; }
            public List<string> Columns { get; set; }
        }

        public ExerciseFillTable(DataTable sheet, Section section, bool includeLocked)
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
            ItemList = new List<ItemFillTable>();
            ColumnNames = new List<string>();

            //Neni vyplneny nadpis
            if(sheet.Rows[0].ItemArray[0] == DBNull.Value)
            {
                HasError = true;
                return;
            }

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

            //Nazvy sloupcu
            for (int i=1; i < sheet.Rows[2].ItemArray.Length; i++)
            {
                string colName = Convert.ToString(sheet.Rows[2].ItemArray[i]);
                if (colName == "")
                    break;
                ColumnNames.Add(colName);
            }

            for (int i = 3; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                ItemFillTable item = new ItemFillTable();

                if (i > 3 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                //Neni vyplnene ID
                if(row.ItemArray[0] == DBNull.Value)
                {
                    HasError = true;
                    return;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);
                item.Columns = new List<string>();

                for (int j=1; j < row.ItemArray.Length; j++)
                {
                    string str = Convert.ToString(row.ItemArray[j]);
                    if (str == "")
                        break;
                    item.Columns.Add(str);
                }

                ItemList.Add(item);
            }
        }
    }
}
