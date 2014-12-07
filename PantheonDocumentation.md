# Pantheon Server

## Introduction
The Pantheon Distributed Server System is a framework for developing Multiplayer Online Games (MOGs) that can scale under pressure.  Pantheon contains several built-in services to help you get started, but the overall structure of your cluster is up to you.  Using Pantheon, you gain access to the integrated services framework, which allows for you to implement your game components using familiar C# method syntax; no messing around with low level sockets -- just your game logic.  Pantheon also includes a Distributed Object system which is intended to help you create your game's world.

### Streams no More; RPC Galore!
Using Pantheon's powerful Service Proxy system, there is no need to worry yourself with writing directly to your network streams.  Create your Service Client using the simple Service Proxy system and define some `[RemoteMethod]`s on your remote calls.

### Don't Care What Happened There?
It is never a good idea to let a client see the entire game world.  In order to reduce O(n^2) networking complexity, Pantheon segments your world into different zones which exist on their own Zone Server.  This system is open-ended enough that it allows you to define your zones how you see fit.  No fuss.

### Persist it All, Remember it All
But not everything!  Pantheon allows you to choose what you save to the database and when.  Just players?  No problem.  Anything else?  Let me know where it goes.  The best part: if you're not quite happy with our binary backend, you can use one of our other backends or write your own.

### Choose What Goes Where
No more defining custom logic for each and every one of your networked components.  With Pantheon's Distributed Object system, you can choose what happens with your object's data.  Using several attributes affixed to your base Type's properties and methods, you can choose what data gets stored in `[Ram]`, in the `[Db]`, or anywhere else.  This system also provides security measures that protect from unauthorized writes.  Affix a `[ClientSend]` attribute to allow any client to write that, or reserve it for `[OwnerSend]` -- or keep it safe in the server.

### All at a Price You Can't Resist -- Free
That's right zero dollars and zero cents.  Not only that, but it's open source too.  Feel free to rummage through the code and pick out what you want or improve it and help us all out.

## System Requirements
Pantheon.Client - .NET Framework 3.5+ or equivalent.  
Pantheon.Client.Extensions - .NET Framework 4.5+ or equivalent.  
Pantheon.Server - .NET Framework 4.5+ or equivalent.

## Files
### Client
* Pantheon.Client.dll - Includes core client components for communicating with a Pantheon Client Agent.
* Pantheon.Client.Extensions.dll **OPTIONAL** - Includes extension component adding async/await support to Datagrams.
### Server and Client
* Pantheon.Common.dll - Includes core components for operating and communicating using Pantheon's messaging framework.
* Lidgren.Network.dll - Includes networking components used in Pantheon's messaging backend.

### Server Files
* Pantheon.Core.dll - Provides core components for communicating within Pantheon's internal subsystems.
* Pantheon.ClientAgent.dll - Provides components for client communications within a Pantheon cluster.
* Pantheon.MessageDirector.dll - Provides a central Message Director for use inside a Pantheon cluster.
* Pantheon.StateServer.dll - Provides components for storing state of Distributed Objects.
* Pantheon.Database.dll - Provides components for persisting Distributed Objects.

## Components
### Message Director (MD)
The Message Director is the beating heart of any Pantheon Server cluster.  The Message Director routes messages over the network to other Message Directors.  The system works on a publish-subscribe model, allowing programs to add interest to any number of channels.  The number of possible channels is 2^64.  The Message Director, along with the Client Agent, represent the only "true" servers in the Pantheon System.

### Message Router (MR)
A Message Router attaches itself to a Message Director's `OnMessagesAvailable` event and maps the messages to their intended recipients.  It is very common for a component requiring the ability to receive message to require a Message Router be passed to it as an argument.

### Client Agent (CA)
The Client Agent is responsible for all communication between clients and the server cluster as a whole.  A Client Agent is also the primary security apparatus in a Pantheon Server Cluster.  This is because it is the entry point that clients use to access the other components of the Pantheon Cluster.  

### Service Manager (SM)
The Service Manager is responsible for keeping a registrar of all services and their locations.  The Service Manager is unique in that it is a static service which may be accessed anywhere in the cluster.

### State Server (SS)
The State Server handles all short-term object allocations.  The State Server is also responsible for culling interest for different Game Servers and Clients.

### Database Server (DB)
The Database handles long-term persistence of objects.  This server acts as a front to a database backend such as MongoDB.  When generating an object, a State Server will query the database to access or create an object.

### Game Server (GS)
The Game Server is the component responsible for the simulation of game worlds.  You, as the developer, may implement this component however you want -- there is no set method for implementing this.

