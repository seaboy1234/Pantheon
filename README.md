Pantheon
========

*Pantheon Distributed Server*

## Project Structure
The project generally follows a tree with one branch being the server and the other being the client side of the Pantheon project.

```
                                        Pantheon.Common
                                               |-----------\
                                               |            \
                                              / \            \  Extension Libraries
                                             /   \            \
                                            /     \            \------------------
                                           /       \            \            \
                                          /         \            \            \
                                         /           \     Pantheon.Lidgren  Game Specific Assembies
                                        /             \                                            |
                                       /               \                                           |
                                      /                 \                                          |
         /-------------------> Pantheon.Core       Pantheon.Client.Core                            |
       /                            /                     /       \                                |
      | Pantheon Server Software   /                     /         \    Pantheon Client Software   |
      |                           /           Optional  /           \                             /|
      |                  Pantheon.Server     Client.Extensions <--- Custom Pantheon Client  <----/ |
      |                         /     \                                                            |
      |                        /       \---------------------\                                     |
      |                        | Server.ServiceLoader         \                                    |
      |                  Loads |----------------------\   Server.Config                           /
      |                        |                      |         \                                /
      |                 Pantheon.CoreServices         |          \ Xml Default                  /
      |                        |                      |           \                            /
      |                        |                      |     Other Config Loaders              /
      |                        |                      \                                      /
      |                        V  Loads               GameServer->--------------------------/            
       \--------------------------------------------\               
        |                      |                    |                
Pantheon.StateServer   Pantheon.ClientAgent   Pantheon.Database                                                               
```

## Goals

The primary goal of the development of Pantheon is to have as few dependencies as possible.  This does have the side-effect of sometimes re-inventing the wheel, but for the most part, this allows the components to better gel.  Compatibility layers to third-party backends are included (e.g. Pantheon.Lidgren) in separate extension libraries.  This allows for these third-party components to be used in place of included components.

## Contributing

It is recommended that you familerize yourself with the general coding conventions before you start writing code.  Afterwards, you shoud submit changes on a development branch specific to the feature being implemented.

## Creating Extension Libraries

Create a class that inherits from `PantheonInitializer`, which is in the `Pantheon.Common` project and implement the `Initialize()` method.  This method is called when the Pantheon Server or a Pantheon Client is started.  Generally this method is used to register `NetStream` extensions or to register new Network Managers. 