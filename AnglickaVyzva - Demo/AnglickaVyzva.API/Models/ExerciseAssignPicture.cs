using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseAssignPicture : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "assign_picture";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemAssignPicture> ItemList { get; set; }

        public class ItemAssignPicture
        {
            public int ID { get; set; }
            public string First { get; set; }
            public string Image { get; set; }
        }

        /// <summary>
        /// dva sloupecky a priradit spravne vety ke spravnym obrazkum
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="section"></param>
        /// <param name="includeLocked"></param>
        public ExerciseAssignPicture(DataTable sheet, Section section, bool includeLocked)
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
            ItemList = new List<ItemAssignPicture>();

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

            for (int i = 2; i < sheet.Rows.Count; i++) // Pozor ExerciseAssing zacina na i = 3, protoze ma jeste title
            {
                DataRow row = sheet.Rows[i];
                ItemAssignPicture item = new ItemAssignPicture();

                if (i > 4 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);

                item.First = row.ItemArray[1] == System.DBNull.Value ? "" : Convert.ToString(row.ItemArray[1]);

                item.Image = ImageHelper.CreateImage(row.ItemArray[2], section);

                ItemList.Add(item);
            }
        }
    }
}
