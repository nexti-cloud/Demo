using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Vytvori seznam, ve kterem ma kazdy uzivatel nastaveno, od jakeho casu v minulosti jsou jeho autorizacni tokeny neplatne (LogOutAllDevices)
        /// </summary>
        /// <param name="service"></param>
        /// <param name="connectionString"></param>
        public static void AddAuthorizationTokensBlackList(this IServiceCollection service, string connectionString)
        {
            var options = SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder<DataContext>(), connectionString).Options;
            DataContext dbContext = new DataContext(options);
            var userRepo = new EFUserRepo(dbContext, null);
            var userIdsAndDates = userRepo.All
                .Where(x => x.LogOutAllDevicesDate != null)
                .Select(x => new
                {
                    x.Id,
                    x.LogOutAllDevicesDate
                })
                .ToList();

            foreach (var userIdAndDate in userIdsAndDates)
            {
                AuthenticationHelper.LogOutFromDeviceDictionary.Add(userIdAndDate.Id, (DateTime)userIdAndDate.LogOutAllDevicesDate);
            }
        }

        public static void AddApplicationError(this HttpResponse response, string message)
        {
            response.Headers.Add("Application-Error", WebUtility.UrlEncode(message));
            response.Headers.Add("Access-Control-Expose-Headers", "Application-Error");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
        }

        public static string GetDayNameShort(this DateTime day)
        {
            switch (day.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return "ne";
                case DayOfWeek.Monday:
                    return "po";
                case DayOfWeek.Tuesday:
                    return "út";
                case DayOfWeek.Wednesday:
                    return "st";
                case DayOfWeek.Thursday:
                    return "čt";
                case DayOfWeek.Friday:
                    return "pá";
                case DayOfWeek.Saturday:
                    return "so";
                default:
                    throw new Exception("Špatný den");
            }
        }
    }
}
