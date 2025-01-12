namespace System.Runtime.CompilerServices;

#pragma warning disable CS9113 // Parameter is unread.

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptsLocationAttribute(int version, string data) : Attribute
{
}
