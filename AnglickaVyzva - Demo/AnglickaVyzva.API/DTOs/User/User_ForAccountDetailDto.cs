using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.DTOs.User
{
    public class User_ForAccountDetailDto
    {
        public int Id { get; set; }
        public string Username { get; set; } // Prihlasovaci email
        public string Email { get; set; } // Kontaktni email
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public bool IsUserNameVerified { get; set; }
        public bool IsEmailVerified { get; set; }
        public string LoginMethod { get; set; }

        public string DisplayName { get; set; }

        public bool IsCompletelyRegistered { get; set; }
        public string CompletelyRegisteredErrorMessage { get; set; }

        public bool IsTemporary { get; set; }

        public bool IsPremium { get; set; }
        public DateTime? PrepaidUntil { get; set; }
        public string SubscriptionType { get; set; }
        public bool? DoNotRenewSubscription { get; set; }
    }
}
