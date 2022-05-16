using AnglickaVyzva.API.Helpers;
using ExcelDataReader;
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
    public class Section
    {
        public override string ToString()
        {
            return (string.IsNullOrWhiteSpace(NameCZ) ? base.ToString() : NameCZ) + "[" + DataFolderPath + "]";
        }

        public int Order { get; set; } // Cislo/poradi
        public string NameCZ { get; set; }
        public string NameEN { get; set; }

        public string DataFolderPath { get; set; }
        public List<IExercise> Exercises { get; set; }
        public string Type { get; set; }
        public bool IsLock { get; set; }

        //// INFORMACE ZVLAST PRO KAZDEHO UZIVATELE - dopocitavaji se za behu pro kazdeho uzivatele zvlast
        public bool IsOpen { get; set; }
        public bool IsDone { get; set; }
        public double PercentageDone { get; set; }
        //// END INFORMACE ZVLAST PRO KAZDEHO UZIVATELE

        public Section() { }

        public Section(string filePath, Lesson lesson, bool includeLocked)
        {
            Exercises = new List<IExercise>();

            string FileName = filePath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).Last();
            string[] FileNameParts = FileName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            NameCZ = FileName.Replace(".xlsx", "").Replace("{P}","").Replace("hra ", "").Replace("grammar ","").Replace("vocabulary ","");
            NameCZ = NameCZ.Remove(0, NameCZ.IndexOf(' ') + 1);
            Order = Convert.ToInt32(FileNameParts[0]);
            Type = FileNameParts[1].Replace(".xlsx","");

            if(includeLocked == false)
                IsLock = filePath.Contains("{P}") ? true : false;

            // --- POZOR -  bylo to odkomentovane, ale ja potrebuju, aby se nacelt spravny nazev z prvniho cviceni v sekci. Takze budu nacitat i cviceni -> stejne pokud je cviceni zamknute, tak se do nej nenactou data
            // --- POZOR 2 - na konci odeberu vsechna cviceni ze sekce, pokud je sekce zamcena
            ////Pokud nemam nacitat zamknute, vratim se zpatky
            //if (IsLock && includeLocked == false)
            //    return;

            //DataFolderpath = baseURL + @"/Data/Data/Lekce " + lesson.Order + "/" + FileName.Replace(".xlsx", "") + "/";
            DataFolderPath = "Lekce " + lesson.Order + "/" + FileName.Replace(".xlsx", "") + "/"; //TADY

            FileStream fs = System.IO.File.Open(filePath, FileMode.Open);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet();
            fs.Close();

            foreach (DataTable sheet in result.Tables)
            {
                if(Exercises.Count > 0)
                    Exercises.Last().Order = Exercises.Count;

                string[] sheetNameParts = sheet.TableName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (sheetNameParts.Length < 2) //Neznamy format (List1)
                    continue;

                string exerciseType = sheetNameParts[1];
                exerciseType = ExcelHelper.RemoveDiacritics(exerciseType).ToLower();

                try
                {

                    switch (exerciseType)
                    {
                        case "slovicka":
                            Exercises.Add(new ExerciseVocabulary(sheet, this, includeLocked));
                            NameCZ = Exercises.Last().NameCZ;
                            NameEN = Exercises.Last().NameEN;
                            break;

                        case "video":
                            Exercises.Add(new ExerciseVideo(sheet, this, includeLocked));
                            NameCZ = Exercises.Last().NameCZ;
                            break;

                        case "dotaz_fulltextodpoved":
                            Exercises.Add(new ExerciseQuestion_FulltextReply(sheet, this, includeLocked));
                            break;

                        case "diktat":
                            Exercises.Add(new ExerciseDictation(sheet, this, includeLocked));
                            break;

                        case "diktat_dopln":
                            Exercises.Add(new ExerciseDictation_Fill(sheet, this, includeLocked));
                            break;

                        case "diktat_vyber":
                            Exercises.Add(new ExerciseDictation_Select(sheet, this, includeLocked));
                            break;

                        case "hra":
                            if (sheetNameParts.Length > 2)
                            {
                                string gameType = sheetNameParts[2];
                                switch (gameType)
                                {
                                    case "kostka":
                                        Exercises.Add(new ExerciseGame_Dice(sheet, this, includeLocked));
                                        NameCZ = Exercises.Last().NameCZ;
                                        break;

                                    case "sloupecky":
                                        Exercises.Add(new ExerciseGame_Columns(sheet, this, includeLocked));
                                        NameCZ = Exercises.Last().NameCZ;
                                        break;
                                }
                            }
                            break;

                        case "vyber":
                            Exercises.Add(new ExerciseSelect(sheet, this, includeLocked));
                            break;

                        case "vyber_obrazek_moznosti":
                            // Stejne jako ExerciseSelect, akorat je jeste pridany obrazek. Vybira se z moznosti. Moznosti jsou stejne pro cele cviceni.
                            Exercises.Add(new ExerciseSelect_PictureOptions(sheet, this, includeLocked));
                            break;

                        case "dopln_a_preloz":
                        case "preloz_zavorku":
                        case "doplnovacka":
                        case "doplnovacka_dve_odpovedi":
                            //slouci se a vznikne cviceni doplnovacka
                            Exercises.Add(new ExerciseFill(sheet, this, includeLocked));
                            break;

                        case "doplnovacka_obrazek":
                            // Obrazek, Veta s mistem na doplneni podle obrazku
                            Exercises.Add(new ExerciseFill_Picture(sheet, this, includeLocked));
                            break;

                        case "obrazek_fulltextodpoved":
                            Exercises.Add(new ExercisePicture_FulltextReply(sheet, this, includeLocked));
                            break;

                        case "klikacka":

                        case "obrazek_vyber":
                            //Zvuk - vybrat spravny obrazek
                            Exercises.Add(new ExercisePicture_Select(sheet, this, includeLocked));
                            break;

                        case "dotaz_vyber_obrazek":
                            // Nahore obrazek - vybrat ze slov
                            Exercises.Add(new ExerciseSelect_Picture(sheet, this, includeLocked));
                            break;

                        case "prirad":
                            //dva sloupecky a priradit vety jednu k druhe
                            Exercises.Add(new ExerciseAssign(sheet, this, includeLocked));
                            break;

                        case "prirad_kratky":
                            //dva sloupecky a priradit vety jednu k druhe
                            Exercises.Add(new ExerciseAssignShort(sheet, this, includeLocked));
                            break;

                        case "prirad_obrazek":
                            //dva sloupecky a priradit spravne vety ke spravnym obrazkum
                            Exercises.Add(new ExerciseAssignPicture(sheet, this, includeLocked));
                            break;

                        case "dve_odpovedi":
                            Exercises.Add(new ExerciseTwo_Sentences(sheet, this, includeLocked));
                            break;

                        case "poskladej_vety":
                            Exercises.Add(new ExerciseSort(sheet, this, includeLocked));
                            break;

                        case "klikni_dovety":
                            //Kliknout na spravne misto mezi slova (mezi slovy je tlacitko)
                            Exercises.Add(new ExerciseInsert(sheet, this, includeLocked));
                            break;

                        case "zarad_do_sloupce":
                            Exercises.Add(new ExerciseColumns(sheet, this, includeLocked));
                            break;

                        case "picture_columns":
                            Exercises.Add(new ExercisePicture_Columns(sheet, this, includeLocked));
                            break;

                        case "moznosti":
                            //Ve vete je jenom jedno misto na doplneni moznosti
                            Exercises.Add(new ExerciseOptions(sheet, this, includeLocked));
                            break;

                        case "rady":
                            Exercises.Add(new ExerciseRows(sheet, this, includeLocked));
                            break;

                        case "to_nebo_to":
                            Exercises.Add(new ExerciseDecision(sheet, this, includeLocked));
                            break;

                        case "prepis_vetu":
                            Exercises.Add(new ExerciseRewrite(sheet, this, includeLocked));
                            break;

                        case "dopln_tabulku":
                            Exercises.Add(new ExerciseFillTable(sheet, this, includeLocked));
                            break;

                        case "zvuk_vyber_obrazek":
                            //Ozve se zvuk a podle nej se vybere jeden ze ctyr obrazku
                            Exercises.Add(new ExerciseAudio_SelectPicture(sheet, this, includeLocked));
                            break;

                        case "rozhovor":
                            NameCZ = "Rozhovor";
                            //Ozve se zvuk a podle nej se vybere jeden ze ctyr obrazku
                            Exercises.Add(new ExerciseConversation(sheet, this, includeLocked));
                            break;

                        default:
                            exerciseType = exerciseType;
                            string nic = filePath;
                            var prd = sheet;
                            throw new Exception("Neznámý typ cvičení");
                    }
                }
                catch(Exception exc)
                {
                    var err = $"\n\n\nERROR: {lesson.Order}/{Order}/{Exercises.Count + 1} {exerciseType} {exc.Message} {exc.StackTrace}\n\n\n";
                    Debug.WriteLine(err);
                    throw new Exception("Chyba v cviceni. " + err);
                }                
            } // konec prochazeni excelu

            if (Exercises.Count > 0)
                Exercises.Last().Order = Exercises.Count;

            Exercises = Exercises.OrderBy(x => x.Order).ToList();

            // Pokud je sekce zamcena, nesmi v ni byt zadna cviceni. Nacital jsem je jenom proto, abych ziskal spravny nazev pro sekci, protoza ten se bere z prvniho cviceni (napriklad slovicek)
            if (IsLock)
                Exercises = new List<IExercise>();
        }
    }
}
