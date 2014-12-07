Pantheon Distributed Object Framework
---
The Pantheon Distributed Server Framework offers an object framework that allows simple management of so called "Distributed Objects."  This special class of object is stored on a state server where it could be transferred to another game server, if you so please.

The framework can be accessed by accessing the appropriate `Pantheon.XYZ.DistributedObjects` namespace.  `Pantheon.Common`, `Pantheon.Client`, and `Pantheon.GameServer` all offer Distributed Object helper types.

### Using the Framework

To use the Distributed Object Framework, you will need to make a model.  A model is an interface which derives from `Pantheon.Common.DistributedObjects.IDistributedObject`.  Your model may define zero or more properties and methods.  All of these will be visible on the network, so only define what is needed to be **stored** or **sent**.

The next step is to define some behaviors for how an object's members will be treated on the network.  To assist with this, Pantheon offers several attributes you may add to your methods.

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
> **Broadcast** *implies OwnerReceive, AIReceive, ClientReceive* - updates to this *property* or *method* will be broadcast on the object's global channel.

So, if you wanted a *method* to be callable by clients and received by everything, you would define your method like this:

```csharp
	[ClientSend, Broadcast]
	void DoSomething(int someCode);
``` 

If you wanted a *property* to be stored in the database, updated by the owner, updates to be sent to the owner and the AI, and for it to be persisted on the State Server, you would define it like this:

```csharp
	[Db, Ram, OwnerSend, OwnerReceive, AIReceive]
	int SomeProperty { get; set; }
```

Note that if the owner was an AI Server, that we have a redundant attribute.  Pantheon will detect this and ensure that an update is correctly sent.