## Server Messaging Protocol
Pantheon Server messages are grouped together inside a `NetStream` which is itself packed into a `Lidgren.Network.NetBuffer`.  

### Message Structure
A typical message will be formatted as such  

    Count(int32)
    Message[count]
      Length(uint16)
      ChannelCount(uint8)
        Channel[ChannelCount](uint64)
      From(uint64)
      Data(uint8[])

In almost all messages, the first four bytes of `Data` will be a value from the `MessageCode` enum.  

### Reserved Channels
All channels below 100 are reserved for library communication.  Reserved channels must always have a constant value because they are ineligible for Service Discovery.  Below are a list of these such channels.

    0-99 - System Channel
    0 - Null channel
    1 - Service Discovery
    2 - Service Query
    20 - ClientAgent Id Callback

    92,000 - 100,000 - Callback Channels.
    
    4,294,967,295 - 8,589,934,590 - Distributed Objects

You may check if a channel is reserved by invoking the `Pantheon.Core.Channel.IsChannelReserved(uint64)` method.  Channels above 9,223,372,036,854,775,807 are considered to be Client Channels and may be checked for using the `Pantheon.Core.Channel.IsGameChannel(uint64)` method.

### Special Message Codes

Certain message codes are designated as special.  These appear with the `ALL_` prefix in the `MessageCode` enum.  

Currently the only special message code is `ALL_QueryChannel` which instructs the service(s) located on the specified channel to broadcast their `ServiceName` fields on the `Message.From` channel.

## Client Messaging Protocol
The client messaging protocol is much more limited than the server protocol.  The primary reason for this is security.  It is important to remember that the client must **never** be allowed to communicate directly with the MessageDirector.  Client messages are grouped similar to server messages, allowing for more data to be sent per tick.

### Message Structure
A typical message is formatted as such:

    Count(int32)
    Message
      Length(uint16)
      MessageType(int32)
      Data(uint8[])

The exception is anything that derives from a `Pantheon.Client.Core.Packets.Datagram`, which uses the first 8 bytes of the `Data` field to specify a server-side channel for the containing data to be sent on.

The most significant difference between the client and server protocol is the fact that there is never a `From` field and there is a maximum of one destination channel.

##Extending the NetStream
Adding custom methods to the NetStream's automatic read-write system is simple.  
Step 1: define your custom data type.
```csharp
    public class MyDataType
    {
        public int X, Y, Z;
    }
```
Step 2: define a reader and writer.

Your reader-writer will need to break `MyDataType` down into its individual components and rebuild it on the other end.  This can be done by defining several extension methods in the same namespace as `MyDataType`.
```csharp
	public static class NetStreamExtensions
	{
	    public static void Write(this NetStream stream, MyDataType value)
	    {
	        stream.Write(value.X);
	        stream.Write(value.Y);
	        stream.Write(value.Z);
	    }
	
	    public static MyDataType ReadMyDataType(this NetStream stream)
	    {
	        MyDataType value = new MyDataType();
	        value.X = stream.ReadInt32();
	        value.Y = stream.ReadInt32();
	        value.Z = stream.ReadInt32();
	
	        return value;
	    }
	}
```
Congratulations!  You have extended the NetStream to support your custom type.  You may call your new extension methods like any normal instance methods.  However, the NetStream's `ReadObject(Type)` and `WriteObject(object)` methods will be unable to use your methods.

Step 3: register your custom readers and writers.

You will need to extend your `NetStreamExtensions` class so that it registers your reader-writer methods.  You can do this by using a static constructor.
```csharp
	public static class NetStreamExtensions
	{
	    static NetStreamExtensions()
	    {
	        NetStream.AddDataHandler(ReadMyDataType, Write);
			// OR NetStream.AddDataHandlers(typeof(NetStreamExtensions));
	    }
	    
	    // and so on...
	}
```
##Distributed Objects
Pantheon offers a powerful object framework that allows for objects to exist from several different perspectives.  Like services, which will be explained later, Distributed Objects offer an automatically implemented RPC service.  

### Defining Distributed Objects
Your Distributed Objects should be defined in a separate assembly just for your network-types.  This assembly will be shared between your client, server, and all Pantheon Server instances.  

To define a Distributed Object, reference `Pantheon.Common.dll` in your network project and create a new `interface` that derives from `Pantheon.Common.DistributedObjects.IDistributedObject` and define your networked properties and methods.  Here is an example DO Definition:

