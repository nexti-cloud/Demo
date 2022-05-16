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
    public class Topic
    {
        public string Name { get; set; }
        public string Code { get; set; } // airport, restaurant...
        public int Order { get; set; }
        public List<TopicItem> Words { get; set; } = new List<TopicItem>();
        public List<TopicItem> Sentences { get; set; } = new List<TopicItem>();

        public Topic(string filePath, DataSet excel)
        {
            try
            {
                string[] nameParts = Path.GetFileNameWithoutExtension(filePath).Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                Order = Convert.ToInt32(nameParts[0]);
                Code = nameParts[1]; // airport, restaurant...

                for (int t = 0; t < 2; t++)
                {
                    var table = excel.Tables[t];

                    // Sesit se Slovickama
                    if (t == 0)
                    {
                        Name = Convert.ToString(table.Rows[0].ItemArray[0]);
                    }
                    // END Sesit se slovickama


                    var sheet = excel.Tables[t];

                    for (int i = 2; i < sheet.Rows.Count; i++) // 0 - nazev, 1 - hlavicka tabulky
                    {
                        var row = sheet.Rows[i];
                        if (row.ItemArray[0] == DBNull.Value) //Prazdny radek. Nekdy to parsuje radky i kdyz jsou prazdne
                            break;

                        TopicItem item = new TopicItem();
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


                        if (row.ItemArray.Length >= 7)
                        {
                            item.NoteEN = row.ItemArray[5] == DBNull.Value ? null : (string)row.ItemArray[5];
                            item.NoteCZ = row.ItemArray[6] == DBNull.Value ? null : (string)row.ItemArray[6];
                        }


                        // V prvnim sesite jsou slovica a v druhem sesite jsou vety
                        if(t == 0)
                        {
                            Words.Add(item);
                        }
                        else
                        {
                            Sentences.Add(item);
                        }
                    }


                }
            }
            catch (Exception exc)
            {

            }
        }

        public Topic() { }

        public Topic MakeCopyWithoutItems()
        {
            return new Topic
            {
                Code = Code,
                Name = Name,
                Order = Order
            };
        }
    }
}
