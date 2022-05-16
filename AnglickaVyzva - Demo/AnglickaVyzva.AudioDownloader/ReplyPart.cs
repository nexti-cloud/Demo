using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnglickaVyzva.AudioDownloader
{
    public class ReplyPart
    {
        public override string ToString()
        {
            return OriginalText;
        }

        public string OriginalText { get; set; }
        public List<string> Options { get; set; }
        public int CorrectIndex { get; set; }

        public ReplyPart(string originalText)
        {
            OriginalText = originalText;
            Options = new List<string>();

            // Pouze jedna moznost bez chlupatych zavorek
            if (!OriginalText.Contains("{"))
            {
                Options.Add(OriginalText);
            }
            else
            {
                string text = OriginalText.Trim();
                var splitsByCurly = Regex.Split(text, "(?={)"); //// "Amber ", "{isn't/is not} my niece but she ", "{'s/is} my granddaughter."

                List<string> splits = new List<string>();
                for(int i=0; i < splitsByCurly.Length; i++)
                {
                    if(splitsByCurly[i].Contains("{")) //Obsahuje chlupatou  ->   "{isn't/is not} my niece but she "
                    {
                        //Neumim udelat, aby se zaviraci chlupata pridala k predchozimu retezci, proto to delam takhle slozite
                        var splitsEndCurly = Regex.Split(splitsByCurly[i], "(})"); // {isn't/is not", "}", " my niece but she "    nebo   "{'s/is", "}", " my granddaughter."
                        for(int j=0; j < splitsEndCurly.Length; j++)
                        {
                            if(splitsEndCurly[j].StartsWith("{")) // "{'s/is", "}"
                            {
                                splits.Add(splitsEndCurly[j] + "}");
                            }
                            else // " my granddaughter."   nebo   "}"
                            {
                                if (splitsEndCurly[j] == "}") //Neumim napsat regular, aby to nevyhazovalo koncove zavorky zvlast
                                    continue;
                                else
                                    splits.Add(splitsEndCurly[j]);
                            }
                        }
                    }
                    else //Neobsahuje chlupatou   ->  "Amber "
                    {
                        splits.Add(splitsByCurly[i]);
                    }
                }


                List<List<string>> sourceArray = new List<List<string>>();
                for (int i = 0; i < splits.Count; i++)
                {
                    List<string> iLevel = new List<string>();
                    if(!splits[i].StartsWith("{")) //Nema chlupate -> jenom jedna moznost
                    {
                        iLevel.Add(splits[i]);
                    }
                    else //Ma chlupate -> vice moznosti
                    {
                        var slashSplits = splits[i].Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(var part in slashSplits)
                        {
                            iLevel.Add(part.Replace("{", "").Replace("}", ""));
                        }
                    }
                    sourceArray.Add(iLevel);
                }

                List<string> outputArray = new List<string>();
                MakeAllPossibilities(outputArray, sourceArray);
                Options = outputArray;
                for(int i=0; i < Options.Count; i++)
                {
                    Options[i] = Options[i].Replace(" '", "'"); // ODSTRANIM MEZERU PRED APOSTROFEM
                    if(Options[i].Contains("°"))
                    {
                        Options[i] = Options[i].Replace("°", "");
                        CorrectIndex = i;
                    }
                }
            }

            
        }

        void MakeAllPossibilities(List<string> outputArray, List<List<string>> sourceArray, string actualPrefix = null, int? i = null, int? j = null)
        {
            if(actualPrefix == null)
            {
                actualPrefix = "";
                i = 0;
                j = 0;
            }

            string text = actualPrefix + sourceArray[(int)i][(int)j];

            // Pustim funkci na polozku ze stejneho I indexu na J index o 1 vetsi:
            // [i][j+1]
            // Takhle se postupne projdou vsechny polozky na stejne pozici, ktere jsou pod sebou.
            if (sourceArray[(int)i].Count > j + 1)
            {
                MakeAllPossibilities(outputArray, sourceArray, actualPrefix /*jsme na stejne urovni -> mame stejny prefix*/, i, j + 1);
            }

            if (sourceArray.Count > i + 1 && sourceArray[(int)i + 1].Count > 0)
            {
                MakeAllPossibilities(outputArray, sourceArray, text /*postupuji na dalsi index -> maji muj text jako prefix*/, i + 1, 0);
            }

            //Jsem na poslednim indexu pole -> pridam text do vystupniho pole
            if (i + 1 >= sourceArray.Count)
            {
                outputArray.Add(text);
            }
        }
    }
}
