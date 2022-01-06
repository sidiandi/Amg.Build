namespace Amg.Build;

static class TaskExtensions
{
    public static bool TryGetResult(this Task task, out Type resultType, out object? result)
    {
        var taskType = task.GetType();
        if (taskType.IsGenericType && (!taskType.GenericTypeArguments[0].Name.Equals("VoidTaskResult")))
        {
            resultType = taskType.GenericTypeArguments[0];
            var resultProperty = taskType.GetProperty("Result");
            result = resultProperty.GetValue(task, new object[] { });
            return true;
        }

        resultType = null!;
        result = null;
        return false;
    }

    public static Task FromResult(Type type, object? result)
    {
        return (Task)typeof(Task)
            .GetMethod("FromResult")
            .MakeGenericMethod(type)
            .Invoke(null, new object[] { result! });
    }
}
