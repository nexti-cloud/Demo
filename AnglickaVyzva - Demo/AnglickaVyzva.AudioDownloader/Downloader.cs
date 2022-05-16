
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace AnglickaVyzva.AudioDownloader
{
    public static class Downloader
    {
        private static string CreateFilePath(string folderPath, IExercise ex)
        {
            string path = folderPath + ex.Order + " " + ex.NameCZ.Replace("/", "_").Replace(":", "_").Replace("?", "_") + ".txt";
            return path;
        }

        private static string CreateAudioUrl(string sentence)
        {
            string url = @"https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=";
            url += sentence;
            url += "&tl=en";
            return url;
        }

        public static void Download(List<Lesson> lessons)
        {
            // https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=How%20are%20you%20today?%20I'm%20fine,%20thank%20you.&tl=en

            for (int i = 5; i < lessons.Count; i++)
            {
                if (i > 0)
                    break;

                for (int j = 0; j < lessons[i].Sections.Count; j++)
                {
                    Lesson lesson = lessons[i];
                    Section section = lesson.Sections[j];
                    for (int k = 0; k < section.Exercises.Count; k++)
                    {
                        try
                        {

                            IExercise exercise = section.Exercises[k];

                            StringBuilder lines = new StringBuilder();

                            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AudioDownloader\";
                            folderPath += lesson.Order + @"\";
                            folderPath += section.NameCZ.Replace("/", "_") + @"\";
                            Directory.Exists(folderPath);
                            Directory.CreateDirectory(folderPath);

                            if (exercise.Type == "question_fulltextReply") // QUESTION_FULLTEXTrEPLY
                            {
                                ExerciseQuestion_FulltextReply ex = (ExerciseQuestion_FulltextReply)exercise;

                                string filePath = CreateFilePath(folderPath, ex); //folderPath + ex.Order + " " + ex.NameCZ.Replace("/", "_") + ".txt";

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemQuestion_FulltextReply item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper(item.Question, item.Reply);

                                    StringBuilder line = new StringBuilder();

                                    for (int y = 0; y < sentence.ReplyParts[0].Options.Count; y++)
                                    {
                                        string option = sentence.ReplyParts[0].Options[y].Replace("~", " ");
                                        option = option.Replace(" '", "'"); //Odstranim mezeru pred apostrofem

                                        string url = CreateAudioUrl(option);


                                        string fileName = option.Replace("?", "_") + ".mp3";
                                        using (WebClient webClient = new WebClient())
                                        {
                                            webClient.DownloadFile(url, folderPath + fileName);
                                        }

                                        line.Append(fileName.Replace(".mp3", ""));
                                        line.Append("/");
                                    }

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "fill") // FILL
                            {
                                ExerciseFill ex = (ExerciseFill)exercise;
                                string filePath = CreateFilePath(folderPath, ex);

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemFill item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper(item.Question, item.Reply);

                                    //if(item.Question.Contains("JÁ"))
                                    //{
                                    //    item.Question = item.Question;
                                    //}


                                    StringBuilder line = new StringBuilder();

                                    var inputs = sentence.QuestionParts.Where(o => o.IsInput).ToList();


                                    // Udelam to jednoduse. Vezmu prvni odpovedi z kazdeho reply part. (Kdyz to nekde nebude sedet, tak kocka)
                                    //if (inputs.Count > 1)
                                    //{
                                    int inputIndex = 0;
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        if (qPart.IsInput)
                                        {
                                            qPart.OriginalText = sentence.ReplyParts[inputIndex].Options[0];
                                            inputIndex++;
                                        }
                                    }

                                    string glued = "";
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        glued += qPart.OriginalText;
                                    }
                                    glued = Regex.Replace(glued, @" *\([^)]*\)*", ""); //Odstranim ceskou napovedu v zavorce
                                    glued = glued.Replace(" '", "'").Replace("~", " "); //Odstranim mezeru pred apostrofem

                                    string url = CreateAudioUrl(glued);

                                    string fileName = glued.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        try
                                        {
                                            webClient.DownloadFile(url, folderPath + fileName);
                                        }
                                        catch (Exception exc)
                                        {

                                        }
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");
                                    /*}
                                    else
                                    {

                                        for (int y = 0; y < sentence.ReplyParts[0].Options.Count; y++)
                                        {
                                            string option = sentence.Question.Replace("_", sentence.ReplyParts[0].Options[y]).Replace("~", " "); //Nahradim _ jednou z moznosti
                                            option = Regex.Replace(option, @" *\([^)]*\)*", ""); //Odstarim ceskou napovedu v zavorce
                                            option = option.Replace(" '", "'"); //Odstranim mezeru pred apostrofem

                                            string url = @"https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=";
                                            url += option;
                                            url += "&tl=en";


                                            string fileName = option.Replace("?", "_") + ".mp3";
                                            using (WebClient webClient = new WebClient())
                                            {
                                                webClient.DownloadFile(url, folderPath + fileName);
                                            }

                                            line.Append(fileName.Replace(".mp3", ""));
                                            line.Append("/");
                                        }
                                    }*/

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "dictation_fill") //DICTATION_FILL
                            {
                                ExerciseDictation_Fill ex = (ExerciseDictation_Fill)exercise;
                                string filePath = CreateFilePath(folderPath, ex);

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemDictation_Fill item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper(item.Question, item.Reply);

                                    StringBuilder line = new StringBuilder();

                                    var inputs = sentence.QuestionParts.Where(o => o.IsInput).ToList();


                                    // udelam to jednoduse. Vezmu prvni odpovedi z kazdeho reply part. (Kdyz to nekde nebude sedet, tak kocka)
                                    int inputIndex = 0;
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        if (qPart.IsInput)
                                        {
                                            if (sentence.ReplyParts.Count <= inputIndex) // pokud ma byt odpoved "" (nic), tak to musim rucne dat, jinak prekrocim hranici pole
                                                qPart.OriginalText = "";
                                            else
                                                qPart.OriginalText = sentence.ReplyParts[inputIndex].Options[0];
                                            inputIndex++;
                                        }
                                    }

                                    string glued = "";
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        glued += qPart.OriginalText;
                                    }
                                    glued = Regex.Replace(glued, @" *\([^)]*\)*", ""); //Odstarim ceskou napovedu v zavorce
                                    glued = glued.Replace(" '", "'"); //Odstranim mezeru pred apostrofem
                                    glued = glued.Trim();

                                    string url = CreateAudioUrl(glued);

                                    string fileName = glued.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "sort")
                            {
                                ExerciseSort ex = (ExerciseSort)exercise;
                                string filePath = CreateFilePath(folderPath, ex);

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemSort item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper("", item.Question);

                                    StringBuilder line = new StringBuilder();

                                    string glued = "";
                                    foreach (var part in sentence.ReplyParts)
                                    {
                                        glued += part.Options[0] + " ";
                                    }
                                    glued = glued.Replace(" ?", "?").Replace(" !", "!").Replace(" .", ".").Trim();

                                    string url = CreateAudioUrl(glued);

                                    string fileName = glued.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "select")
                            {
                                ExerciseSelect ex = (ExerciseSelect)exercise;
                                string filePath = CreateFilePath(folderPath, ex);

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemSelect item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper(item.Question, item.Reply);

                                    StringBuilder line = new StringBuilder();

                                    var inputs = sentence.QuestionParts.Where(o => o.IsInput).ToList();


                                    // Udelam to jednoduse. Vezmu prvni odpovedi z kazdeho reply part. (Kdyz to nekde nebude sedet, tak kocka)
                                    int inputIndex = 0;
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        if (qPart.IsInput)
                                        {
                                            qPart.OriginalText = sentence.ReplyParts[inputIndex].Options[0];
                                            inputIndex++;
                                        }
                                    }

                                    string glued = "";
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        glued += qPart.OriginalText;
                                    }
                                    glued = Regex.Replace(glued, @" *\([^)]*\)*", ""); //Odstarim ceskou napovedu v zavorce
                                    glued = glued.Replace(" '", "'").Replace("~", " "); //Odstranim mezeru pred apostrofem

                                    string url = CreateAudioUrl(glued);

                                    string fileName = glued.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "decision")
                            {
                                ExerciseDecision ex = (ExerciseDecision)exercise;
                                string filePath = CreateFilePath(folderPath, ex);

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemDecision item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper("", item.Question);

                                    StringBuilder line = new StringBuilder();
                                    string correctAnswer = sentence.ReplyParts.Where(o => o.OriginalText.Contains("°")).First().Options[0].Replace("°", "");



                                    string url = CreateAudioUrl(correctAnswer);

                                    string fileName = correctAnswer.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "options")
                            {
                                ExerciseOptions ex = (ExerciseOptions)exercise;
                                string filePath = CreateFilePath(folderPath, ex);

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemOptions item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper(item.Question, item.Reply);

                                    StringBuilder line = new StringBuilder();

                                    var inputs = sentence.QuestionParts.Where(o => o.IsInput).ToList();


                                    // Udelam to jednoduse. Vezmu prvni odpovedi z kazdeho reply part. (Kdyz to nekde nebude sedet, tak kocka)
                                    int inputIndex = 0;
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        if (qPart.IsInput)
                                        {
                                            qPart.OriginalText = sentence.ReplyParts[inputIndex].Options[sentence.ReplyParts[inputIndex].CorrectIndex];
                                            inputIndex++;
                                        }
                                    }

                                    string glued = "";
                                    for (int q = 0; q < sentence.QuestionParts.Count; q++)
                                    {
                                        QuestionPart qPart = sentence.QuestionParts[q];
                                        glued += qPart.OriginalText;
                                    }
                                    glued = Regex.Replace(glued, @" *\([^)]*\)*", ""); //Odstarim ceskou napovedu v zavorce
                                    glued = glued.Replace(" '", "'").Replace("~", " "); //Odstranim mezeru pred apostrofem

                                    string url = CreateAudioUrl(glued);

                                    string fileName = glued.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());

                            }
                            else if (exercise.Type == "video")
                            {

                            }
                            else if (exercise.Type == "vocabulary")
                            {

                            }
                            else if (exercise.Type == "assign")
                            {

                            }
                            else if (exercise.Type == "dictation")
                            {
                                ExerciseDictation ex = (ExerciseDictation)exercise;

                                string filePath = CreateFilePath(folderPath, ex); //folderPath + ex.Order + " " + ex.NameCZ.Replace("/", "_") + ".txt";

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemDictation item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper("", item.Reply);

                                    StringBuilder line = new StringBuilder();

                                    string option = sentence.ReplyParts[0].Options[0].Replace("~", " ");
                                    option = option.Replace(" '", "'"); //Odstranim mezeru pred apostrofem

                                    string url = CreateAudioUrl(option);


                                    string fileName = option.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "rewrite")
                            {
                                ExerciseRewrite ex = (ExerciseRewrite)exercise;

                                string filePath = CreateFilePath(folderPath, ex); //folderPath + ex.Order + " " + ex.NameCZ.Replace("/", "_") + ".txt";

                                if (File.Exists(filePath)) //Nebudu porad dokola stahovat stejne soubory, kdyz uz je mam stazene
                                    continue;

                                for (int x = 0; x < ex.ItemList.Count; x++)
                                {
                                    ItemRewrite item = ex.ItemList[x];
                                    SentenceHelper sentence = new SentenceHelper("", item.Reply);

                                    StringBuilder line = new StringBuilder();

                                    string option = sentence.ReplyParts[0].Options[0];
                                    option = option.Replace(" '", "'"); //Odstranim mezeru pred apostrofem

                                    string url = CreateAudioUrl(option);


                                    string fileName = option.Replace("?", "_") + ".mp3";
                                    using (WebClient webClient = new WebClient())
                                    {
                                        webClient.DownloadFile(url, folderPath + fileName);
                                    }

                                    line.Append(fileName.Replace(".mp3", ""));
                                    line.Append("/");

                                    string lineStr = line.ToString();
                                    lineStr = lineStr.Remove(line.ToString().Length - 1); //Odendam posledni lomeno
                                                                                          //pokud je vice zvuku -> dam to do chlupatych, aby to poznala aplikace
                                    if (lineStr.Contains("/"))
                                    {
                                        lineStr = "{" + lineStr + "}";
                                    }

                                    lines.Append(lineStr);
                                    lines.AppendLine();


                                }
                                File.WriteAllText(filePath, lines.ToString());
                            }
                            else if (exercise.Type == "picture_select")
                            {

                            }
                            else if (exercise.Type == "fillTable")
                            {

                            }
                            else
                            {
                                if (exercise.Type == null)
                                    continue;
                            }
                        }
                        catch (Exception exc)
                        {

                        }

                    }
                }
            }

            //string question = "How _ you?";
            //string reply = "Amber {isn't/is not} my niece but she {'s/is} my granddaughter.";

            //string question = "How _ you? I _.";
            //string reply = "{are/'re};{am/'m} fine";

            //SentenceHelper sentence = new SentenceHelper(question, reply);

            /*int i = 0;
            while (i < sentence.QuestionParts.Count)
            {

                i++;
            }*/


            //string url = @"https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=How%20are%20you%20today?%20I'm%20fine,%20thank%20you.&tl=en";
            //string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\AudioDownloader\";

            //filePath += "soubor.mp3";

            //WebClient client = new WebClient();
            //client.DownloadFile(url, filePath);
        }
    }
}
