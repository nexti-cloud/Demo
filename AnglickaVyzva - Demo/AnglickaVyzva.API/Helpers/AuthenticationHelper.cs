using AnglickaVyzva.API.Entities;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UAParser;

namespace AnglickaVyzva.API.Helpers
{
    public class AuthenticationHelper
    {
        #region TOKENS
        public static TimeSpan TokenExpirationTimespan = new TimeSpan(365, 3, 0, 0);

        /// <summary>
        /// Jsou zde ulozeny casy, kdy se uzivatel odhlasil ze vsech zarizeni -> starsi autorizacni tokeny nez je toto datum budou brany za neplatne
        /// (Tento seznam se plni pri startu webu v Startup.cs. Pouze potom se pridavaji uzivatele postupne pomoci AddLogOUtFromDevice)
        /// </summary>
        public static Dictionary<int, DateTime> LogOutFromDeviceDictionary = new Dictionary<int, DateTime>();

        /// <summary>
        /// Ulozim do staticke promenne, ze tento uzivatel v tomto case pouzil moznost Odhlasit ze vsech zarizeni
        /// </summary>
        public static async Task AddLogOutFromDevice(int userId, DateTime date)
        {
            try
            {
                await SharedSemaphores.Semaphore_Authentication.WaitAsync();

                if (!LogOutFromDeviceDictionary.ContainsKey(userId))
                {
                    LogOutFromDeviceDictionary.Add(userId, date);
                }
                else
                {
                    LogOutFromDeviceDictionary[userId] = date;
                }
            }
            finally
            {
                SharedSemaphores.Semaphore_Authentication.Release();
            }
        }

        /// <summary>
        /// Kdy naposledy tento uzivatel pouzil Odhlaseni ze vsech zarizeni
        /// </summary>
        public static async Task<DateTime?> GetLogOutFromDeviceDate(int userId)
        {
            try
            {
                await SharedSemaphores.Semaphore_Authentication.WaitAsync();

                DateTime date;

                if (LogOutFromDeviceDictionary.TryGetValue(userId, out date))
                {
                    return date;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                SharedSemaphores.Semaphore_Authentication.Release();
            }
        }

        public static string CreateToken(string tokenPrivateKey, string userId, string userName)
        {
            var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userName),
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenPrivateKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.Add(TokenExpirationTimespan),
                //Expires = DateTime.Now.AddYears(10),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Vraci parametry, ktere se pouzivaji pri vytvareni JWT token handleru. (Aby se vsude pouzivala stejna pravidla)
        /// </summary>
        /// <param name="tokenPrivateKey"></param>
        /// <returns></returns>
        public static TokenValidationParameters GetTokenValidationParameters(string tokenPrivateKey)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenPrivateKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,

            };
        }
        #endregion END TOKENS

        #region AUTHENTICATION

        public class AuthenticationSettings
        {
            public string Google_Redirect_Uri { get; set; }
            public string Facebook_Redirect_Uri { get; set; }
            public string Apple_Redirect_Uri { get; set; }

            public AuthenticationSettings(string google_redirect_uri, string facebook_redirect_uri, string apple_redirect_uri)
            {
                Google_Redirect_Uri = google_redirect_uri;
                Facebook_Redirect_Uri = facebook_redirect_uri;
                Apple_Redirect_Uri = apple_redirect_uri;
            }
        }

        public class WaitForExternalLogin
        {
            // Request by User part
            public DateTime Created { get; set; }
            public string Guid { get; set; }
            public string Provider { get; set; }
            public bool IsTemporary { get; set; }
            public int TemporaryUserId { get; set; }

            // After return URI part
            public bool ReceivedReturnRequest { get; set; }
            public int UserId { get; set; }
            public bool IsCorrect { get; set; }
            public string ErrorMessage { get; set; }
            /// <summary>
            /// Pouziva jinou metodu prihlasovani
            /// </summary>
            public bool UsingAnotherProvider { get; set; }
            /// <summary>
            /// Jakou metodu prihlasovani pouziva
            /// </summary>
            public string UsingLoginMethod { get; set; }
        }

        /// <summary>
        /// Pristup je chraneny semaforem
        /// </summary>
        public static List<WaitForExternalLogin> WaitForExtarnalLoginList = new List<WaitForExternalLogin>();

        public static readonly int ExternalLoginTimeoutMinutes = 5;

        /// <summary>
        /// Kdy se naposledy resetovaly zaznamy o poctech pokusu o zadani hesla
        /// </summary>
        public static DateTime MaxLoginRequestsDictionary_LastReset = DateTime.Now;

        /// <summary>
        /// Pocita, kolik bylo pro konkretni email poctu pokusu o zadani hesla. Pri pokusu o zadani heslo se zkontroluje, jestli je tento seznam starsi nez 10 minut, pokud ano, resetuji ho.
        /// </summary>
        public static Dictionary<string, int> MaxLoginRequestsDictionary = new Dictionary<string, int>();

        public static class LoginMethods
        {
            public const string Email = "email";
            public const string Google = "google";
            public const string Facebook = "facebook";
            public const string Apple = "apple";
        }

