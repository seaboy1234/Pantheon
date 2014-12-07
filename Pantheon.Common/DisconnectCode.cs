using System;

namespace Pantheon.Common
{
    public enum DisconnectCode
    {
        Invalid = 0,
        ERR_PacketSize = 100,

        CA_InvalidHello = 200,
        CA_InvalidMessage = 201,
        CA_NoHeartbeat = 202,
        CA_IOException = 203,

        ERR_Unauthenticated = 300,
        ERR_BadInterest = 301,
        ERR_Outdated = 302,
        ERR_ObjectDef = 303,
        ERR_BadService = 304,

        GAME_MultipleLogins = 500,
        GAME_LoginFailed = 501,
        GAME_Permissions = 502,
        GAME_KickedAdmin = 503,
        GAME_KickedRules = 504,
        GAME_ShardShutdown = 505,
        GAME_Security = 510,
    }
}