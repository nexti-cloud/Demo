using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseGame_Columns : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "game_columns";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Subtitle { get; set; }
        public List<string> ColumnList { get; set; }
        public List<ItemGame_Columns> ItemList { get; set; }

        //      Člen neurčitý
        //      Tři sloupečky, objeví se slovo, musí se zařadit do sloupečku

        //      A           AN      -
        //	    teacher     apple   teachers
        //      student     actor   students
        //      hairdresser actress hairdressers

        public class ItemGame_Columns
        {
            public string Word { get; set; }
            public int ColumnID { get; set; }
        }

        public ExerciseGame_Columns(DataTable sheet, Section section, bool includeLocked)
        {
            ColumnList = new List<string>();
            ItemList = new List<ItemGame_Columns>();

            NameCZ = (string)sheet.Rows[0].ItemArray[0];

            // Obsahuje podnadpis
            if (NameCZ.Contains("~"))
            {
                var parts = NameCZ.Split('~');
                NameCZ = parts[0];
                Subtitle = parts[1];
            }


            if (includeLocked == false)
                IsLock = sheet.TableName.Contains("{P}") ? true : false;

            if (IsLock)
                return;

            //Nazvy sloupcu
            for (int i=1; i < sheet.Rows[3].ItemArray.Length; i++)
            {
                ColumnList.Add((string)sheet.Rows[3].ItemArray[i]);
            }

            for(int col=0; col < ColumnList.Count; col++)
            {
                for(int row =4; row <sheet.Rows.Count; row++)
                {
                    string word = sheet.Rows[row][col + 1] == DBNull.Value ? null : (string)sheet.Rows[row][col + 1];

                    if (word == null) //Skoncila slova ve sloupecku
                        break;

                    ItemGame_Columns item = new ItemGame_Columns();
                    item.ColumnID = col;
                    item.Word = word;
                    ItemList.Add(item);
                }
            }
        }

    }
}