        private static Random random = new Random();
        public static string RandomNumericString(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
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

        public static bool IsEmailValid(string email)
        {
            string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|" + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)" + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(email);
        }

        public static bool IsPasswordSecure(string password)
        {
            var message = GetPasswordSecureErrorMessage(password);

            if (message != null)
            {
                return false;
            }
            return true;
        }

        public static string GetPasswordSecureErrorMessage(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "Heslo nesmí být prázdné";
            }

            if (password.Length < 6)
            {
                return "Heslo musí mít minimálně 6 znaků";
            }

            return null;
        }

        public static Guid CreateCryptographicallySecureGuid()
        {
            using (var provider = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[16];
                provider.GetBytes(bytes);

                return new Guid(bytes);
            }
        }

        //public static void AddAuthorizationCookie(HttpResponse response, HttpRequest request, string token)
        //{
        //    var sameSite = SameSiteMode.None;
        //    if (request.Headers.ContainsKey("User-Agent"))
        //    {
        //        var uaString = request.Headers["User-Agent"];
        //        //var uaString = "Mozilla/5.0 (iPad; CPU OS 13_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.5 Mobile/15E148 Safari/604.1";
        //        var uaParser = UAParser.Parser.GetDefault();
        //        var clientInfo = uaParser.Parse(uaString);

        //        if(clientInfo.UA.Family == "Mobile Safari")
        //        {
        //            //sameSite = SameSiteMode.Unspecified;

        //            //response.Cookies.Append("susenkaProAppleJedna", "jednicka", new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(TokenExpirationTimespan), SameSite = sameSite, Secure = true });
        //            //response.Cookies.Append("susenkaProAppleDva", "dvojka", new CookieOptions { /*HttpOnly = true,*/ Expires = DateTime.Now.Add(TokenExpirationTimespan), SameSite = sameSite, Secure = true });
        //            //response.Cookies.Append("susenkaProAppleTri", "trojka", new CookieOptions { /*HttpOnly = true,*/ Expires = DateTime.Now.Add(TokenExpirationTimespan)/*, SameSite = sameSite*/, Secure = true });
        //            //response.Cookies.Append("susenkaProAppleCtyri", "ctyrka", new CookieOptions { /*HttpOnly = true,*/ Expires = DateTime.Now.Add(TokenExpirationTimespan)/*, SameSite = sameSite*/ /*, Secure = true */});
        //            //response.Cookies.Append("susenkaProApplePet", "petka");
        //            //response.Cookies.Append("susenkaProAppleSest", "sestka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net" });
        //            //response.Cookies.Append("susenkaProAppleSedm", "sedmicka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net", HttpOnly = true });
        //            //response.Cookies.Append("susenkaProAppleOsm", "osmicka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net", HttpOnly = true, SameSite = SameSiteMode.None });
        //            //response.Cookies.Append("susenkaProAppleDevet", "devitka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net", HttpOnly = true, SameSite = SameSiteMode.None });
        //            //response.Cookies.Append("susenkaProAppleDeset", "desitka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net", HttpOnly = true, SameSite = SameSiteMode.None, Expires = DateTime.Now.Add(TokenExpirationTimespan) });
        //            //response.Cookies.Append("susenkaProAppleJedenact", "jedenactka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net", SameSite = SameSiteMode.None, Expires = DateTime.Now.Add(TokenExpirationTimespan) });
        //            //response.Cookies.Append("susenkaProAppleDvanact", "dvanactka", new CookieOptions { Domain = "Domain=anglicka-vyzva-api.azurewebsites.net", SameSite = SameSiteMode.None, Expires = DateTime.Now.Add(TokenExpirationTimespan), Secure=true });

        //            //response.Cookies.Append("susenkaProAppleSest", "sestka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net" });
        //            //response.Cookies.Append("susenkaProAppleSedm", "sedmicka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net", HttpOnly = true });
        //            //response.Cookies.Append("susenkaProAppleOsm", "osmicka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net", HttpOnly = true, SameSite = SameSiteMode.None });
        //            //response.Cookies.Append("susenkaProAppleDevet", "devitka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net", HttpOnly = true, SameSite = SameSiteMode.None });
        //            //response.Cookies.Append("susenkaProAppleDeset", "desitka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net", HttpOnly = true, SameSite = SameSiteMode.None, Expires = DateTime.Now.Add(TokenExpirationTimespan) });
        //            //response.Cookies.Append("susenkaProAppleJedenact", "jedenactka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net", SameSite = SameSiteMode.None, Expires = DateTime.Now.Add(TokenExpirationTimespan) });
        //            //response.Cookies.Append("susenkaProAppleDvanact", "dvanactka", new CookieOptions { Domain = "Domain=anglicka-vyzva-app.azurewebsites.net", SameSite = SameSiteMode.None, Expires = DateTime.Now.Add(TokenExpirationTimespan), Secure = true });
        //        }
        //    }

        //    response.Cookies.Append("authorization", token, new CookieOptions { HttpOnly = true, Expires = DateTime.Now.Add(TokenExpirationTimespan), SameSite = sameSite, Secure = true });
        //}