```csharp
	public interface IMyObject : IDistributedObject
    {
        [Db, Required, Broadcast, OwnerSend, AIReceive]
        float X { get; set; }

        [Db, Required, Broadcast, OwnerSend, AIReceive]
        float Y { get; set; }

        [Db, Required, Broadcast, OwnerSend, AIReceive]
        float Z { get; set; }

        [Db, Required, Broadcast]
        string Name { get; set; }

        [OwnerSend, AIReceive]
        void DoSomething();

        [ClientSend, AIReceive]
        string GetData();
    }
```

Here is a breakdown of the interface and what this all means.

> ### Storage Attributes in a Object definition
> **Db** - updates to this *property* will to be stored in the database.
> **Ram** - updates to this *property* will be stored on the state server.
> **Required** *implies ram* - this *property* is to be sent when the object generates.
> ### Security Attributes in a Object definition
> **OwnerSend** - updates to this *property* or *method* may be set by its owner.
> **ClientSend** - updates to this *property* or *method* may be set by any client.
> ### Culling Attributes in a Object Definition
> **OwnerReceive** - updates to this *property* or *method* will be received by the owner.
> **AIReceive** - updates to this *property* or *method* will be received by the `AIRepository` that possess it.
> **ClientReceive** - updates to this *property* or *method* will be broadcast to all clients listening to this object's updates.
> **Broadcast** *implies OwnerReceive, AIReceive, ClientReceive* - updates to this *property* or *method* will be broadcast on the object's channel.

Distributed Object inheritence forms a tree, so you can make a more derived interface such as `IDistributedLiving : IMyObject`.  `IDistributedLiving` could add things like health, attacking, healing, and more.  Unlike in regular reflection, Pantheon will find interface members that your Distributed Objects implement.

## Creating Services
Services operate on one of two systems: **distributed services** and **client-server services**.

### Distributed Service
A distributed service works by exposing one or several methods that may be called.  These services generally utilize only a single channel for all communications.  One example of a distributed service is the `ServiceManager`.

#### *The Service Manager as an example of Distributed Services*
The Service Manager handles all service registration and discovery.  At least one Service Manager on the shard must know how to access a service for discovery to be allowed.  When a Service Manager is asked to provide the channel of a given service, it first checks its local registrar.  If no match is found, it will broadcast a `ServiceDiscoveryMessage` on channel 1 or channel 3, depending on the level of information requested.  All other Service Managers will receive the message and check their local registrar.  If one owns the requested service, it will send a reply containing information; otherwise, no message will be sent.

The key is that **a Service Manager only replies if it owns the requested service**.  Assuming one instance of a service per shard, no more than one reply will be generated per request.

### Client-Server Services
*Note: these are not to me confused with services that allow Pantheon Clients to connect.*
A client-server service is a service that only lives in a single place.  These services all inherit from the `ServiceOwner` class.  Clients to these services inherit from the `ServiceClient` class.

