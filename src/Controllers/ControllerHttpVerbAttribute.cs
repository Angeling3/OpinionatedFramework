namespace Controllers;

public static class HttpVerb
{
    public const string Get = "GET";
    public const string Post = "POST";
    public const string Put = "PUT";
    public const string Delete = "DELETE";
    public const string Head = "HEAD";
    public const string Options = "OPTIONS";
    public const string Patch = "PATCH";
    public const string Trace = "TRACE";
}

[AttributeUsage(AttributeTargets.Class)]
public class ControllerHttpVerbAttribute(string verb) : Attribute
{
    public string Verb { get; } = verb;
}