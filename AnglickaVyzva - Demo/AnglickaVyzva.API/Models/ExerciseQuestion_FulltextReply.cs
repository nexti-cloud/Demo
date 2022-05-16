using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseQuestion_FulltextReply : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "question_fulltextReply";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemQuestion_FulltextReply> ItemList { get; set; }

        public class ItemQuestion_FulltextReply
        {
            public int ID { get; set; }
            public string Question { get; set; }
            public string Reply { get; set; }
            public string Audio { get; set; }
            public string ReplyTranslation { get; set; }
        }

        public ExerciseQuestion_FulltextReply(DataTable sheet, Section section, bool includeLocked)
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

            ItemList = new List<ItemQuestion_FulltextReply>();

            if(sheet.Rows[0].ItemArray[0] == DBNull.Value)
            {
                HasError = true;
                return;
            }

            string fullName = (string)sheet.Rows[0].ItemArray[0];
            NameCZ = fullName.Replace("-", "‑"); // Nahrazeni pomlcky nedelitelnou pomlckou

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
                ItemQuestion_FulltextReply item = new ItemQuestion_FulltextReply();
                DataRow row = sheet.Rows[i];

                if(row.ItemArray.Length < 5) //Malo sloupecku
                {
                    HasError = true;
                    break;
                }

                if (i > 3 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                if (row.ItemArray[0] == DBNull.Value) //Neni doplneno IDCko
                {
                    HasError = true;
                    break;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);
                item.Question = Convert.ToString(row.ItemArray[1]).Trim();

                item.Reply = Convert.ToString(row.ItemArray[2]).Replace(";", "; ").Replace("  ", " ").Trim(); //Pridam Mezeru za konec vety. Kdyby tam uz mezera byla, tak odstranim dvojmezery
                item.Audio = row.ItemArray[3] == System.DBNull.Value ? null : (string)row.ItemArray[3];
                item.Audio = AudioHelper.CreateAudio(item.Audio);

                if (row.ItemArray.Length < 5) //Neni sloupecek s prekladem
                {
                    item.ReplyTranslation = string.Empty;
                    continue;
                }
                else
                {
                    item.ReplyTranslation = row.ItemArray[4] == System.DBNull.Value ? "" : Convert.ToString(row.ItemArray[4]).Trim();
                }

                ItemList.Add(item);
            }
            /*
                Napište množné číslo (-s)				
                ID	dotaz	odpoved	zvuk aj	preklad odpovedi
                1	table	tables	tables	stoly
                2	picture	pictures	pictures	obrazy
                3	car	    cars	cars	auta
                4	girl	girls	girls	dívky
                5	house	houses	houses	domy
                6	apple	apples	apples	jablka
                7	wall	walls	walls	zdi
*/

        }
    }
}
