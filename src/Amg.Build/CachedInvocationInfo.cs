using Amg.Build.Extensions;
using Amg.FileSystem;

namespace Amg.Build;

internal class CachedInvocationInfo : IInvocation
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    public CachedInvocationInfo(OnceInterceptor interceptor, InvocationId id, Castle.DynamicProxy.IInvocation invocation)
    {
        this.Id = id;

        var fileName = typeof(CachedInvocationInfo).GetProgramDataDirectory().Combine(Yaml.Md5Checksum(id) + ".yml");

        if (fileName.IsFile())
        {
            Logger.Debug("{task} uses cached result from {fileName}", this, fileName);
            next = null;
            begin = DateTime.UtcNow;
            var deserializer = (new YamlDotNet.Serialization.DeserializerBuilder()).Build();
            try
            {
                using (var r = new StreamReader(fileName))
                {
                    var taskResultType = InvocationInfo.TryGetTaskResultType(invocation.Method.ReturnType);
                    if (taskResultType is { })
                    {
                        var taskResult = deserializer.Deserialize(r, taskResultType);
                        returnValue = TaskExtensions.FromResult(taskResultType, taskResult);
                    }
                    else
                    {
                        returnValue = deserializer.Deserialize(r, invocation.Method.ReturnType);
                    }
                }
                end = DateTime.UtcNow;
                return;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Cached return value in {fileName} could not be read. Cache will be reset.", fileName);
                fileName.EnsureFileNotExists();
            }
        }

        next = new InvocationInfo(interceptor, id, invocation);
        if (next.ReturnValue is { })
        {
            var serializer = (new YamlDotNet.Serialization.SerializerBuilder()).Build();

            if (next.ReturnValue is Task task)
            {
                task.ContinueWith(_ =>
                {
                    if (_.TryGetResult(out var resultType, out var result))
                    {
                        using var writer = new StreamWriter(fileName.EnsureParentDirectoryExists());
                        serializer.Serialize(writer, result!, resultType);
                        Logger.Debug("{task} stored cached result at {fileName}", this, fileName);
                    }
                });
            }
            else
            {
                using (var writer = new StreamWriter(fileName.EnsureParentDirectoryExists()))
                {
                    serializer.Serialize(writer, next.ReturnValue);
                }
            }
        }
    }

    readonly object? returnValue = null;
    readonly DateTime? begin = null;
    readonly DateTime? end = null;
    readonly IInvocation? next;

    public InvocationId Id { get; }

    public InvocationState State { get; private set; }

    public DateTime? Begin => next is { } ? next.Begin : begin;
    public DateTime? End => next is { } ? next.End : end;

    public object? ReturnValue => next is { } ? next.ReturnValue : returnValue;

    public Exception? Exception => next?.Exception;

    public override string ToString() => this.Id.ToString();
}
