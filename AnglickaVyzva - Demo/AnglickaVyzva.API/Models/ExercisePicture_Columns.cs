using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExercisePicture_Columns : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "picture_columns";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<string> ColumnList { get; set; }
        public List<ItemPictureColumns> ItemList { get; set; }

        //      Člen neurčitý
        //      Tři sloupečky, objeví se slovo, musí se zařadit do sloupečku

        //      A           AN      -
        //	    teacher     apple   teachers
        //      student     actor   students
        //      hairdresser actress hairdressers

        public class ItemPictureColumns
        {
            public string Image { get; set; }
            public int ColumnID { get; set; }
            public string Translation { get; set; }
            public string Audio { get; set; }
        }

        public ExercisePicture_Columns(DataTable sheet, Section section, bool includeLocked)
        {
            if (sheet.Rows[2].ItemArray[0] != DBNull.Value)
            {
                try
                {
                    Points = Convert.ToInt32(sheet.Rows[2].ItemArray[0]);
                }
                catch
                {

                }
            }

            ColumnList = new List<string>();
            ItemList = new List<ItemPictureColumns>();

            NameCZ = sheet.Rows[0].ItemArray[0] == DBNull.Value ? "" : Convert.ToString(sheet.Rows[0].ItemArray[0]);

            // Obsahuje podnadpis
            if (NameCZ.Contains("~"))
            {
                var parts = NameCZ.Split('~');
                NameCZ = parts[0];
                Subtitle = parts[1];
            }

            Title = sheet.Rows[1].ItemArray[0] == DBNull.Value ? "" : Convert.ToString(sheet.Rows[1].ItemArray[0]);

            if (includeLocked == false)
                IsLock = sheet.TableName.Contains("{P}") ? true : false;

            if (IsLock)
                return;

            //Nazvy sloupcu
            for (int i = 1; i < sheet.Rows[2].ItemArray.Length; i++)
            {
                if (sheet.Rows[2].ItemArray[i] == DBNull.Value)
                    break;
                ColumnList.Add((string)sheet.Rows[2].ItemArray[i]);
            }

            for (int col = 0; col < ColumnList.Count; col++)
            {
                for (int row = 3; row < sheet.Rows.Count; row++)
                {
                    string word = sheet.Rows[row][col + 1] == DBNull.Value ? null : (string)sheet.Rows[row][col + 1];

                    if (word == null) //Skoncila slova ve sloupecku
                        break;

                    // Cesky preklad je oddelen procentem
                    var parts = word.Split(new[] { "%" }, StringSplitOptions.RemoveEmptyEntries);
                    word = parts[0];

                    ItemPictureColumns item = new ItemPictureColumns();
                    if (parts.Length > 1)
                        item.Translation = parts[1];


                    item.ColumnID = col;
                    item.Image = section.DataFolderPath + word + ".jpg";
                    item.Audio = word.Replace(" ", "_");
                    item.Audio = AudioHelper.CreateAudio(item.Audio);

                    ItemList.Add(item);
                }
            }
        }
    }
}
