using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class StringHelper
    {
        public static string GetPhoneCorrectFormat(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new Exception("Telefonní číslo má špatný tvar");
            }
            var str = phone.Trim().Replace(" ", "");

            // Mene znaku nez 789 456 123
            if (str.Length < 9)
            {
                throw new Exception("Telefonní číslo má špatný tvar");
            }

            // Vice znaku nez +420 789 456 123
            if (str.Length > 13)
            {
                throw new Exception("Telefonní číslo má špatný tvar");
            }


            if (str.Length == 9)
            {
                if (!str.All(char.IsDigit))
                {
                    throw new Exception("Telefonní číslo má špantý tvar");
                }
            }
            else if (str.Length == 13)
            {
                if (str[0] != '+')
                {
                    throw new Exception("Telefonní číslo má špatný tvar");
                }

                for (int i = 1 /*preskakuji uvodni +*/; i < str.Length; i++)
                {
                    char c = str[i];
                    if (c < '0' || c > '9')
                    {
                        throw new Exception("Telefonní číslo má špatný tvar");
                    }
                }
            }

            // Cislo je bez predcisli -> predpokladam, ze je ceske
            if (str.Length == 9)
            {
                str = "+420" + str;
            }

            return str;
        }
    }
}
