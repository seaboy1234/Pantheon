using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Core;
using Pantheon.Core.DistributedObjects;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;
using Pantheon.Core.StateServer;
using Pantheon.GameServer.DistributedObjects;

namespace Pantheon.GameServer
{
    public abstract class AIRepositoryBase : ServiceOwner, IAIRepository
    {
        private IObjectGenerator _generator;
        private List<DistributedObjectBase> _objects;
        private StateServerClient _stateServer;

        public IEnumerable<DistributedObjectBase> Objects
        {
            get { return _objects; }
        }

        public IObjectGenerator RootGenerator
        {
            get { return _generator; }
            set { _generator = value; }
        }

        public StateServerClient StateServer
        {
            get { return _stateServer; }
        }

        protected sealed override bool AutoRegister
        {
            get { return false; }
        }

        protected AIRepositoryBase(MessageRouter router, ulong channel)
            : base(router, "air", false, false, channel)
        {
            _stateServer = ServiceManager.GetServiceHandle<StateServerClient>(router);
            _generator = _stateServer;

            _objects = new List<DistributedObjectBase>();

            EventLog.SetLoggerName("AI");

            RegisterDisconnectMessages();
        }

        public void Destroy(uint doid)
        {
            Destroy(GetObject(doid));
        }

        public virtual void Destroy(DistributedObjectBase obj)
        {
            obj.Destroy();

            Message message = new Message();
            message.From = ServiceChannel;
            message.AddChannel(StateServer.ServiceChannel);
            message.Write(MessageCode.StateServer_Destroy);
            message.Write(obj.DoId);

            MessageDirector.QueueSend(message);

            Remove(obj);
        }

        public T Generate<T>(params object[] args)
                    where T : DistributedObjectBase
        {
            return (T)Generate(typeof(T), args);
        }

        public DistributedObjectBase Generate(Type type, params object[] args)
        {
            return GenerateWithOwner(type, ServiceChannel, args);
        }

        public async Task<T> GenerateAsync<T>(params object[] args)
            where T : DistributedObjectBase
        {
            return await Task.Run(() => Generate<T>(args));
        }

        public async Task<DistributedObjectBase> GenerateAsync(Type type, params object[] args)
        {
            return await Task.Run(() => Generate(type, args));
        }

        public T GenerateWithOwner<T>(ulong owner, params object[] args)
                            where T : DistributedObjectBase
        {
            return (T)GenerateWithOwner(typeof(T), owner, args);
        }

        public DistributedObjectBase GenerateWithOwner(Type type, ulong owner, params object[] args)
        {
            return GenerateWithParent(type, owner, RootGenerator, args);
        }

        public async Task<T> GenerateWithOwnerAsync<T>(ulong owner, params object[] args)
            where T : DistributedObjectBase
        {
            return await Task.Run(() => GenerateWithOwner<T>(owner, args));
        }

        public async Task<DistributedObjectBase> GenerateWithOwnerAsync(Type type, ulong owner, params object[] args)
        {
            return await Task.Run(() => GenerateWithOwner(type, owner, args));
        }

        public T GenerateWithParent<T>(ulong owner, IObjectGenerator generator, params object[] args)
            where T : DistributedObjectBase
        {
            return (T)GenerateWithParent(typeof(T), owner, generator, args);
        }

        public virtual DistributedObjectBase GenerateWithParent(Type type, ulong owner, IObjectGenerator generator,
                                                                                        params object[] args)
        {
            Type[] cotrArgs = new Type[args.Length + 2];
            object[] realArgs = new object[args.Length + 2];

            cotrArgs[0] = typeof(MessageRouter);
            cotrArgs[1] = typeof(ulong);

            realArgs[0] = MessageRouter;
            realArgs[1] = owner;

            for (int i = 2; i < cotrArgs.Length; i++)
            {
                cotrArgs[i] = args[i - 2].GetType();
                realArgs[i] = args[i - 2];
            }

            var cotr = type.GetConstructor(cotrArgs);
            var obj = (DistributedObjectBase)cotr.Invoke(realArgs);

            obj.Generate(this, generator);
            _objects.Add(obj);

            if (generator is DistributedObjectBase)
            {
                obj.Parent = (DistributedObjectBase)generator;
            }

            return obj;
        }

        public async Task<T> GenerateWithParentAsync<T>(ulong owner, IObjectGenerator parent, params object[] args)
            where T : DistributedObjectBase
        {
            return await Task.Run(() => GenerateWithParent<T>(owner, parent, args));
        }

        public async Task<DistributedObjectBase> GenerateWithParentAsync(Type type, ulong owner,
            IObjectGenerator parent, params object[] args)
        {
            return await Task.Run(() => GenerateWithParent(type, owner, parent, args));
        }

        public T GetObject<T>(uint doid)
            where T : DistributedObjectBase
        {
            return (T)GetObject(doid);
        }

        /// <summary>
        ///   Gets an object from this <see cref="AIRepositoryBase" /> .
        /// </summary>
        /// <param name="doid">The doid of the object to find.</param>
        /// <returns>
        ///   The object with the specified doid. If one does not exist, <c>null</c> .
        /// </returns>
        public virtual DistributedObjectBase GetObject(uint doid)
        {
            return _objects.Find(o => o.DoId == doid);
        }

