using IOKode.OpinionatedFramework.Commands;

namespace Controllers;

public interface ICommandController<TCommand> where TCommand : Command
{
    public void Execute(TCommand command);
}