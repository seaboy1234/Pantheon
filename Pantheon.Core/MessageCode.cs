namespace Pantheon.Core
{
    public enum MessageCode
    {
        Invalid = 0,

        ALL_QueryChannel = 900,

        ClientAgent_SetStatus = 1000,
        ClientAgent_SetClientId = 1001,
        ClientAgent_SendDatagram = 1002,
        ClientAgent_Eject = 1003,

        ClientAgent_OpenChannel = 1100,
        ClientAgent_CloseChannel = 1101,
        ClientAgent_SetZoneServer = 1102,

        ClientAgent_AddInterest = 1200,
        ClientAgent_AddInterestRange = 1201,
        ClientAgent_AddInterestMultiple = 1202,

        ClientAgent_RemoveInterest = 1210,
        ClientAgent_RemoveInterestRange = 1211,
        ClientAgent_RemoveInterestMultiple = 1212,

        ClientAgent_PurgeInterest = 1220,

        ClientAgent_Generate = 1300,
        ClientAgent_Destroy = 1301,
        ClientAgent_ObjectChangedZones = 1302,

        MessageDirector_AddInterest = 2000,
        MessageDirector_AddInterestRange = 2001,
        MessageDirector_AddInterestMultiple = 2002,

        MessageDirector_RemoveInterest = 2100,
        MessageDirector_RemoveInterestRange = 2101,
        MessageDirector_RemoveInterestMultiple = 2102,

        MessageDirector_QueueOnDisconnect = 2200,

        StateServer_Generate = 3000,
        StateServer_GenerateWithDoid = 3001,
        StateServer_Destroy = 3002,
        StateServer_DispenseDoId = 3003,

        StateServer_GenerateResp = 3010,
        StateServer_DestroyResp = 3012,

        StateServer_ObjectSetParent = 3100,

        StateServer_ObjectGetRequired = 3110,
        StateServer_ObjectGetAll = 3111,
        StateServer_ObjectGetField = 3112,

        StateServer_ObjectGetRequiredResp = 3120,
        StateServer_ObjectGetAllResp = 3121,
        StateServer_ObjectGetFieldResp = 3122,

        StateServer_ObjectSetField = 3130,
        StateServer_ObjectSetFields = 3131,

        StateServer_CreateOwner = 3200,
        StateServer_RemoveOwner = 3201,
        StateServer_MoveOwner = 3202,
        StateServer_CreateOwnerResp = 3210,
        StateServer_RemoveOwnerResp = 3211,
        StateServer_MoveOwnerResp = 3212,

        StateServer_UpdateObject = 3300,

        StateServer_DiscoverChildren = 3400,
        StateServer_DiscoverChildrenResp = 3410,

        StateServer_AllEntitiesSent = 3500,

        StateServer_CreateZone = 3600,
        StateServer_RemoveZone = 3601,
        StateServer_CreateServer = 3610,
        StateServer_CloseServer = 3611,

        StateServer_CreateZoneResp = 3620,

        Database_GetObject = 4000,
        Database_SaveObject = 4001,

        Database_GetObjectResp = 4100,
        Database_SaveObjectResp = 4101,

        DObject_BroadcastRpc = 5000,
        DObject_BroadcastRpcResp = 5001,
        DObject_BroadcastUpdate = 5100,
    }
}