        /// <summary>
        ///   Logs a message to the event logger.
        /// </summary>
        /// <param name="level">The <see cref="LogLevel" /> to use.</param>
        /// <param name="message">The message to log.</param>
        public virtual void Log(LogLevel level, string message)
        {
            EventLog.Log(level, message);
            LogFile.Log(level, message);
        }

        /// <summary>
        ///   Logs an Info message to the EventLogger.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Log(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        ///   Logs an object to the event logger with the following format: <br /> AI : Message,
        ///   Property1: Value, Property2: Value, etc. <br /> If the <c>Message</c> or
        ///              <c>LogLevel</c> properties are set, these will be treated as such.
        /// </summary>
        /// <param name="obj">The object containing properties to be logged.</param>
        /// <example>
        ///   This example will show the correct usage of this method:
        ///   <code>
        ///     Log(new { Property1 = "Value", LogLevel = LogLevel.Warning, Message = "Check this out!" });
        ///
        ///   </code>
        /// </example>
        public void Log(object obj)
        {
            if (obj is IDictionary<string, object>)
            {
                LogDynamic(obj);
                return;
            }
            if (obj is string)
            {
                Log(LogLevel.Info, (string)obj);
                return;
            }
            var properties = obj.GetType().GetProperties();
            var fields = obj.GetType().GetFields();

            StringBuilder message = new StringBuilder();
            var msg = properties.Where(p => string.Equals(p.Name,
                                                "message",
                                               StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()?.GetValue(obj) ??
                fields.FirstOrDefault(g => string.Equals(g.Name,
                                                        "message",
                                                        StringComparison.InvariantCultureIgnoreCase))?.GetValue(obj);
            LogLevel? logLevel = (LogLevel?)properties.Where(p => string.Equals(p.Name,
                                            "loglevel",
                                            StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()?.GetValue(obj) ??
                        (LogLevel?)fields.FirstOrDefault(g => string.Equals(g.Name,
                                                        "loglevel",
                                                        StringComparison.InvariantCultureIgnoreCase))?.GetValue(obj);

            if (msg != null)
            {
                message.Append(", ");
                message.Append(msg);
            }

            foreach (var prop in properties)
            {
                if (string.Equals(prop.Name, "message", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(prop.Name, "loglevel", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                else
                {
                    message.Append(", ");
                    message.Append(prop.Name);
                    message.Append(": ");

                    if (prop.PropertyType == typeof(ExpandoObject))
                    {
                        message.Append(ExpandoToString(prop.GetValue(obj)));
                    }
                    else
                    {
                        message.Append(prop.GetValue(obj).ToString());
                    }
                }
            }
            if (message[0] == ',')
            {
                message = message.Remove(0, 2);
            }
            Log(logLevel ?? LogLevel.Info, message.ToString());
        }

        /// <summary>
        ///   Logs a dynamic object to the EventLogger.
        /// </summary>
        /// <param name="obj">The object to log.</param>
        public void LogDynamic(dynamic obj)
        {
            var values = (IDictionary<string, object>)obj;

            StringBuilder log = new StringBuilder();

            LogLevel? logLevel = null;

            if (values.ContainsKey("message"))
            {
                log.Append(", " + obj.message);
                values.Remove("message");
            }
            if (values.ContainsKey("logLevel"))
            {
                logLevel = obj.logLevel;
                values.Remove("loglevel");
            }

            foreach (var value in values)
            {
                if (value.Value is ExpandoObject)
                {
                    log.AppendFormat(", {0}: {1}", value.Key, ExpandoToString(value.Value));
                }
                else
                {
                    log.AppendFormat(", {0}: {1}", value.Key, value.Value);
                }
            }

            if (log[0] == ',')
            {
                log.Remove(0, 2);
            }

            Log(logLevel ?? LogLevel.Info, log.ToString());
        }

        internal void Remove(DistributedObjectBase obj)
        {
            _objects.Remove(obj);
            foreach (var dobj in _objects.ToArray().Where(g => g.Parent == obj))
            {
                dobj.Destroy();
            }
            obj.Dispose();
        }

        protected virtual void RegisterDisconnectMessages()
        {
            var messages = StateServer.OnDisconnect<StateServerClient>(s =>
            {
                s.ZoneServerExited(ServiceChannel);
            });

            foreach (var message in messages)
            {
                SendOnDisconnect(message);
            }
        }

        /// <summary>
        ///   Queues a message to be sent when this process exits.
        /// </summary>
        /// <param name="message">The message to queue.</param>
        protected void SendOnDisconnect(Message message)
        {
            MessageDirector.QueueOnDisconnect(message);
        }

        private string ExpandoToString(dynamic obj)
        {
            var values = (IDictionary<string, object>)obj;

            StringBuilder log = new StringBuilder();
            log.Append("{ ");
            foreach (var value in values)
            {
                if (value.Value is ExpandoObject)
                {
                    log.Append(ExpandoToString(value.Value));
                }
                else
                {
                    log.AppendFormat(", {0}: {1}", value.Key, value.Value);
                }
            }

            log.Append(" }");
            log.Remove(2, 2);

            return log.ToString();
        }
    }
}