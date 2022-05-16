using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Entities
{
    public class Log : BaseEntity
    {
        public string Action { get; set; }

        public string Description { get; set; }
        public string Values { get; set; }
        public string Json { get; set; }

        public string EntityName { get; set; }
        public int EntityId { get; set; }

        public string Host { get; set; } //Web host

        public Log() { }

        public Log(string action)
        {
            Action = action;
        }

        public Log(string action, string description)
        {
            Action = action;
            Description = description;
        }

        public Log(string action, string description, string values)
        {
            Action = action;
            Description = description;
            Values = values;
        }

        [NotMapped]
        public class Actions
        {
            public const string Login = "login";
            public const string LoginExternalRedirect = "login external redirect";
            public const string LoginExternalWaitCheck = "login external wait check";
            public const string LoginFake = "login fake";
            public const string LoginByEmail = "login by email";

            public const string RegisterByEmail = "register by email";
            /// <summary>
            /// Docasny uzivatel se zmenil na prihlaseni pomoci emailu
            /// </summary>
            public const string RegisterByEmail_Temporary = "register by email temporary";

            public const string Logout = "logout";
            public const string LogoutAllDevices = "logout all devices";

            public const string Create = "create";
            public const string Update = "update";
            public const string Delete = "delete";

            public const string PasswordChange = "password change";
            public const string PasswordResetCodeSend = "password reset code send";
            public const string PasswordResetCodeCheck = "password reset code check";


            public const string NotificationTokenRegistration = "notification token registration";

            public const string VerificationCodeSend_UserName = "verification code send username";
            public const string VerificationCodeSend_Email = "verification code send email";
            public const string VerificationCodeSend_Phone = "verification code send phone";

            public const string VerificationCodeCheck_UserName = "verification code check username";
            public const string VerificationCodeCheck_Email = "verification code check email";
            public const string VerificationCodeCheck_Phone = "verification code check phone";

        }

        [NotMapped]
        public class DescriptionsPredefined
        {
            public const string Success = "success";
            public const string ErrorUndefined = "error undefined";

            public const string LoginBadMethod = "login bad method";
            public const string TooEarly = "too early";
            public const string EmailBadFormat = "email bad format";
            public const string EmailNotFound = "email not found";
            public const string EmailAlreadyInUse = "email already in use";
            public const string AlreadyVerified = "already verified";
            public const string UserNotFound = "user not found";
            public const string PasswordNotProvided = "password not provided";
            public const string PasswordWrong = "password wrong";
            public const string PasswordWeak = "password weak";
            public const string VerificationCodeNotFound = "verification code not found";
            public const string VerificationCodeNotMatch = "verification code not match";
            public const string UserNotMatch = "user not match";
            public const string GuidNotFound = "guid not found";
            public const string GuidNotProvided = "guid not provided";
            public const string GuidExpired = "guid expired";
            public const string ResponseAlreadyArrived = "response already arrived";
            public const string IdNotMathch = "id not match";
            public const string PhoneNotFound = "phone not found";
            public const string Expired = "expired";
            public const string TooManyAttempts = "too many attempts";
            public const string ChangeUnconfirmedLoginEmail_EmailExists = "change unconfirmed login email - another user with the same email exists";
            public const string MergeProgress = "merge progress";

        }
    }
}
