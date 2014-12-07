using System;
using System.IO;
using Pantheon.Common.IO;

namespace Pantheon.Database.Backends
{
    public class FileSystemBackend : DatabaseBackend
    {
        private string _path;

        public override string Name
        {
            get { return "Default"; }
        }

        public FileSystemBackend(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            _path = path;
        }

        public override SerializedObject GetObject(int id)
        {
            if (!File.Exists(GetFileName(id)))
            {
                return null;
            }
            NetStream stream = new NetStream(File.ReadAllBytes(GetFileName(id)));
            Type type = stream.ReadType();
            return new SerializedObject(type, stream);
        }

        public override object GetProperty(int id, string name)
        {
            SerializedObject obj = GetObject(id);
            return obj[name];
        }

        public override void SetObject(int id, SerializedObject obj)
        {
            NetStream stream = new NetStream();
            stream.Write(obj.Type);
            obj.WriteTo(stream);

            if (File.Exists(GetFileName(id)))
            {
                File.Delete(GetFileName(id));
            }
            File.WriteAllBytes(GetFileName(id), stream.Data);
        }

        private string GetFileName(int id)
        {
            return Path.Combine(_path, id.ToString());
        }
    }
}