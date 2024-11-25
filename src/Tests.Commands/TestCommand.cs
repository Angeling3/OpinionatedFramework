using System.Threading.Tasks;
using Controllers;
using IOKode.OpinionatedFramework.Commands;

namespace IOKode.OpinionatedFramework.Tests.Commands;

public class TestCommand : Command
{
    protected override Task ExecuteAsync(ICommandContext context)
    {
        return Task.CompletedTask;
    }

    public void GetRoute()
    {
        throw new System.NotImplementedException();
    }
}