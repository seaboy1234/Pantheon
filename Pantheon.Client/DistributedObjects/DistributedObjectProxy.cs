using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security.Permissions;
using System.Threading;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.Event;
using Pantheon.Common.IO;

namespace Pantheon.Client.DistributedObjects
{
    public class DistributedObjectProxy : RealProxy
    {
        private static uint _RpcId;

        private DObjectBaseClient _client;
        private Type _clientType;
        private DistributedObjectDefinition _obj;

        static DistributedObjectProxy()
        {
            _RpcId = 1;
        }

        public DistributedObjectProxy(DObjectBaseClient clientObject, DistributedObjectDefinition def)
            : base(clientObject.DistributedType)
        {
            _client = clientObject;
            _clientType = clientObject.DistributedType;
            _obj = def;
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage call = (IMethodCallMessage)msg;
            MethodInfo method = (MethodInfo)call.MethodBase;
            if (_RpcId >= uint.MaxValue - 100)
            {
                _RpcId = 1;
            }
            uint rpcId = _RpcId++;

            NetStream message = new NetStream();

            if (method.Name.StartsWith("set_"))
            {
                message.Write(ClientControlCode.Client_ObjectSetField);
                message.Write(rpcId);
                message.Write(_client.DoId);
                message.Write(_obj.GetPropertyId(method.Name.Substring(4)));
                message.WriteObject(call.InArgs.First());
            }
            else if (method.Name.StartsWith("get_"))
            {
                message.Write(ClientControlCode.Client_ObjectGetField);
                message.Write(rpcId);
                message.Write(_client.DoId);
                message.Write(_obj.GetPropertyId(method.Name.Substring(4)));
            }
            else
            {
                message.Write(ClientControlCode.Client_SendRPC);
                message.Write(rpcId);
                message.Write(_client.DoId);
                message.Write(_obj.GetMethodId(method.Name));

                int i = 0;
                foreach (var arg in call.InArgs)
                {
                    if (arg.GetType().Is(typeof(IDistributedObject)))
                    {
                        var doid = ((IDistributedObject)arg).DoId;
                        if (_client.Repository.GetObject(doid) == null)
                        {
                            throw new ArgumentException("no dobj with id " + doid, call.GetArgName(i));
                        }
                        message.Write(doid);
                    }
                    else
                    {
                        message.WriteObject(arg);
                    }
                    i++;
                }
            }

            if (method.ReturnType != typeof(void))
            {
                object reply = null;
                int waiting = 0;

                EventHandler<DataEventArgs> action = (sender, e) =>
                {
                    e.Data.Position = 0;
                    var packetId = e.Data.ReadPacketId();
                    if (packetId == ClientControlCode.Client_SendRPCResp)
                    {
                        uint rpcIdResp = e.Data.ReadUInt32();
                        if (rpcIdResp == rpcId)
                        {
                            if (method.ReturnType.Is(typeof(IDistributedObject)))
                            {
                                uint doid = e.Data.ReadUInt32();
                                reply = _client.Repository.GetObject(doid);
                            }
                            else
                            {
                                reply = e.Data.ReadObject(method.ReturnType);
                            }
                        }
                    }
                };

                _client.NetworkManager.OnDataReceived += action;
                _client.NetworkManager.WriteMessage(message);
                while (reply == null && waiting < 5000)
                {
                    Thread.Sleep(25);
                    waiting += 25;
                }
                _client.NetworkManager.OnDataReceived -= action;
                return new ReturnMessage(reply, null, 0, call.LogicalCallContext, call);
            }

            _client.NetworkManager.WriteMessage(message);

            return new ReturnMessage(null, null, 0, call.LogicalCallContext, call);
        }
    }
}