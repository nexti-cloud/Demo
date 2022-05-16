using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

namespace AnglickaVyzva.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : BaseController
    {
        public FeedbackController(IConfiguration config, IMapper mapper, DataContext dbContext, IWebHostEnvironment env) : base(config, mapper, dbContext, env)
        {
        }

        public class SendFeedback_Model { public string Dump { get; set; } public string Message { get; set; } public string ImageBase64 { get; set; } }
        [HttpPost("SendFeedback")]
        public async Task<IActionResult> SendFeedback(SendFeedback_Model model)
        {

            string urlToFeedbackScreenshot;
            string fileName = null;

            if(!string.IsNullOrWhiteSpace(model.ImageBase64))
            {
                // Odeberu prefix pred Base64 daty (data:/image/jpeg;base64,...az ted jsou data obrazku...)
                var withoutPrefix = model.ImageBase64;
                if (model.ImageBase64.StartsWith("data:"))
                {
                    var spl = withoutPrefix.Split('/')[1];
                    var format = spl.Split(';')[0];
                    withoutPrefix = withoutPrefix.Replace($"data:image/{format};base64,", String.Empty);
                }

                var bytes = Convert.FromBase64String(withoutPrefix);

                var stream = new MemoryStream(bytes);


                var userIdStr = "noUserId";
                if (UserIdOrDefault != null)
                {
                    userIdStr = "userId_" + UserId;
                }
                fileName = DateTime.Now.ToString() + "_" + userIdStr + "_" + Guid.NewGuid() + ".png";

                BlobContainerClient container = await StorageHelper.CreateContainer(StorageHelper.Container_Feedback);
                await StorageHelper.UploadFile(container, fileName, stream);

                urlToFeedbackScreenshot = $"https://anglickavyzva.blob.core.windows.net/feedback/{fileName}";
            }
            else
            {
                urlToFeedbackScreenshot = "Nepodařilo se vytvořit screenshot";
            }

            


            EmailRepo.Add(
                        new Email("zbyneklazarek@gmail.com", $"Feedback", $"{model.Message} <br /><br />  {urlToFeedbackScreenshot}"));

            var feedback = new Feedback
            {
                Dump = model.Dump,
                Message = model.Message,
                ImageName = fileName,
            };

            FeedbackRepo.Add(feedback);
            await FeedbackRepo.SaveAll();

            return Ok();
        }
    }
}
