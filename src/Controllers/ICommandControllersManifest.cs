namespace Controllers;

public interface ICommandControllersManifest
{
    public IEnumerable<Type> GetCommandTypes();
}