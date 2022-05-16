using AnglickaVyzva.API.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class AdminHelper
    {
        public static void CheckAuthorization(User loggedUser)
        {
            var adminUserNames = new string[] { "zbyneklazarek@gmail.com" };

            if (!adminUserNames.Contains(loggedUser?.UserName))
            {
                throw new Exception("Nepovolený přístup");
            }

            // Kdyby se vymazala DB a my bychom si jeste nestihli vytvorit ucty, tak si nekdo muze vyrobit nase a delat za nas admina.
            if (!loggedUser.IsUserNameVerified)
            {
                throw new Exception("Přihlašovací email není ověřený");
            }
        }
    }
}
