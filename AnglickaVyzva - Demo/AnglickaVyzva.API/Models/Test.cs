using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class Test
    {
        public int Order { get; set; }
        
        public string Title { get; set; }
        public List<ItemTest> ItemList { get; set; } = new List<ItemTest>();

        public int PercentageDone { get; set; }
        public bool IsDone { get; set; }
        public bool IsOpen { get; set; }

        public string DataFolderPath { get; set; }

        public const int PercentageThreshold = 80;
        public const int PointsForSuccess = 25;


        public class ItemTest
        {
            public int ID { get; set; }
            public string Question { get; set; }
            public string Reply { get; set; }
            public string Audio { get; set; }
        }

        public Test() { }

        public Test(Lesson lesson, string filePath, DataTable sheet, int order)
        {
            Order = order;
            Title = (string)sheet.Rows[0].ItemArray[0];

            DataFolderPath = "Lekce " + lesson.Order + "/" + Path.GetFileNameWithoutExtension(filePath) + "/";

            for (int i=10; i < sheet.Rows.Count; i++) // Hlavicka zacina na radku 10 (cislovani od 1 v excelu) -> data jsou na radku 11 (cislovani od 1 v excelu)
            {
                var row = sheet.Rows[i];

                var item = new ItemTest();
                try
                {
                    if (row.ItemArray[0] == DBNull.Value)
                        break;

                    item.ID = Convert.ToInt32(row.ItemArray[0]);
                    item.Question = Convert.ToString(row.ItemArray[1]).Trim();

                    item.Reply = Convert.ToString(row.ItemArray[2]).Trim();
                    var replyParts = item.Reply.Split(new string[] { ";" }, StringSplitOptions.None);
                    item.Reply = "{" + string.Join("};{", replyParts) + "}";

                    if (row.ItemArray.Length < 4)
                    {
                        item.Audio = "";
                    }
                    else
                    {
                        item.Audio = row.ItemArray[3] == System.DBNull.Value ? "" : (string)row.ItemArray[3];
                        item.Audio = AudioHelper.CreateAudio(item.Audio);
                    }

                    ItemList.Add(item);
                }
                catch(Exception exc)
                {
                    i--;
                    continue;
                    throw exc;
                }
            }
        }
    }
}
