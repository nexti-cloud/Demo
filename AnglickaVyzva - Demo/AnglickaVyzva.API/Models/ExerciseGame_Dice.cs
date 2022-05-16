using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    class ExerciseGame_Dice : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "game_dice";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string GameID { get; set; } //O jaky excel se jedna - excely se mirne lisi. Nejjednodussi zpracovani je poznacit si, ktery je ktery
        public List<Dice> DiceList { get; set; }

//      Číslovky
//      Hází se koustkou 1-10, k tomu se přiřadí slovo, musí se přeložit
//      kostka 1	    kostka 2	odpoved jednotne    odpoved mnozne
//      1; one          kluk        boy                 boys
//      2; two          dívka       girl                girls

        public class Dice
        {
            // 3 moznosti: cesky/anglicky mnozne/ anglicky jednotne
            public List<Tuple<string, string, string>> Sides { get; set; }

            public Dice()
            {
                Sides = new List<Tuple<string, string, string>>();
            }
        }

        public ExerciseGame_Dice(DataTable sheet, Section section, bool includeLocked)
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

            DiceList = new List<Dice>();
            
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

            GameID = Convert.ToString(sheet.Rows[2].ItemArray[1]);
            int diceNumber = 2;
            if (GameID == "3")
                diceNumber = 3;

            for(int i=0; i < diceNumber; i++)
            {
                Dice dice = new Dice();
                DiceList.Add(dice);
                for(int j=4; j < sheet.Rows.Count; j++)
                {
                    string val = sheet.Rows[j].ItemArray[i] == DBNull.Value ? null : Convert.ToString(sheet.Rows[j].ItemArray[i]);
                    if (val == null) //Skoncily hrany kostky
                        break;


                    Tuple<string, string, string> side;

                    string[] parts = val.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    if(parts.Length == 3) //Tri udaje na jedne strane
                    {
                        side = new Tuple<string, string, string>(parts[0], parts[1], parts[2]);
                    }
                    else //Dva udaje na jedne strane
                    {
                        side = new Tuple<string, string, string>(parts[0], parts[1], null);
                    }

                    dice.Sides.Add(side);
                }
            }
        }
    }
}
