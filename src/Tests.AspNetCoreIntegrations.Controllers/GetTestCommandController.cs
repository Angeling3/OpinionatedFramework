using Controllers;
using IOKode.OpinionatedFramework.Tests.Commands;

namespace IOKode.OpinionatedFramework.Tests.AspNetCoreIntegrations.Controllers;

public partial class GetTestCommandController : ICommandController<TestCommand>
{
    public void Execute(TestCommand command)
    {
        
    }
}