####*Implementing a Client-Server Service*
In order to implement the server side of your new service, you should inherit your server class from the `ServiceOwner` class like so:  
```csharp
using System;
using Pantheon.Common;
using Pantheon.Core;

namespace MyServiceTest
{
    public class MyServiceManager : ServiceOwner
    {
        // Grouping message codes in constant values at the top of your class makes  your code more readable.
        const int MyServiceAction = 10000;
        const int MyServiceOther = 10001;

        public MyServiceManager(MessageRouter router, ulong channel)
            : base(router, "my_service", false, false, channel)
        {
            RegisterMessageRoute(MyServiceAction, HandleAction);
            RegisterMessageRoute(MyServiceOther, HandleOther);
        }
        
        private void HandleAction(Message message)
        {
            // Do something.
        }

        private void HandleOther(Message message)
        {
            Console.WriteLine(message.ReadString());

            Message reply = new Message();
            reply.Channels = new ulong[] { message.From };

			reply.Write("Written to console");
            // Message FROM field will be set by the SendMessage method.
            SendMessage(reply);
        }
    }
}
```
Now we need to implement a client service.
```csharp
	using System;
	using Pantheon.Common;
	using Pantheon.Core;
	
	namespace MyServiceTest
	{
	    public class MyServiceClient : ServiceClient
	    {
			public override string Name
			{
				get { return "my_service"; }
			}
			
			// Standard service constructor pattern for auto discovery.
	        public MyServiceClient(MessageRouter router)
	            : base(router, 0)
	        {
				// Extra initialization may be performed here.  For now, we will just auto-discover.
				DiscoverService();
	        }
	    }
	}
```
You could implement the service's sender methods yourself, or you can use Pantheon's built-in `ServiceProxy`.  You can even use the `ServiceProxy` on top of your existing `ServiceClient`.  Here is how to initialize a Proxy Service Client:
```csharp
	using Pantheon.Core.DistributedServices;
	
	public static MyServiceClient Create(MessageRouter router)
	{
		return ServiceProxy.Create<MyServiceClient>(router);
	}
```
This method, however, will fail if your client does not implement the standard service constructor pattern.  Here is how to implement your custom constructor logic into the Pantheon Service Proxy.
```csharp
	public static MyServiceClient Create(MessageRouter router, string name)
	{
		var client = new MyServiceClient(router, name);
		return ServiceProxy.Create<MyServiceClient(client);
	}
```
This system is useless if you don't make use of the Remote Method system.  This system intercepts any method calls to methods marked with a `RemoteMethodAttribute`.  Here is one of these methods in action:
```csharp
	[RemoteMethod(MyServiceAction)]
	public void SendMyServiceAction() { }
	
	[RemoteMethod(MyServiceOther)]
	public string SendMyServiceOther(string str) 
	{ 
		return "no reply"; // default value of the reply.
	}
```
When the `SendMyServiceAction` method is called using a `ServiceProxy`, code similar to the following will execute.
```csharp
	Message message = new Message()
	message.Channels = new ulong[] { ServiceChannel };
	message.From = Channel.GenerateCallback();

	message.Write(MyServiceAction);
	
	MessageDirector.QueueSend(message);
```
When `SendMyServiceOther` is called, something like this will run:
```csharp
	Message message = new Message()
	message.Channels = new ulong[] { ServiceChannel };
	message.From = Channel.GenerateCallback();

	message.Write(MyServiceOther);
	message.Write(str);
	
	Message reply = message.AwaitReply(MessageRouter);
	return reply.ReadString();
```
Lastly, if you have a custom data type or a reply header, you can define a response method as such:
```csharp
	private string MyCustomMessageResp(Message message)
	{
		message.ReadInt32();
		return message.ReadString();
	}
```
The `Resp` suffix is important.  The `ServiceProxy` will search for `MethodNameResp` in all cases.  So, if we wanted to implement a custom response handler for `SendMyServiceOther`, all we need do is create this method:
```csharp
	private string SendMyServiceOtherResp(Message message)
	{
		return message.ReadString();
	}
```
Alternatively, you may specify that you wish the system to skip the 32-bit message code as such: 
```csharp
	[RemoteMethod(ServiceCode, ReplySkipMessageCode = true)]
```
This will cause the system to advance the stream by 4 bytes when the message is received, allowing for the result to be read without error.

The final code for our service might look something like this:
```csharp
    using System;
    using Pantheon.Common;
    using Pantheon.Core;
	using Pantheon.Core.DistributedServices;
    
    namespace MyServiceTest
    {
        public class MyServiceManager : ServiceOwner
        {
            // Grouping message codes in constant values at the top of your class makes  your code more readable.
            const int MyServiceAction = 10000;
            const int MyServiceOther = 10001;

            public MyServiceManager(MessageRouter router, ulong channel)
                : base(router, "my_service", false, false, channel)
            {
                RegisterMessageRoute(MyServiceAction, HandleAction);
                RegisterMessageRoute(MyServiceOther, HandleOther);
            }
            
            private void HandleAction(Message message)
            {
                // Do something.
            }

            private void HandleOther(Message message)
            {
                Console.WriteLine(message.ReadString());

                Message reply = new Message();
                reply.Channels = new ulong[] { message.From };

				reply.Write("Written to console");
                // Message FROM field will be set by the SendMessage method.
                SendMessage(reply);
            }
        }

		public class MyServiceClient : ServiceClient
        {
			public override string Name
			{
				get { return "my_service"; }
			}
			
			// Standard service constructor pattern for auto discovery.
            public MyServiceClient(MessageRouter router)
                : base(router, 0)
            {
				// Extra initialization may be performed here.  For now, we will just auto-discover.  
				// We also should check if the service is currently connected (channel is not 0).  This is useful in the event 
				// you create another constructor that allows the user to set the channel or auto-discover.
				if(!IsConnected)
				{
					DiscoverService();
            	}
			}

			public static MyServiceClient Create(MessageRouter router)
			{
				return ServiceProxy.Create<MyServiceClient>(router);
			}

			[RemoteMethod(MyServiceAction)]
			public void SendMyServiceAction() { }
		
			[RemoteMethod(MyServiceOther)]
			public string SendMyServiceOther(string str) 
			{ 
				return "no reply"; // default value of the reply.
			}

			private string SendMyServiceOtherResp(Message message)
			{
				return message.ReadString();
			}
        }
    }
```
And there you have it!  This service can be used inside of your code to communicate between your custom components.