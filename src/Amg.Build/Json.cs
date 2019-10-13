using Amg.Build.Extensions;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Amg.Build
{
    internal static class Json
    {
        public static Task<T> Read<T>(string path) => Task.Factory.StartNew(() =>
        {
            // deserialize JSON directly from a file
            using (var file = new StreamReader(path))
            {
                var serializer = new JsonSerializer();
                var reader = new JsonTextReader(file);
                return serializer.Deserialize<T>(reader);
            }
        });

        public static Task Write<T>(string path, T data) => Task.Factory.StartNew(() =>
        {
            // serialize JSON directly to a file
            using (var file = new StreamWriter(path))
            {
                var serializer = new JsonSerializer();
                var writer = new JsonTextWriter(file);
                serializer.Serialize(writer, data);
            }
        });

        internal static string Hash<T>(T data)
        {
            using (var text = new StringWriter())
            {
                var serializer = new JsonSerializer();
                var writer = new JsonTextWriter(text);
                serializer.Serialize(writer, data);
                return text.ToString().Md5Checksum();
            }
        }
    }
}
