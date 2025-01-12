using System;
using System.Collections.Generic;
using System.Linq;

namespace MyExpr;

public interface IMyEnvironment
{
    object Get(string name);
}

public class ClosureEnvironment(object target) : IMyEnvironment
{
    private readonly Dictionary<string, Func<object>> _environment =
        target.GetType().GetFields().Select(f => KeyValuePair.Create(f.Name, () => f.GetValue(target))).ToDictionary();

    public object Get(string name) =>
        _environment.TryGetValue(name, out var value)
        ? value()
        : throw new KeyNotFoundException();
}

public class BindEnvironment(string key, object value, IMyEnvironment fallback) : IMyEnvironment
{
    public object Get(string name) =>
        name == key
        ? value
        : fallback.Get(name);
}

public static class MyEnvironment
{
    public static IMyEnvironment FromDelegate(Delegate d) =>
        new ClosureEnvironment(d?.Target ?? throw new ArgumentException());

    public static IMyEnvironment FromAnything(object d) =>
        FromDelegate(d as Delegate ?? throw new ArgumentException());

    public static IMyEnvironment Bind(this IMyEnvironment environment, string key, object value) =>
        new BindEnvironment(key, value, environment);
}
