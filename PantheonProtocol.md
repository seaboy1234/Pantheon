# Pantheon Server Protocol Reference
| Component | Message Name | Code | Structure |
|:---------:|:-------------|-----:|:---------:|
|All        | QueryChannel |0900   |`string`   |
|ClientAgent| SetStatus    |1000  |``|
|ClientAgent|SetClientId   |1001  |``|
|ClientAgent|SendDatagram  |1002  |``|
|ClientAgent|Eject         |1003  |`int, string`|
|ClientAgent|OpenChannel   |1100  |`uint64`|
|ClientAgent|CloseChannel  |1101  |`uint64`|
|ClientAgent|SetZoneServer |1102  |`uint64`|
|ClientAgent|AddInterest   |1200  |`uint64`|
|ClientAgent|AddInterestRange       |1201| `uint64, uint64`|
|ClientAgent|AddInterestMulitple    |1202| `int, uint64[]`|
|ClientAgent|RemoveInterest         |1210| `uint64`|
|ClientAgent|RemoveInterestRange    |1211| `uint64, uint64`|

