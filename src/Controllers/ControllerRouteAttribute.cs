namespace Controllers;

[AttributeUsage(AttributeTargets.Class)]
public class ControllerRouteAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}