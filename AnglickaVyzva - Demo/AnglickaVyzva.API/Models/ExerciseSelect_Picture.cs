using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseSelect_Picture : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "select_picture";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemSelect_Picture> ItemList { get; set; }


        public class ItemSelect_Picture
        {
            public int ID { get; set; }
            public string Audio { get; set; }
            public string Image { get; set; }
            public List<string> Questions { get; set; } //Ta prvni je zaroven spravna odpoved
        }

        public ExerciseSelect_Picture(DataTable sheet, Section section, bool includeLocked)
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

            ItemList = new List<ItemSelect_Picture>();

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
                ItemSelect_Picture item = new ItemSelect_Picture();
                item.Questions = new List<string>();

                if (row.ItemArray.Length < 4)
                {
                    HasError = true;
                    return;
                }

                if (i > 3 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);

                item.Audio = row.ItemArray[1] == System.DBNull.Value ? "" : (string)row.ItemArray[1];
                item.Audio = AudioHelper.CreateAudio(item.Audio);

                item.Image = section.DataFolderPath + (string)row.ItemArray[2] + ".png";

                for (int col = 3; col < row.ItemArray.Length; col++)
                {
                    if (row.ItemArray[col] == DBNull.Value)
                        break;

                    string question = (string)row.ItemArray[col];
                    item.Questions.Add(question);
                }

                ItemList.Add(item);
            }
        }
    }

}