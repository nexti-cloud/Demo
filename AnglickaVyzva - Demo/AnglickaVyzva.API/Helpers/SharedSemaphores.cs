using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class SharedSemaphores
    {
        public static readonly SemaphoreSlim Semaphore_Authentication = new SemaphoreSlim(1, 1);
        public static readonly SemaphoreSlim Semaphore_UserAccount_UserName = new SemaphoreSlim(1, 1);
        public static readonly SemaphoreSlim Semaphore_UserAccount_Email = new SemaphoreSlim(1, 1);
        public static readonly SemaphoreSlim Semaphore_UserAccount_Phone = new SemaphoreSlim(1, 1);
        public static readonly SemaphoreSlim Semaphore_Auth_WaitForExternalLogin = new SemaphoreSlim(1, 1);
        public static readonly SemaphoreSlim Semaphore_Auth_ResetPassword = new SemaphoreSlim(1, 1);
        public static readonly SemaphoreSlim Semaphore_Auth_MaxLoginRequests = new SemaphoreSlim(1, 1);
    }
}
