using System;

namespace Pantheon.Client
{
    public enum ClientControlCode
    {
        Invalid = 0,

        Client_Hello = 1,
        Client_HelloResp = 2,
        Client_Disconnecting = 3,
        Client_Eject = 4,
        Client_Heartbeat = 5,
        Client_Authenticated = 6,

        Client_SendDatagram = 100,
        Client_GenerateObject = 101,
        Client_GenerateObjectOwner = 102,
        Client_DestroyObject = 103,

        Client_ObjectSetField = 110,
        Client_ObjectGetField = 111,

        Client_SendRPCResp = 120,
        Client_SendRPC = 121,

        Client_AddInterest = 200,
        Client_AddInterestRange = 201,
        Client_AddInterestMultiple = 202,

        Client_RemoveInterest = 210,
        Client_RemoveInterestRange = 211,
        Client_RemoveInterestMultiple = 212,

        Client_DiscoverService = 220,
        Client_DiscoverServiceResp = 221,
        Client_CloseService = 230,

        Client_AddInterestResp = 300,
        Client_AddInterestRangeResp = 301,
        Client_AddInterestMultipleResp = 302,
        Client_RemoveInterestResp = 310,
        Client_RemoveInterestRangeResp = 311,
        Client_RemoveInterestMultipleResp = 312,

        Client_DiscoverObjectChildren = 400,

        Client_ObjectMoved = 410,
        Client_ObjectLeft = 411,
    }
}