using System;

namespace Pantheon.Core
{
    public enum AuthenticationState
    {
        NewClient,
        Rejected,
        Unauthenticated,
        Authenticated
    }
}