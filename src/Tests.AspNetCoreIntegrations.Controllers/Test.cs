using System;
using System.Collections.Generic;

namespace IOKode.OpinionatedFramework.Tests.AspNetCoreIntegrations.Controllers;

public class Test
{
    void main()
    {
        ControllerGeneratorConfig.CommandResolutor = delegate ()
        {
            return new[] {Type.GetType("")};
        };
        
        ControllerGeneratorConfig.HttpMethodResolutor = type =>
        {
            return "GET";
        };
    }
}

static class ControllerGeneratorConfig
{
    public static Func<IEnumerable<Type>> CommandResolutor;
    public static Func<Type, string> HttpMethodResolutor;
}