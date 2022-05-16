using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseConversation : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "conversation";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Image { get; set; }
        public string FooterSentense { get; set; }

        public List<string> People { get; set; } = new List<string>();// {"Tom","Jane", "Matt", "Kate"}
        public List<string> PeopleImages { get; set; } = new List<string>();

        public List<ItemConversationQuestion> ItemList { get; set; } = new List<ItemConversationQuestion>();
        public List<ItemConversationSentence> ItemSentenceList { get; set; } = new List<ItemConversationSentence>();

        public class ItemConversationQuestion
        {
            public int ID { get; set; }
            public string Question { get; set; }
            public string Reply { get; set; }
        }

        public class ItemConversationSentence
        {
            public int PersonOrder { get; set; } // Kolikaty clovek to je (napriklad Jane je 2.)
            public string Sentence { get; set; }
            public bool? IsTitle { get; set; }
            public string TitleText { get; set; }
        }

        public ExerciseConversation(DataTable sheet, Section section, bool includeLocked)
        {

            NameCZ = "Rozhovor";

            if (sheet.Rows[0].ItemArray[0] != DBNull.Value)
            {
                try
                {
                    Points = Convert.ToInt32(sheet.Rows[0].ItemArray[0]);
                }
                catch
                {

                }
            }

            Image = section.DataFolderPath + "rozhovor.png";

            Title = sheet.Rows[1].ItemArray[0] == DBNull.Value ? "" : (string)sheet.Rows[1].ItemArray[0];

            if (includeLocked == false)
                IsLock = sheet.TableName.Contains("{P}") ? true : false;

            if (IsLock)
                return;

            var peopleNamesRow = sheet.Rows[4];
            foreach(var col in peopleNamesRow.ItemArray)
            {
                if (col == DBNull.Value)
                    break;

                People.Add((string)col);
            }

            var peopleImagesRow = sheet.Rows[2];
            for(int i=0; i < People.Count; i++)
            {
                var image = peopleImagesRow.ItemArray[i].ToString();
                if(string.IsNullOrWhiteSpace(image) || image == "obrazek hlavy")
                {
                    image = People[i]+".jpg";
                }
                image = section.DataFolderPath + image;
                PeopleImages.Add(image);
            }

            int rowIndex = 5;
            while(true)
            {
                var row = sheet.Rows[rowIndex];

                // Misto tohoto radku vlozit dalsi nadpis. (Prvne jsou nekde a ted jsou treba tady, tak ukazuje jiny nadpis)
                if(row.ItemArray[0] != DBNull.Value && ((string)row.ItemArray[0]).Trim().StartsWith("~") )
                {
                    var sentence = new ItemConversationSentence();
                    sentence.IsTitle = true;
                    sentence.TitleText = ((string)row.ItemArray[0]).Replace("~", "");
                    ItemSentenceList.Add(sentence);
                    rowIndex++;
                    continue;
                }

                if (row.ItemArray[0] != DBNull.Value && (string)row.ItemArray[0] == "#")
                    break;

                for(int i=0; i < People.Count; i++)
                {
                    if (row.ItemArray.Length < i + 1)
                        break;

                    var col = row.ItemArray[i];
                    if(col != DBNull.Value)
                    {
                        var sentence = new ItemConversationSentence();
                        sentence.PersonOrder = i;
                        sentence.Sentence = (string)col;
                        ItemSentenceList.Add(sentence);
                    }
                }

                rowIndex++;
            }

            rowIndex++;
            FooterSentense = (string)sheet.Rows[rowIndex].ItemArray[0];
            rowIndex += 3; // Preskocim # a ID

            for(int i=rowIndex; i < sheet.Rows.Count; i++)
            {
                var row = sheet.Rows[i];
                if (row.ItemArray[0] == DBNull.Value)
                    break;

                ItemConversationQuestion item = new ItemConversationQuestion();
                item.ID = Convert.ToInt32(row.ItemArray[0]);
                item.Question = (string)row.ItemArray[1];
                item.Reply = (string)row.ItemArray[2];

                ItemList.Add(item);
            }

        }
    }
}
