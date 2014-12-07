using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Core;

namespace Pantheon.GameServer.DistributedObjects
{
    public class DistributedObjectProxy : RealProxy
    {
        private DistributedObjectBase _object;
        private Type _type;

        public DistributedObjectProxy(DistributedObjectBase obj)
            : base(obj.DistributedType)
        {
            _type = obj.DistributedType;
            _object = obj;
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage call = (IMethodCallMessage)msg;
            MethodInfo method = (MethodInfo)call.MethodBase;

            Message message = new Message();
            message.From = _object.ObjectChannel;
            if (method.Name.StartsWith("set_"))
            {
                message.Write(MessageCode.StateServer_ObjectSetField);
                message.Write(method.Name.Substring(4));
                if (call.InArgs.First().GetType().Is(typeof(IDistributedObject)))
                {
                    message.Write(((IDistributedObject)call.InArgs.First()).DoId);
                }
                else
                {
                    message.WriteObject(call.InArgs.First());
                }
                message.Write(0u);
                message.AddChannel(_object.ObjectChannel);
            }
            else if (method.Name.StartsWith("get_"))
            {
                message.Write(MessageCode.StateServer_ObjectGetField);
                message.Write(method.Name.Substring(4));
                message.AddChannel(_object.ObjectChannel);
            }
            else
            {
                message.Write(MessageCode.DObject_BroadcastRpc);
                message.Write(method.Name);
                foreach (var arg in call.InArgs)
                {
                    if (arg.GetType().Is(typeof(IDistributedObject)))
                    {
                        message.Write(((IDistributedObject)arg).DoId);
                    }
                    else
                    {
                        message.WriteObject(arg);
                    }
                }
                if (method.HasAttribute<BroadcastAttribute>())
                {
                    message.AddChannel(_object.ObjectChannel);
                }
                else
                {
                    if (method.HasAttribute<OwnerReceiveAttribute>())
                    {
                        message.AddChannel(_object.Owner);
                    }
                    if (method.HasAttribute<AIReceiveAttribute>() && _object.Repository.ServiceChannel != _object.Owner)
                    {
                        message.AddChannel(_object.AIChannel);
                    }
                    if (method.HasAttribute<ClientReceiveAttribute>())
                    {
                        message.AddChannel(_object.ClChannel);
                    }
                }
            }

            if (method.ReturnType != typeof(void))
            {
                MessageCode expected = MessageCode.DObject_BroadcastRpcResp;
                var reply = message.SendAsync(_object.MessageRouter, 2000, expected).Result;
                reply.Position += 4;
                object value;
                if (method.ReturnType.GetType().Is(typeof(IDistributedObject)))
                {
                    var doid = reply.ReadUInt32();
                    value = _object.Repository.GetObject(doid);
                }
                else
                {
                    value = reply.ReadObject(method.ReturnParameter.ParameterType);
                }
                return new ReturnMessage(value, null, 0, call.LogicalCallContext, call);
            }
            _object.MessageDirector.QueueSend(message);
            return new ReturnMessage(null, null, 0, call.LogicalCallContext, call);
        }
    }
}