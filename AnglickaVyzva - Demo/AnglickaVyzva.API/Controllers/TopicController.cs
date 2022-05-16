using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.DTOs.Topic;
using AnglickaVyzva.API.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TopicController : BaseController
    {
        static Random random = new Random();

        public TopicController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {

        }

        [HttpPost("getTopicList")]
        public async Task<IActionResult> GetTopicList()
        {
            var loggedUser = await GetLoggedUser();

            var topics = await TopicRepo.All.Where(x=>x.IsHidden != true).Include(x => x.TopicSections).ToListAsync();

            var topicsDtos = _mapper.Map<List<Topic_Dto>>(topics);

            var knownItemsCount = await TopicRepo.All
                .Include(x => x.TopicSections)
                .ThenInclude(x => x.TopicSets)
                .ThenInclude(x => x.TopicItems)
                .ThenInclude(x => x.TopicItems_User)
                .Select(topic => new
                {
                    TopicId = topic.Id,
                    Sections = topic.TopicSections.Select(section => new
                    {
                        SectionId = section.Id,
                        knownItemsCount = section.TopicSets.SelectMany(set => set.TopicItems.SelectMany(item => item.TopicItems_User))
                        .Where(x => x.UserId == loggedUser.Id && x.DontKnow != true && x.Score >= 0.5).Count()
                    }),
                })
                .ToListAsync();

            //var knownItemsCount = await TopicRepo.All
            //    .Include(x => x.TopicSections)
            //    .ThenInclude(x => x.TopicSets)
            //    .ThenInclude(x => x.TopicItems)
            //    .ThenInclude(x => x.TopicItems_User)
            //    .SelectMany(x => x.TopicSections.SelectMany(x => x.TopicSets).SelectMany(x => x.TopicItems).SelectMany(x => x.TopicItems_User))
            //    .Where(x => x.UserId == loggedUser.Id && x.DontKnow != true && x.Score >= 0.5)
            //    .CountAsync();

            foreach (var topic in topicsDtos)
            {
                var knownItems = knownItemsCount.Where(x => x.TopicId == topic.Id).FirstOrDefault();
                if (knownItems != null)
                {
                    topic.KnownItems = knownItems.Sections.Sum(x => x.knownItemsCount);

                    foreach (var section in topic.TopicSections)
                    {
                        var knownItemsSection = knownItems.Sections.FirstOrDefault(x => x.SectionId == section.Id);
                        if (knownItemsSection != null)
                        {
                            section.KnownItems = knownItemsSection.knownItemsCount;
                        }
                    }
                }
            }

            return Ok(new
            {
                topics = topicsDtos,
            });
        }

        public class GetTopicSectionDetail_Model
        {
            public int TopicSectionId { get; set; }
        }
        [HttpPost("getTopicSectionDetail")]
        public async Task<IActionResult> GetTopicSectionDetail(GetTopicSectionDetail_Model model)
        {
            var loggedUser = await GetLoggedUser();

            var section = await TopicSectionRepo.All.Include(x => x.TopicSets).FirstAsync(x => x.Id == model.TopicSectionId);
            section.TopicSets = section.TopicSets.Where(x => x.IsHidden != true).ToList();
            var sectionDto = _mapper.Map<TopicSection_Dto>(section);

            var setsItems = await TopicSectionRepo.All.Include(x => x.TopicSets).ThenInclude(y => y.TopicItems).ThenInclude(z => z.TopicItems_User)
                                        .Where(x => x.Id == model.TopicSectionId)
                                        .SelectMany(x => x.TopicSets)
                                        .Select(set => new
                                        {
                                            SetId = set.Id,
                                            Items = set.TopicItems.Select(item => new
                                            {
                                                Item_User = item.TopicItems_User
                                                                .Where(item_user => item_user.UserId == loggedUser.Id)
                                                                .Select(item_user => new
                                                                {
                                                                    Score = item_user.Score,
                                                                    DontKnow = item_user.DontKnow,
                                                                })
                                                                .FirstOrDefault()

                                            })
                                            //Items = x.TopicItems.SelectMany(y=>y.TopicItems_User).Where(x=>x.UserId == loggedUser.Id)
                                            //            .Select(item => new
                                            //            {
                                            //                Score = item.Score,
                                            //                DontKnow = item.DontKnow,
                                            //            })
                                        })
                                        .ToListAsync();


            foreach (var set in sectionDto.TopicSets)
            {
                set.PerfectKnowledgeCount = 0;
                set.GoodKnowledgeCount = 0;
                set.BadKnowledgeCount = 0;
                set.NotPlayedCount = 0;

                var setItems = setsItems.Where(x => x.SetId == set.Id).FirstOrDefault();
                if (setItems != null)
                {
                    foreach (var item in setItems.Items)
                    {
                        if (item.Item_User == null)
                        {
                            set.NotPlayedCount++;
                        }
                        else
                        {
                            if (item.Item_User.DontKnow == true)
                            {
                                set.BadKnowledgeCount++;
                            }
                            else
                            {
                                if (item.Item_User.Score < 0.5)
                                {
                                    set.BadKnowledgeCount++;
                                }
                                else if (item.Item_User.Score < 0.75)
                                {
                                    set.GoodKnowledgeCount++;
                                }
                                else
                                {
                                    set.PerfectKnowledgeCount++;
                                }
                            }
                        }
                    }
                }

            }


            return Ok(new
            {
                section = sectionDto,
            });
        }

        public class GetTopicSetDetail_Model
        {
            public int TopicSetId { get; set; }
        }
        [HttpPost("getTopicSetDetail")]
        public async Task<IActionResult> GetTopicSetDetail(GetTopicSetDetail_Model model)
        {
            var user = await GetLoggedUser();

            var set = await TopicSetRepo.All.Include(x => x.TopicItems).FirstAsync(x => x.Id == model.TopicSetId);
            set.TopicItems = set.TopicItems.Where(x => x.IsHidden != true).ToList();

            var setDto = _mapper.Map<TopicSet_Dto>(set);



            var items_user = await TopicSetRepo.All
                .Include(x => x.TopicItems)
                .ThenInclude(x => x.TopicItems_User)
                .Where(x => x.Id == model.TopicSetId)
                .SelectMany(x => x.TopicItems.SelectMany(y => y.TopicItems_User).Where(z => z.UserId == user.Id))
                .Select(x => new
                {
                    ItemId = x.TopicItemId,
                    DontKnow = x.DontKnow,
                    Score = x.Score,
                })
                .ToListAsync();

            foreach (var item in setDto.TopicItems)
            {
                var item_user = items_user.FirstOrDefault(x => x.ItemId == item.Id);
                if (item_user != null)
                {
                    item.Score = item_user.Score;
                    item.DontKnow = item_user.DontKnow;
                }
            }

            setDto.TopicItems = setDto.TopicItems.OrderByDescending(x => x.Score).ToList();

            return Ok(new
            {
                set = setDto,
            });
        }

        public class GetItemsFromSet_Model
        {
            public int TopicSetId { get; set; }
        }
        [HttpPost("getItemsFromSet")]
        public async Task<IActionResult> GetItemsFromSet(GetItemsFromSet_Model model)
        {
            if (await IsFreeDoseExhaustedForToday())
            {
                return Ok(new
                {
                    freeDoseExhaustedForToday = true
                });
            }

            const int targetItemCountInResult = 7;

            var loggedUser = await GetLoggedUser();

            var items_user = await TopicSetRepo.All.Include(x => x.TopicItems).ThenInclude(x => x.TopicItems_User)
                .Where(topicSet =>
                        topicSet.Id == model.TopicSetId// &&
                        //topicSet.TopicItems.Any(y => y.TopicItems_User.Any(z => z.UserId == loggedUser.Id))
                )
                .SelectMany(x => x.TopicItems.SelectMany(x => x.TopicItems_User))
                .Where(topicItem_user=>topicItem_user.UserId == loggedUser.Id)
                .ToListAsync();


            var itemsInSet = await TopicItemRepo.All.Where(x => x.TopicSetId == model.TopicSetId && x.IsHidden != true).ToListAsync();


            // Pokud ma z teto sady uz nejaka slovicka rozehrana, tak je nactu. Ale musi je umet jenom malo -> Pod 0.5
            // Uz je hral ale umi je jenom malo nebo vubec
            var played_items_user_ids = items_user.Where(x => x.Score < 0.5 || x.DontKnow).OrderBy(x => x.Score).Take(targetItemCountInResult).Select(x => x.TopicItemId).ToList();
            var playedItems_badKnowledge = itemsInSet.Where(x => played_items_user_ids.Contains(x.Id)).ToList();


            var resultItems = new List<TopicItem>();
            resultItems.AddRange(playedItems_badKnowledge);

            var remainingItemsInSet = itemsInSet.Where(x => !played_items_user_ids.Contains(x.Id)).ToList();


            // Zbylym polozkam priradim skore (pokud uz je nekdy hral), abych podle skore mohl vybirat ty, ktere umi nejmene
            var remainingItemsWithScore = new List<TopicItem_Dto>();
            foreach (var remainingItem in remainingItemsInSet)
            {
                var score = 0.0;

                var item_user = items_user.FirstOrDefault(x => x.TopicItemId == remainingItem.Id);
                if (item_user != null)
                {
                    score = item_user.Score;
                }

                remainingItemsWithScore.Add(new TopicItem_Dto
                {
                    Id = remainingItem.Id,
                    Score = score,
                    Cz = remainingItem.Cz, // Jenom pro ucel DEBUGU, jinak to nepotrebuju
                });
            }

            // Doplnim je nahodne zbytkem slovicek ze sady (prednostne budu vybirat ty, ktere umi nejmin)
            double threshold = 0.1;
            while (resultItems.Count < targetItemCountInResult && remainingItemsWithScore.Count > 0)
            {
                // Vyberu jenom slovicka, ktere umi pod tuto hranici
                var itemsUnderThreshold = remainingItemsWithScore.Where(x => x.Score <= threshold).ToList();

                // Vsechna slovicka umi lepe nez 0.1 -> zvedam threshold
                if (itemsUnderThreshold.Count == 0)
                {
                    threshold += 0.1;
                    continue;
                }

                var index = random.Next(itemsUnderThreshold.Count);
                var item = itemsUnderThreshold[index];

                resultItems.Add(remainingItemsInSet.First(x => x.Id == item.Id));
                remainingItemsWithScore.Remove(item);
            }

            var items = _mapper.Map<List<TopicItem_Dto>>(resultItems);

            // Rekl si naposled u slovicka, ze ho neumi? - Musim to propsat do odpovedi
            foreach (var item in items)
            {
                var item_user = items_user.FirstOrDefault(x => x.TopicItemId == item.Id);
                if (item_user != null && item_user.DontKnow == true)
                {
                    item.DontKnow = true;
                }
            }

            if(items.Count == 0)
            {
                return BadRequest("Žádná slovíčka k zobrazení");
            }

            return Ok(new
            {
                items
            });
        }

        /// <summary>
        /// Postup pro pridavani pridavani slovicek do vysledku:
        /// (Kdyz je ve vysledku uz dost slovicek, prestavam doplnovat a vracim vysledek)
        /// - Seradim slovicka podle score (jak moc je umi)
        /// - Kdyz je nejake slovicko pod 0.5 (spatne ho umi), tak ho pridam do vysledku
        /// - ZBYLA VOLNA MISTA DOPLNIM NASLEDOVNE:
        /// - - Pulku prazdneho mista doplnim slovicky, ktera nejmin umi a ktera nehral prinejmensim den
        /// - - Druhou pulku (muze byt i vic, kdyz v predchozim kroku neni dostatek slovicek) doplnim slovicky, ktera jeste nikdy nehral
        ///     (Pokud uz neni v nasi DB dostatek slovicek, ktera jeste nehral, tak zbytek doplnim slovicky, ktera uz hral)
        /// (Je naprosto minimalni sance, ze vsechna slovicka stihne odehrat za den -> v tom pripade mu to vrati prazdny seznam)
        /// </summary>
        /// <returns></returns>
        [HttpPost("getItemsForQuickLearning")]
        public async Task<IActionResult> GetItemsForRemembering()
        {
            if(await IsFreeDoseExhaustedForToday())
            {
                return Ok(new
                {
                    freeDoseExhaustedForToday = true
                });
            }

            const int targetItemCountInResult = 7;
            var resultItems_User = new List<TopicItem_User>();

            var loggedUser = await GetLoggedUser();

            var items_user = await TopicItem_UserRepo.All.Where(x => x.UserId == loggedUser.Id).ToListAsync();

            // - Seradim slovicka podle score (jak moc je umi)
            var ordered_items_user = items_user.OrderBy(x => x.Score).ToList(); // Pokazde, kdyz z tohoto seznamu neco pouziju, tak to odsud odeberu

            // - Kdyz je nejake slovicko pod 0.5 (spatne ho umi), tak ho pridam do vysledku
            for(int i =0; i < ordered_items_user.Count; i++)
            {
                var item_user = ordered_items_user[i];

                // Uz jich mam dost
                if (resultItems_User.Count == targetItemCountInResult)
                {
                    break;
                }

                // Tohle a dalsi slovicka uz umi dobre
                if (item_user.Score >= 0.5)
                {
                    break;
                }

                resultItems_User.Add(item_user);
                ordered_items_user.RemoveAt(0); // Polozku odeberu ze seznamu, abych ji nepridal v dalsich krocich znovu
                i--;

            }

            var allItemsInDbIds = await TopicItemRepo.All.Where(x=>x.IsHidden != true).Select(x => x.Id).ToListAsync();
            var playedItemsIds = items_user.Select(x => x.TopicItemId).ToList();
            var notPlayedItemsIds = allItemsInDbIds.Where(x => !playedItemsIds.Contains(x)).ToList();

            // -ZBYLA VOLNA MISTA DOPLNIM NASLEDOVNE:

            // - -Pulku prazdneho mista doplnim slovicky, ktera nejmin umi a ktera nehral prinejmensim den
            var spaceForRemindItems = (targetItemCountInResult - resultItems_User.Count) / 2;
            for (int i = 0; i < ordered_items_user.Count; i++)
            {
                var item_user = ordered_items_user[i];

                if (spaceForRemindItems == 0)
                {
                    break;
                }

                if( (DateTime.Now - (DateTime)item_user.LastUpdatedDate).TotalHours > 24 )
                {
                    resultItems_User.Add(item_user);
                    ordered_items_user.RemoveAt(0); // Polozku odeberu ze seznamu, abych ji nepridal v dalsich krocich znovu
                    i--;
                    spaceForRemindItems--;
                }
            }

            // - - Druhou pulku prazdneho mista (muze byt i vic, kdyz v predchozim kroku neni dostatek slovicek) doplnim slovicky, ktera jeste nikdy nehral
            var idsToBeLoaded = resultItems_User.Select(x => x.TopicItemId).ToList();
            while(notPlayedItemsIds.Count > 0 && idsToBeLoaded.Count != targetItemCountInResult) // Dokud jsou jeste slovicka ktera nehral && jeste nemam pozadovany pocet IDcek pro nasteni z DB
            {
                var index = random.Next(0, notPlayedItemsIds.Count);
                idsToBeLoaded.Add(notPlayedItemsIds[index]);
                notPlayedItemsIds.RemoveAt(index);
            }

            //     (Pokud uz neni v nasi DB dostatek slovicek, ktera jeste nehral, tak zbytek doplnim slovicky, ktera uz hral)
            while(idsToBeLoaded.Count != targetItemCountInResult)
            {
                // Dosla slovicka (kdyz jsem cvicne vsechno nastavil na IsHidden = true, tak to tady spadlo, proto sem to osetril.
                if(ordered_items_user.Count == 0)
                {
                    break;
                }

                var index = random.Next(0, ordered_items_user.Count);
                idsToBeLoaded.Add(ordered_items_user[index].TopicItemId);
                ordered_items_user.RemoveAt(index);
            }

            var resultItems = await TopicItemRepo.All.Where(x => idsToBeLoaded.Contains(x.Id)).ToListAsync();

            var resultItemsDto = _mapper.Map<List<TopicItem_Dto>>(resultItems);

            // Nastavim, jestli o slovicku minule rekl, ze ho neumi
            foreach(var resultItemDto in resultItemsDto)
            {
                var item_user = items_user.FirstOrDefault(x => x.TopicItemId == resultItemDto.Id);
                if(item_user != null && item_user.DontKnow == true)
                {
                    resultItemDto.DontKnow = true;
                }
            }

            // Zamicham
            resultItemsDto = resultItemsDto.OrderBy(a => random.Next()).ToList();

            if (resultItemsDto.Count == 0)
            {
                return BadRequest("Slovíčka se nepodařilo načíst");
            }

            return Ok(new
            {
                items = resultItemsDto
            });
        }

        private async Task<bool> IsFreeDoseExhaustedForToday()
        {
            var activeChallenge = await PersonalChallengeController.GetOrCreateActivePersonalChallenge(PersonalChallengeRepo, UserRepo, UserId);

            var loggedUser = await GetLoggedUser();

            // Je premium -> neme denni omezeni
            if (loggedUser.IsPremium)
            {
                return false;
            }

            var today = DateTime.Now.Date;

            // Kolikrat uz dneska dodelal uceni tematickych slovicek?
            var todayTopicPoints_Count = await TopicPointsRepo.All.Where(x => x.UserId == loggedUser.Id && x.CreatedDate >= today).CountAsync();

            // Uz dneska pustil slovicka mockrat -> vickrat mu to nedovolim - musi si koupit premium nebo pockat do zitra
            if(todayTopicPoints_Count > 4)
            {
                return true;
            }

            return false;

        }


    }
}
