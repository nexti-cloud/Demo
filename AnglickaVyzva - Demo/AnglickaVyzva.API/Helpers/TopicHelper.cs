using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class TopicHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Created TopicItem</returns>
        public static async Task<TopicItem> InsertNewTopicItem(TopicItem newTopicItem, EFTopicSetRepo topicSetRepo, EFTopicItemRepo topicItemRepo)
        {
            var topicSet = await topicSetRepo.All.FirstAsync(x => x.Id == newTopicItem.TopicSetId);

            var lastOrder = await topicItemRepo.All.Where(x => x.TopicSetId == topicSet.Id).Select(x => x.Order).OrderByDescending(x => x).FirstOrDefaultAsync();

            var topicItem = new TopicItem
            {
                Cz = newTopicItem.Cz,
                NoteCz = newTopicItem.NoteCz,
                Order = lastOrder + 1,
                IsHidden = true,

                TopicSetId = topicSet.Id,
            };

            topicItemRepo.Add(topicItem);
            await topicItemRepo.SaveAll();

            return topicItem;
        }

        public static async Task UpdateTopicItemEnList(int topicItemId, List<TopicItem_En> enList, EFTopicItemRepo topicItemRepo)
        {
            var item = await topicItemRepo.All.FirstAsync(x => x.Id == topicItemId);

            var newEnList = enList == null ? new List<TopicItem_En>() : enList;

            await CreateAudio(item, enList);

            item.EnList = enList;

            await topicItemRepo.SaveAll();
        }

        /// <summary>
        /// Nahraje na AzureStorage mp3 soubory.
        /// Kazda polozka v EnList seznamu se bude jmenovat treba "{topicSetId}_{newEnList[i]} (index v EnListu)
        /// Pokud se En polozka na 'n' pozici lisi, prehraje se novym zvukem
        /// Odebranym polozkam se smaze mp3 ze storage (pokud bylo predtim 5 polozek a ted jsou jenom 3, tak ty dve posledni se proste smazou)
        /// </summary>
        private static async Task CreateAudio(TopicItem item, List<TopicItem_En> newEnList)
        {
            var client = TextToSpeechHelper.CreateTextToSpeechClient();

            var container = await StorageHelper.CreateContainer(StorageHelper.Container_TopicAudio);

            for (int i = 0; i < newEnList.Count; i++)
            {
                var newEn = newEnList[i];

                string fileName = $"{item.TopicSetId}_{item.Id}_{i}.mp3";

                using (var audioStream = TextToSpeechHelper.DownloadAudio(client, newEn.En))
                {
                    audioStream.Position = 0;
                    await StorageHelper.UploadFile(container, fileName, audioStream);
                }
            }

            // Odstraneni zvuku pro smazane polozky
            for (int i = newEnList.Count; i < item.EnList.Count; i++) // Pokud bylo predtim polozek vic, smazu jim MP3ky
            {
                string fileName = $"{item.TopicSetId}_{item.Id}_{i}.mp3";

                await StorageHelper.DeleteFile(container, fileName);
            }
        }

        public static async Task ImportItemsFromExcel(int topicSetId, MemoryStream excelMemoryStream, EFTopicSetRepo topicSetRepo, EFTopicItemRepo topicItemRepo)
        {
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(excelMemoryStream);
            DataSet excelDataSet = reader.AsDataSet();

            var sheet = excelDataSet.Tables[0];

            var newTopicItems = new List<TopicItem>();

            var rowIndex = 2;
            while (sheet.Rows.Count > rowIndex)
            {
                var row = sheet.Rows[rowIndex];

                var colEn = row.ItemArray[1];
                var colPron = row.ItemArray[2];
                var colCz = row.ItemArray[3];
                var colNoteCz = row.ItemArray[4];

                if (colEn == DBNull.Value || colPron == DBNull.Value || colCz == DBNull.Value) // Nejake prazdne hodnoty
                {
                    break;
                }

                var topicItem = new TopicItem
                {
                    Cz = colCz.ToString(),
                    NoteCz = colNoteCz == DBNull.Value ? null : colNoteCz.ToString(),
                    TopicSetId = topicSetId,
                };

                var enList = new List<TopicItem_En>();

                var enParts = colEn.ToString().Split(";", StringSplitOptions.RemoveEmptyEntries);
                var pronParts = colPron.ToString().Split("~", StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < enParts.Length; i++)
                {
                    var enPart = enParts[i];

                    if(pronParts.Length <= i)
                    {
                        throw new Exception($"Chybí pronunciation pro slovíčko: {topicItem.Cz}. Řádek: {rowIndex + 1}");
                    }
                    var pronPart = pronParts[i];

                    var pronSplits = pronPart.Split(';');
                    if(pronSplits.Length != 2)
                    {
                        throw new Exception($"Špatně pronunciation pro slovíčko: {topicItem.Cz}. Řádek: {rowIndex + 1}");
                    }

                    var enItem = new TopicItem_En
                    {
                        En = enPart,
                        PronunciationEn = pronParts[i].Split(';')[0],
                        PronunciationCz = pronParts[i].Split(';')[1],
                    };
                    enList.Add(enItem);
                }

                topicItem.EnList = enList; // MUSI TO BYT TADY, protoze prirazenim se v Setteru nastavuje EnListStr


                newTopicItems.Add(topicItem);


                rowIndex++;
            }

            foreach(var newTopicItem in newTopicItems)
            {
                var createdTopicItem = await TopicHelper.InsertNewTopicItem(newTopicItem, topicSetRepo, topicItemRepo);
                await UpdateTopicItemEnList(createdTopicItem.Id, newTopicItem.EnList, topicItemRepo);
            }
        }
    }
}
