using Amg.Extensions;
using Amg.FileSystem;
using YamlDotNet.Serialization;

namespace Amg.Build.Extensions;

public static class Yaml
{
    readonly static ISerializer serializer = new SerializerBuilder().Build();
    readonly static IDeserializer deserializer = new DeserializerBuilder().Build();

    public static string Md5Checksum(object graph)
    {
        return ToYaml(graph).Md5Checksum();
    }

    public static Task WriteFile(string file, object graph) => Task.Factory.StartNew(() =>
    {
        using (var writer = new StreamWriter(file.EnsureParentDirectoryExists()))
        {
            serializer.Serialize(writer, graph);
        }
    });

    public static string ToYaml(object graph) => serializer.Serialize(graph);

    public static Task<T> ReadFile<T>(string file) => Task.Factory.StartNew(() =>
    {
        using (var reader = new StreamReader(file))
        {
            return deserializer.Deserialize<T>(reader);
        }
    });

    public async static Task<T?> TryReadFile<T>(string file) where T : class
    {
        if (file.IsFile())
        {
            try
            {
                return await ReadFile<T>(file);
            }
            catch (Exception)
            {
                return null;
            }
        }
        return null;
    }
}
