using AnglickaVyzva.API.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class EmailHelper
    {
        static SemaphoreSlim semaphoreEndlessRunner = new SemaphoreSlim(1, 1);

        public const string EmailAddressForLog = "podpora@nexti.cz";



        public class ServerEmailCredentials
        {
            public string FromAddress { get; set; }
            public string FromAddressPassword { get; set; }
            public ServerEmailCredentials(string fromAddress, string fromAddressPassword)
            {
                FromAddress = fromAddress;
                FromAddressPassword = fromAddressPassword;
            }
        }

        private static async Task SendEmail(ServerEmailCredentials serverEmailCredentials, string toAddress, string subject, string body)
        {
            await SendEmail(serverEmailCredentials.FromAddress, toAddress, subject, body, serverEmailCredentials.FromAddressPassword);
        }

        private static async Task SendEmail(string fromAddress, string toAddress, string subject, string body, string fromAddressPassword)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromAddress, "Anglická výzva!");
            //mail.Bcc.Add("no-reply@nexti.cz");
            mail.IsBodyHtml = true;
            mail.Subject = subject;
            mail.Body = body;


            SmtpClient smtp = new SmtpClient("smtp.office365.com", 587);
            smtp.Credentials = new NetworkCredential(fromAddress, fromAddressPassword);
            smtp.EnableSsl = true;


            mail.To.Add(toAddress);

            //var emailAddresses = document.SendEmailTo.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            //foreach (var emailAddress in emailAddresses)
            //{
            //    mail.To.Add(emailAddress);
            //}

            await smtp.SendMailAsync(mail);
        }



        public static void SendLogEmail(ServerEmailCredentials serverEmailCredentials, string title, string body)
        {
            _ = Task.Run(async () =>
            {
                await SendEmail(
                    serverEmailCredentials,
                    EmailAddressForLog,
                    title,
                    GenerateEmail(title, body)
                    );

            });
        }

        public static void RunEndlessEmailSender(string connectionString, ServerEmailCredentials emailServerCredentials)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                try
                {
                    await semaphoreEndlessRunner.WaitAsync();

                    while (true)
                    {
                        //Debug.WriteLine("Stahuju emaily");

                        try
                        {
                            var emailRepo = new EFEmailRepo(
                                new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlServer(connectionString).Options),
                                    new SaveParameters
                                    {
                                        UserIp = "cron",
                                        UserName = "cron",
                                        Host = "cron",
                                        //logHelperCredentials: new HelpersStandard.LogHelper.LogHelperCredentials(Configuration["Storage-Log-Account-Name"], Configuration["Storage-Log-Key"])
                                    }
                            );

                            var emailsToSend = await emailRepo.All.Where(x => x.SendTime == null).ToListAsync();
                            foreach (var email in emailsToSend)
                            {
                                await SendEmail(emailServerCredentials, email.ToAddress, email.Subject, email.Body);

                                email.SendTime = DateTime.Now;
                                await emailRepo.SaveAll();
                            }
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine("Send email ERROR: ", exc.ToString());
                            //throw exc;
                        }

                        await Task.Delay(3000);
                    }

                }
                catch (Exception exc)
                {
                    throw exc;
                }
                finally
                {
                    semaphoreEndlessRunner.Release();
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private static string Base64UrlEncode(string input)
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            // Special "url-safe" base64 encode.
            return Convert.ToBase64String(inputBytes)
              .Replace('+', '-')
              .Replace('/', '_')
              .Replace("=", "");
        }

        public static string GenerateEmail(string title, string body)
        {
            return $"" +
                $"<!-- Container -->" +
                $"<div style='width: 100%; background-color: #f4f4f4; padding-top: 20px; padding-bottom: 20px'>  " +
                $"  <!-- Stredovy sloupec -->  " +
                $"  <div    style='color: #000000; font-family: Helvetica; font-size: 16px; line-height: 25px; margin: 0 auto; max-width: 600px'>    " +
                $"      <!-- Obsah -->    " +
                $"      <div style='background-color:#fff;padding: 9px 25px 25px 25px'>      " +
                $"          <!-- Nadpis -->      " +
                $"          <div style='font-weight: 600; color: #5a5a5a; margin-bottom: 20px; margin-top: 10px;'>" +
                $"              {title}" +
                $"          </div>      " +
                $"          <!-- END Nadpis -->     " +
                $"           <!-- Text -->      " +
                $"              <div>" +
                $"                  {body}" +
                $"              </div>      " +
                $"          <!-- END Text -->    " +
                $"      </div>    " +
                $"      <!-- END Obsah -->    " +
                $"      <!-- Paticka -->    " +
                $"      <div style='font-size: 13px; color:#757575; background-color: #74bd07'>      " +
                $"          <!-- Logo wrapper-->" +
                $"          <div style='text-align: center'>       " +
                $"              <a href='https://anglickavyzva.cz' style='text-decoration: none;'>          " +
                $"                  <img style='' src='https://anglickavyzva.cz/assets/images/anglicka-vyzva-logo-email-70.png' height='50px' alt='logo Anglická výzva!' title='logo Anglická výzva!'>        " +
                $"              </a>      " +
                $"          </div>    " +
                $"      </div>    " +
                $"      <!-- END Paticka -->  " +
                $"  </div>  " +
                $"  <!-- END Stredovy sloupec -->" +
                $"</div>" +
                $"<!-- END Container -->";
        }
    }
}

