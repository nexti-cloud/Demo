using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseVocabulary : IExercise
    {


        public List<ItemVocabulary> ItemList { get; private set; }

        public int Order { get; set; }
        public int Points { get; set; } = 10;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "vocabulary";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }
        public string Note { get; set; }

        public string Subtitle { get; set; }

        public class ItemVocabulary
        {
            public int ID { get; set; }
            public string EN { get; set; }
            public string CZ { get; set; }
            public string PronunciationEN { get; set; }
            public string PronunciationCZ { get; set; }
            public string Audio { get; set; }
            public bool IsUncountable { get; set; }
            public bool IsPlural { get; set; }
            public string NoteEN { get; set; }
            public string NoteCZ { get; set; }
        }

        public ExerciseVocabulary(DataTable sheet, Section section, bool includeLocked)
        {
            if(sheet.Rows[1].ItemArray[0] != DBNull.Value)
            {
                try
                {
                    //Points = Convert.ToInt32(sheet.Rows[1].ItemArray[0]);
                }
                catch
                {

                }
            }

            /*
            Věci okolo nás 1 / Things around us 1				
            ID	anglicky	výslovnost	česky	zvuk aj
            1	boy	        bɔɪ;boi	    kluk	
            2	girl	    ɡɜːl;grl	dívka	
            3	man	        mæn;mén	    muž	
            */
            string fullName = (string)sheet.Rows[0].ItemArray[0];
            string[] nameParts = fullName.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2)
            {
                NameCZ = nameParts[0].Trim();
                NameEN = nameParts[0].Trim();
            }
            else
            {
                NameCZ = nameParts[0].Trim();
                NameEN = nameParts[1].Trim();
            }

            // Obsahuje podnadpis
            if (NameCZ.Contains("~"))
            {
                var parts = NameCZ.Split('~');
                NameCZ = parts[0];
                Subtitle = parts[1];
            }


            if (includeLocked == false)
                IsLock = sheet.TableName.Contains("{P}") ? true : false;

            string[] sheetNameParts = sheet.TableName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Order = Convert.ToInt32(sheetNameParts[0]);

            ItemList = new List<ItemVocabulary>();

            if (IsLock)
                return;

            // Poznamka pod carou ke vsem slovickum
            if(sheet.Rows[0].ItemArray[1] != DBNull.Value)
            {
                Note = Convert.ToString(sheet.Rows[0].ItemArray[1]);
            }

            for (int i = 2; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                if (row.ItemArray[0] == DBNull.Value) //Prazdny radek. Nekdy to parsuje radky i kdyz jsou prazdne
                    break;

                ItemVocabulary item = new ItemVocabulary();
                item.ID = Convert.ToInt32(row.ItemArray[0]);
                item.EN = ((string)row.ItemArray[1]).Trim();

                //Vyslovnost
                if (row.ItemArray[2] == DBNull.Value) //Prazdne policko
                {
                    item.PronunciationCZ = null;
                    item.PronunciationEN = null;
                }
                else
                {
                    string[] pronParts = ((string)row.ItemArray[2]).Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    if (pronParts.Length == 2) //Je pritomna EN i CZ varianta
                    {
                        item.PronunciationEN = pronParts[0];
                        item.PronunciationCZ = pronParts[1];
                    }
                    else
                    {
                        item.PronunciationEN = pronParts[0];
                        item.PronunciationCZ = pronParts[0];
                    }

                    item.PronunciationEN = item.PronunciationEN.Trim();
                    item.PronunciationCZ = item.PronunciationCZ.Trim();
                }

                item.CZ = Convert.ToString(row.ItemArray[3]);
                
                //Pomnozne podstatne jmeno
                if (item.EN.Contains("##"))
                {
                    item.IsPlural = true;
                    item.EN = item.EN.Replace("##", "").Trim();
                }

                //Nepocitatelne podstatne jmeno
                if (item.EN.Contains("#"))
                {
                    item.IsUncountable = true;
                    item.EN = item.EN.Replace("#", "").Trim();
                }

                item.Audio = row.ItemArray[4] == System.DBNull.Value ? item.EN : (string)row.ItemArray[4];
                item.Audio = AudioHelper.CreateAudio(item.Audio);


                if (row.ItemArray.Length >= 6)
                {
                    item.NoteEN = row.ItemArray[5] == DBNull.Value ? null : (string)row.ItemArray[5];
                }
                if (row.ItemArray.Length >= 7)
                {
                    item.NoteCZ = row.ItemArray[6] == DBNull.Value ? null : (string)row.ItemArray[6];
                }

                ItemList.Add(item);

            }

        }
    }
}
