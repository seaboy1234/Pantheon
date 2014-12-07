Pantheon Server Configuration
---

Pantheon's default server offers a robust and complete configuration system that allows you to configure almost any aspect of your Pantheon shard.  When you run the Pantheon.Server executable for the first time, it will generate a new `Pantheon.xml` config file.  Inside of this config file is the default settings for operating a single-server Pantheon system.

### Configuring your Pantheon Server

If you have not already, please take some time to start your Pantheon Server.  You can do this by running `Pantheon.Server.exe`.  This will cause the server to generate the default config file for Pantheon.

Your default config file should have a number of default services defined, including a Message Director.

```xml
<MessageDirector>
	<IsLocal>true</IsLocal>
	<Address>127.0.0.1</Address>
	<Port>9090</Port>
</MessageDirector>
```

The `MessageDirector` directive defines how the local Message Director will behave.  If `IsLocal` is set to true, the current Pantheon process will act as a Message Director server; otherwise, the process will act as a Message Director client.  `Address` is used when a Pantheon process is set to act as a Message Director client; it defines the remote address to connect to.  `Address` may be an IP Address or a hostname (e.g. `messagedirector.server.mygame.com`).  `Port` defines the port to bind or connect to.

The next directive is the `Services` element.

```xml
<Services>
...
</Services>
``` 

In this element, you will define zero or more services.  Here is an example of a service directive:

```xml
<Services>
	<Service Type="MyService">
		<Channel>1234</Channel>
	</Service>
</Services>
```

In this service directive, there is one service defined, `MyService`, with two settings -- `Channel` and `EnableSuperFunStuff`.  The `Type` attribute defines the service's type.  This allows Pantheon to find the correct service type and construct it.  `Channel` is often used to define a control channel for a service.  All default Pantheon services require a Channel directive and it is a good idea for your service to also require one.  This allows a much more modular cluster where your users can select which channel a service operates on.

A list of default services and their special directives is outlined below.

### Defining a Custom Service

Pantheon Server would be useless if it did not allow for server programmers to define custom services.  So, a simple way to hook into Pantheon's server is included with the server software.

 