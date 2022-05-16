
using AnglickaVyzva.API.Models;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public static class ExcelHelper
    {
        public static List<Topic> GetTopics(string dirPath)
        {
            List<Topic> topics = new List<Topic>();

            foreach (string filePath in Directory.GetFiles(dirPath))
            {
                try
                {
                    // Neni to excel
                    if (!filePath.EndsWith(".xlsx"))
                        continue;

                    FileStream fs = System.IO.File.Open(filePath, FileMode.Open);
                    IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
                    DataSet result = reader.AsDataSet();
                    fs.Close();

                    topics.Add(new Topic(filePath, result));
                }
                catch (Exception exc)
                {

                }
            }

            return topics;
        }

        public static List<Lesson> GetLessons(string dirPath, bool includeLocked)
        {
            List<Lesson> lessonList = new List<Lesson>();

            foreach (string lessonDir in Directory.GetDirectories(dirPath))
            {
                try
                {
                    string[] lessonDirParts = lessonDir.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    Lesson lesson = new Lesson();
                    lesson.Order = Convert.ToInt32(lessonDirParts.Last());

                    if(!EnvironmentHelper.IsOnAnyDev())
                    {
                        if (lesson.Order > 21)
                            continue;
                    }

                    lessonList.Add(lesson);


                    var obsahFilePath = lessonDir + "\\Obsah lekce.txt";

                    if (File.Exists(lessonDir + "\\Obsah lekce.txt"))
                    {
                        string lessonContentStr = File.ReadAllText(obsahFilePath, Encoding.Default);
                        string[] contentLines = lessonContentStr.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        lesson.ContentVocabulary = contentLines[0];
                        lesson.ContentGrammar = contentLines[1];
                    }


                    //Projdu vsechny soubory se sekcemi v dane lekci
                    foreach (string filePath in Directory.GetFiles(lessonDir))
                    {
                        try
                        {
                            //Neni to excel s lekci
                            if (!filePath.EndsWith(".xlsx"))
                                continue;

                            if (filePath.EndsWith("Test.xlsx"))
                            {
                                string fileName = filePath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).Last();
                                string[] nameParts = fileName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                int order = Convert.ToInt32(nameParts[0]);


                                FileStream fs = System.IO.File.Open(filePath, FileMode.Open);
                                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
                                DataSet result = reader.AsDataSet();
                                fs.Close();

                                lesson.Tests.Add(new Test(lesson, filePath, result.Tables[0], order));
                            }
                            else
                            {

                                //1 vocabulary  Věci okolo nás 1.xlsx		fileName	null	string

                                string fileName = filePath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).Last();
                                string[] nameParts = fileName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                string order = nameParts[0];
                                string type = nameParts[1];

                                if (fileName.Contains("~$")) //Docasne soubory
                                    continue;

                                lesson.Sections.Add(new Section(filePath, lesson, includeLocked));
                            }
                        }
                        catch (Exception exc)
                        {

                        }

                    }

                    lesson.Sections = lesson.Sections.OrderBy(x => x.Order).ToList();
                    lesson.Tests = lesson.Tests.OrderBy(x => x.Order).ToList();
                }
                catch (Exception exc)
                {

                }
            }

            /*
            string filePath = System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data/Lekce/Lekce 1/1 vocabulary  Věci okolo nás 1.xlsx");

            if (System.IO.File.Exists(filePath))
            {
                FileStream fs = System.IO.File.Open(filePath, FileMode.Open);
                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
                DataSet result = reader.AsDataSet();

                DataTable sheet = result.Tables[0];
                if (sheet == null)
                    sheet = null;
            }*/



            //CHYBY VE CVICENI
            //StringBuilder sb = new StringBuilder();
            //foreach(var lesson in lessonList)
            //{
            //    sb.Append("-Lekce: " + lesson.Order + "\n");
            //    foreach(var section in lesson.Sections)
            //    {
            //        sb.Append("Sekce: " + section.Order + "\n");
            //        foreach(var exercise in section.Exercises)
            //        {
            //            if(exercise.HasError)
            //            {
            //                sb.Append("\tChyba ve cvničení: " + exercise.Order + "\n");
            //            }
            //        }
            //    }
            //}

            //string chyby = sb.ToString();
            //if (chyby == null)
            //    chyby = null;

            lessonList = lessonList.OrderBy(x => x.Order).ToList();

            var lines = new List<string>();

            List<Tuple<string, string>> firstOccurence = new List<Tuple<string, string>>();

            var linesOnlyOnePoint = new List<string>();

            var withOnePointSum = 0;
            var pointsSum = 0;

            foreach (var lesson in lessonList)
            {
                foreach (var section in lesson.Sections)
                {
                    foreach (var exercise in section.Exercises)
                    {
                        if (!firstOccurence.Any(x => x.Item1 == exercise.Type))
                        {
                            firstOccurence.Add(new Tuple<string, string>(exercise.Type, $"{lesson.Order.ToString("00")}/{section.Order.ToString("00")}/{exercise.Order.ToString("00")}"));
                        }

                        if (exercise.Points == 1)
                        {
                            withOnePointSum++;
                            linesOnlyOnePoint.Add($"{exercise.Type} {lesson.Order.ToString("00")}/{section.Order.ToString("00")}/{exercise.Order.ToString("00")} [body: {exercise.Points}]");
                        }

                        if (exercise.Type == "vocabulary")
                        {
                            pointsSum += 100;
                        }
                        else
                        {
                            pointsSum += exercise.Points;
                        }

                        lines.Add($"{exercise.Type} {lesson.Order.ToString("00")}/{section.Order.ToString("00")}/{exercise.Order.ToString("00")} [body: {exercise.Points}]");
                    }
                }
            }

            Debug.WriteLine("Poradi podle vyskytu");
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }


            Debug.WriteLine("Poradi podle abecedy");
            lines = lines.OrderBy(x => x).ToList();
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }

            Debug.WriteLine("Prvni vyskyt");
            firstOccurence.ForEach(x => Debug.WriteLine($"{x.Item1} {x.Item2}"));

            Debug.WriteLine("--Cvičení pouze s jedním bodem:");
            linesOnlyOnePoint.ForEach(x => Debug.WriteLine(x));

            Debug.WriteLine("Součet všech bodů cvičení (vocabulary se počítá za 100): " + pointsSum);
            Debug.WriteLine("Počet cvičení s jedním bodem: " + withOnePointSum);

            lessonList = lessonList.OrderBy(x => x.Order).ToList();

            var generateAllInOneFile = false;

            if (generateAllInOneFile)
            {

                var sb = new StringBuilder();

                foreach (var lesson in lessonList)
                {
                    sb.AppendLine($"Lekce {lesson.Order}");

                    foreach (var section in lesson.Sections)
                    {
                        sb.AppendLine($";{section.Order} {section.NameCZ}");
                        foreach (var exercise in section.Exercises)
                        {
                            var sentenceLines = new List<string>();

                            sb.AppendLine($";;{exercise.Order} {exercise.Type} {exercise.NameCZ} [{exercise.Points}]");
                            switch (exercise.Type)
                            {
                                case IExercise.Types.ExerciseAssing:
                                    sentenceLines.AddRange((exercise as ExerciseAssign).ItemList.Select(x => $"{x.ID};{x.First};{x.Second}"));
                                    break;

                                case IExercise.Types.ExerciseAssignPicture:
                                    sentenceLines.AddRange((exercise as ExerciseAssignPicture).ItemList.Select(x => $"{x.ID};{x.First};{x.Image}"));
                                    break;

                                case IExercise.Types.ExerciseAssignShort:
                                    sentenceLines.AddRange((exercise as ExerciseAssignShort).ItemList.Select(x => $"{x.ID};{x.First};{x.Second}"));
                                    break;

                                case IExercise.Types.ExerciseAudio_SelectPicture:
                                    sentenceLines.AddRange((exercise as ExerciseAudio_SelectPicture).ItemList.Select(x => $"{x.ID};{x.Audio};{string.Join(';', x.Images)};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseColumns:
                                    sentenceLines.AddRange((exercise as ExerciseColumns).ItemList.Select(x => $"{x.ColumnID};{x.Word};{x.Audio}"));
                                    break;

                                case IExercise.Types.ExerciseConversation:
                                    sentenceLines.AddRange((exercise as ExerciseConversation).ItemSentenceList.Select(x => $"{x.TitleText};{x.Sentence};{x.PersonOrder}"));
                                    sentenceLines.Add("---");
                                    sentenceLines.AddRange((exercise as ExerciseConversation).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply}"));
                                    break;

                                case IExercise.Types.ExerciseDecision:
                                    sentenceLines.AddRange((exercise as ExerciseDecision).ItemList.Select(x => $"{x.ID};{x.Question};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseDictation:
                                    sentenceLines.AddRange((exercise as ExerciseDictation).ItemList.Select(x => $"{x.ID};{x.Audio};{x.Reply};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseDictation_Fill:
                                    sentenceLines.AddRange((exercise as ExerciseDictation_Fill).ItemList.Select(x => $"{x.ID};{x.Audio};{x.Question};{x.Reply};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseDictation_Select:
                                    sentenceLines.AddRange((exercise as ExerciseDictation_Select).ItemList.Select(x => $"{x.ID};{x.Audio};{x.Reply}"));
                                    break;

                                case IExercise.Types.ExerciseFill:
                                    sentenceLines.AddRange((exercise as ExerciseFill).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseFill_Picture:
                                    sentenceLines.AddRange((exercise as ExerciseFill_Picture).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation};{x.Image}"));
                                    break;

                                case IExercise.Types.ExerciseFillTable:
                                    sentenceLines.AddRange((exercise as ExerciseFillTable).ItemList.Select(x => $"{x.ID};{string.Join(';', x.Columns)}"));
                                    break;

                                case IExercise.Types.ExerciseGame_Dice:
                                    sentenceLines.Add("HRA");
                                    break;

                                case IExercise.Types.ExerciseInsert:
                                    sentenceLines.AddRange((exercise as ExerciseInsert).ItemList.Select(x => $"{x.ID};{x.Question};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseOptions:
                                    sentenceLines.AddRange((exercise as ExerciseOptions).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExercisePicture_Columns:
                                    sentenceLines.AddRange((exercise as ExercisePicture_Columns).ItemList.Select(x => $"{x.ColumnID};{x.Image};{x.Audio}"));
                                    break;

                                case IExercise.Types.ExercisePicture_FulltextReply:
                                    sentenceLines.AddRange((exercise as ExercisePicture_FulltextReply).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation};{x.Image}"));
                                    break;

                                case IExercise.Types.ExercisePicture_Select:
                                    sentenceLines.AddRange((exercise as ExercisePicture_Select).ItemList.Select(x => $"{x.ID};{x.Audio};{x.Question};{string.Join(';', x.Images)}"));
                                    break;

                                case IExercise.Types.ExerciseQuestion_FulltextReply:
                                    sentenceLines.AddRange((exercise as ExerciseQuestion_FulltextReply).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.ReplyTranslation}"));
                                    break;

                                case IExercise.Types.ExerciseRewrite:
                                    sentenceLines.AddRange((exercise as ExerciseRewrite).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseRows:
                                    sentenceLines.AddRange((exercise as ExerciseRows).ItemList.Select(x => $"{x.ID};{x.Title};{string.Join(';', x.Words)}"));
                                    break;

                                case IExercise.Types.ExerciseSelect:
                                    sentenceLines.AddRange((exercise as ExerciseSelect).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseSelect_Picture:
                                    sentenceLines.AddRange((exercise as ExerciseSelect_Picture).ItemList.Select(x => $"{x.ID};{x.Audio};{x.Image};{string.Join(';', x.Questions)}"));
                                    break;

                                case IExercise.Types.ExerciseSelect_PictureOptions:
                                    sentenceLines.AddRange((exercise as ExerciseSelect_PictureOptions).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation};{x.Image}"));
                                    break;

                                case IExercise.Types.ExerciseSort:
                                    sentenceLines.AddRange((exercise as ExerciseSort).ItemList.Select(x => $"{x.ID};{x.Question};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseTwo_Sentences:
                                    sentenceLines.AddRange((exercise as ExerciseTwo_Sentences).ItemList.Select(x => $"{x.ID};{x.Question};{x.Reply};{x.Audio};{x.Translation}"));
                                    break;

                                case IExercise.Types.ExerciseVideo:
                                    var exc = (exercise as ExerciseVideo);
                                    sentenceLines.Add($"{exc.VideoURL}");
                                    break;

                                case IExercise.Types.ExerciseVocabulary:
                                    sentenceLines.AddRange((exercise as ExerciseVocabulary).ItemList.Select(x => $"{x.ID};{x.EN};{x.PronunciationEN};{x.PronunciationCZ};{x.CZ};{x.Audio};{x.NoteCZ};{x.NoteEN}"));
                                    break;

                                default:
                                    break;
                            }
                            foreach (var line in sentenceLines)
                            {
                                sb.AppendLine($";;;{line}");
                            }
                        }
                    }
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                File.WriteAllText(Path.Combine(desktopPath, "AV podrobny obsah.csv"), sb.ToString(), Encoding.UTF8);
            }

            return lessonList;
        }

        public static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