        #region CODE VERIFICATION

        public partial class TupleBase
        {
            public DateTime Created { get; set; }
            public string Code { get; set; }
            /// <summary>
            /// Kolikrat se nekdo pokusil zadat kod k teto konkretni zadosti o zmenu
            /// </summary>
            public int Attempts { get; set; }

            public TupleBase(string code)
            {
                Created = DateTime.Now;
                Code = code;

            }
        }

        public class TupleUserName : TupleBase
        {
            public int UserId { get; set; }
            public TupleUserName(string code, int userId) : base(code)
            {
                UserId = userId;
            }
        }
        public class TupleEmail : TupleBase
        {
            public int UserId { get; set; }
            public string Email { get; set; }
            public TupleEmail(string code, int userId, string email) : base(code)
            {
                UserId = userId;
                Email = email;
            }
        }
        public class TuplePhone : TupleBase
        {
            public int UserId { get; set; }
            public string Phone { get; set; }
            public TuplePhone(string code, int userId, string phone) : base(code)
            {
                UserId = userId;
                Phone = phone;
            }
        }

        public class TupleResetPassword : TupleBase
        {
            public string UserName { get; set; }
            public TupleResetPassword(string code, string userName) : base(code)
            {
                UserName = userName;
            }


        }

        public class CheckBaseTupleResponse
        {
            public bool IsCorrect { get; set; }
            public string LogDescriptionError { get; set; }
            public string ErrorMessage { get; set; }
        }


        //              UserId
        public static Dictionary<int, TupleUserName> UserNameVerificationCodesDictionary = new Dictionary<int, TupleUserName>();
        //              UserId
        public static Dictionary<int, TupleEmail> EmailVerificationCodesDictionary = new Dictionary<int, TupleEmail>();
        //              UserId,   Vytvoreno, Tel.Cislo, Kod
        public static Dictionary<int, TuplePhone> PhoneVerificationCodesDictionary = new Dictionary<int, TuplePhone>();
        public static Dictionary<int, TupleResetPassword> ResetPasswordVerificationCodesDictionary = new Dictionary<int, TupleResetPassword>();


        public static readonly int codeLength = 6;
        public static readonly int codeLifetimeMinutes = 5;

        /// <summary>
        /// Zkontroluje zakladi veci u ntice, ktere se musi kontrolovat u kazde metody
        /// </summary>
        /// <param name="tuple"></param>
        public static CheckBaseTupleResponse CheckBaseTuple(TupleBase tuple, string code)
        {
            if (tuple == null)
            {
                return new CheckBaseTupleResponse
                {
                    IsCorrect = false,
                    LogDescriptionError = Log.DescriptionsPredefined.VerificationCodeNotFound,
                    ErrorMessage = "Špatný ověřovací kód"
                };
            }

            if (tuple.Attempts > 3)
            {
                return new CheckBaseTupleResponse
                {
                    IsCorrect = false,
                    LogDescriptionError = Log.DescriptionsPredefined.TooManyAttempts,
                    ErrorMessage = "Překročen maximální počet pokusů"
                };
            }

            // Platnost kodu vyprsela
            if (tuple.Created.AddMinutes(codeLifetimeMinutes) < DateTime.Now)
            {
                return new CheckBaseTupleResponse
                {
                    IsCorrect = false,
                    LogDescriptionError = Log.DescriptionsPredefined.Expired,
                    ErrorMessage = "Špatný ověřovací kód"
                };
            }

            // Spatny kod
            if (tuple.Code != code)
            {
                return new CheckBaseTupleResponse
                {
                    IsCorrect = false,
                    LogDescriptionError = Log.DescriptionsPredefined.VerificationCodeNotMatch,
                    ErrorMessage = "Špatný ověřovací kód"
                };
            }

            tuple.Attempts++;

            return new CheckBaseTupleResponse { IsCorrect = true };
        }

        public static void TuplesGarbageCollector<T>(Dictionary<int, T> dictionary)
        {
            // Nebudu to volat pokazde
            if (dictionary.Count > 300 /*magicke cislo*/)
            {
                // Smazu vsechny pozadavky starsi nex 2x zivotnost dotazu. 2x je tam proto, abych mohl jestli chvilku zobrazovat hlasku, ze kod uz vyprsel.
                var thresholdDate = DateTime.Now.AddMinutes(-codeLifetimeMinutes * 2);

                foreach (var key in dictionary.Keys)
                {
                    TupleBase tuple = dictionary[key] as TupleBase;

                    // Pozadavek je moc stary -> smazu ho
                    if (tuple.Created < thresholdDate)
                    {
                        dictionary.Remove(key);
                    }
                }
            }
        }

        #endregion END CODE VERIFICATION

        #region Hash Password

        public static string GenerateSalt()
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: Convert.FromBase64String(salt),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed;
        }

        public static bool ComparePassword(string password, string salt, string hash)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            string pwd = HashPassword(password, salt);
            if (pwd == hash)
            {
                return true;
            }

            return false;
        }

        #endregion END Hash Password

        #endregion END AUTHENTICATION
    }
}
