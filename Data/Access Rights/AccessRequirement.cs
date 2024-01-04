using Microsoft.AspNetCore.Authorization;

namespace _4PL.Data.Access_Rights
{
    public class AccessRequirement : IAuthorizationRequirement
    {
        public string AccessRight {  get; set; }

        public AccessRequirement(string accessRight)
        {
            AccessRight = accessRight.Trim();
        }
    }
}
