using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AnglickaVyzva.API.Controllers;
using AnglickaVyzva.API.Data;
using AnglickaVyzva.API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AnglickaVyzva.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }
        private IWebHostEnvironment _env { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var dbConnectionStringDEV = Configuration.GetConnectionString("DbConnectionStringDEV");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Nedokázal jsem získat connection string k databázi");


            var pathToDataFolder = Path.Combine(Directory.GetCurrentDirectory(), @"Data/Data");
            Directory.CreateDirectory(pathToDataFolder);

            services.AddControllers();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                var tokenPrivateKey = Configuration["TokenPrivateKey"];

                options.TokenValidationParameters = AuthenticationHelper.GetTokenValidationParameters(tokenPrivateKey);
            });


            services.AddDbContext<DataContext>(x =>
            {
                x.UseSqlServer(connectionString);
            });


            services.AddAutoMapper(typeof(EFLessonRepo).Assembly); // typeof je tam proto, protoze jinak je to Ambiguous. Misto AuthRepository muze bejt cokoli, co je ve stejny Assembly (moc nevim co to znamena)
            services.AddCors();
            services.AddMvc().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            }); // Pro serializace odpovedi prestane pouzivat System.Text.Json, ale zacne pouzivat stary Newtonsoft.Json
            

            // NEBUDU posilat emaily z lokalniho debugu ani z Dev azure serveru -> zakaznikum by chodily zdvojene emaily
            // Taky pro jistotu nebudu spoustet Order a Challenge checker
            var isOnAnyDev = EnvironmentHelper.IsOnAnyDev();
            if (isOnAnyDev == false)
            {
                EmailHelper.RunEndlessEmailSender(
                    connectionString,
                    new EmailHelper.ServerEmailCredentials(Configuration["Email-Login"], Configuration["Email-Password"])
                );

                OrderAndChallengeHelper.RunEndlessOrderAndChallengeChecker(connectionString);
            }


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {




                        var error = context.Features.Get<IExceptionHandlerFeature>();

                        if (error != null)
                        {
                            if (error.Error.Message == PersonalChallengeController.NotPaidException.PredefinedMessage)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                                var message = "Nemáte aktivní pøedplatné";

                                //context.Response.AddApplicationError(message); // Prida to hlavicky do odpovedi
                                context.Response.Headers.Add("Access-Control-Expose-Headers", "not-paid");
                                context.Response.Headers.Add("not-paid", "not-paid");
                                await context.Response.WriteAsync(message);
                            }
                            else
                            {

                                var message = error.Error.Message;
                                if (message == "Sequence contains no elements")
                                    message = "Nenalezeno";

                                context.Response.AddApplicationError(error.Error.Message); // Prida to hlavicky do odpovedi
                                await context.Response.WriteAsync(error.Error.Message);

                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                    });
                });

            }


            app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            );

            // Minimalni verze api v aplikaci. Pokud bude apliace navrzena pro starsi api nez je minimalni, nebude se v ni moci delat nic jineho nez aktualizovat aplikaci.
            app.Use(async (context, nextMiddleware) =>
            {
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey("Access-Control-Expose-Headers")) // V ifu je to proto, protoe kdyz vyskoci vyjimka, tak to sem skace jeste jednou
                    {
                        //context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                        context.Response.Headers.Add("Access-Control-Expose-Headers", "min-target-api-version, www-authenticate, refresh-token, not-paid"); // www-authenticate -> Chybova hlaseni k tokenu
                        context.Response.Headers.Add("min-target-api-version", Configuration.GetSection("AppSettings:MinTargetApiVersion").Value);
                    }
                    return Task.FromResult(0);
                });
                await nextMiddleware();
            });

            
            // BLACK LIST autorizacnich tokeny (LogOutAllDevices)
            app.Use(async (context, nextMiddleware) =>
            {
                // Dotaz obsahuje autorizacni token -> overim ho
                // Nekontroluji pokusy o prihlaseni, protoze temi si ten zastaraly token zmeni
                // Nekontroluji ani odhlaseni
                if (context.Request.Headers.ContainsKey("authorization") && context.Request.Path != "/api/auth/fakeLogin" && context.Request.Path != "/api/auth/login" && context.Request.Path != "/api/auth/logOut")
                {
                    var tokenString = context.Request.Headers["authorization"].ToString().Split(" ")[1];
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(tokenString);

                    int userId = int.Parse(token.Claims.First(x => x.Type == "nameid").Value);

                    var date = await AuthenticationHelper.GetLogOutFromDeviceDate(userId);
                    if (date != null)
                    {
                        if (token.ValidFrom.ToLocalTime() < date)
                        {
                            context.Response.Headers.Add("www-authenticate", "token-on-blacklist");

                            context.Response.OnCompleted(() =>
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                                return Task.FromResult(0);
                            });

                            return; // Tim se nezavola nextMiddleware -> melo by to zacit vytvaret odpoved
                        }
                    }
                }
                await nextMiddleware();
            });

            // Refresh token
            app.Use(async (context, nextMiddleware) =>
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Request.Headers.ContainsKey("authorization")) // Pokud byl soucasti dotazu autorizacni token, podivam se, jestli je za pulkou casu platnosti a pripadne ho refreshnu
                    {
                        var tokenString = context.Request.Headers["authorization"].ToString().Split(" ")[1];
                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(tokenString);

                        if (token.ValidTo.ToLocalTime() > DateTime.Now) // Tokeny s proslou trvanlivosti neobnovuji -> musi se znovu prihlasit
                        {
                            var timeToExpire = token.ValidTo.ToLocalTime() - DateTime.Now;
                            var timeWholePeriod = token.ValidTo.ToLocalTime() - token.ValidFrom.ToLocalTime();

                            // Uz uplynula vice jak polovina celkove doby zivota tokenu -> vytvorim novy
                            if (timeWholePeriod / 2 > timeToExpire)
                            {
                                var id = token.Claims.First(x => x.Type == "nameid").Value;
                                var userName = token.Claims.First(x => x.Type == "unique_name").Value;
                                var newToken = AuthenticationHelper.CreateToken(Configuration["TokenPrivateKey"], id, userName);
                                // Pro web
                                //context.Response.Cookies.Append("authorization", newToken, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(AuthenticationHelper.TokenExpirationTimespan) });
                                // Pro mobily
                                context.Response.Headers.Add("refresh-token", newToken);

                            }
                        }
                    }
                    return Task.FromResult(0);
                });
                await nextMiddleware();
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseStaticFiles();


            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                            Path.Combine(Directory.GetCurrentDirectory(), @"Data/Data")),
                RequestPath = new PathString("/data/data")
            });


            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
