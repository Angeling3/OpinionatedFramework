using System;
using System.Collections.Generic;
using Controllers;
using IOKode.OpinionatedFramework.Tests.Commands;

namespace IOKode.OpinionatedFramework.Tests.AspNetCoreIntegrations.Controllers;


public class CommandControllerManifest : ICommandControllersManifest
{
    public IEnumerable<Type> GetCommandTypes()
    {
        return null;
    } 
}