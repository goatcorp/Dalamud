using System;
using System.Linq;
using System.Reflection;

using Dalamud.Utility;

using Xunit;
using Xunit.Abstractions;


namespace Dalamud.Test.Compliance;

public class PublicApiTests
{
    private readonly ITestOutputHelper testOutputHelper;
    public PublicApiTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void NoClientStructsTypes()
    {
        var clientStructsAssembly = typeof(FFXIVClientStructs.ThisAssembly).Assembly;

        var publicTypes = typeof(Dalamud).Assembly.GetTypes().Where(t => t.IsPublic);

        foreach (var t in publicTypes)
        {
            if (t.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

            var methods = t.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName);

            foreach (var m in methods)
            {
                if (m.GetCustomAttribute<ObsoleteAttribute>() != null || m.GetCustomAttribute<Api14ToDoAttribute>() != null) continue;
                if (m.IsPrivate) continue;

                if (m.ReturnType.Assembly == clientStructsAssembly)
                {
                    Assert.Fail($"Method {t.FullName}.{m.Name} returns a type from FFXIVClientStructs: {m.ReturnType.FullName}");
                }

                foreach (var param in m.GetParameters())
                {
                    if (param.ParameterType.Assembly == clientStructsAssembly)
                    {
                        Assert.Fail($"Method {t.FullName}.{m.Name} has a parameter from FFXIVClientStructs: {param.ParameterType.FullName}");
                    }
                }
            }

            foreach (var p in t.GetProperties())
            {
                if (p.GetCustomAttribute<ObsoleteAttribute>() != null || p.GetCustomAttribute<Api14ToDoAttribute>() != null) continue;
                if (p.GetMethod?.IsPrivate == true && p.SetMethod?.IsPrivate == true) continue;

                if (p.PropertyType.Assembly == clientStructsAssembly)
                {
                    Assert.Fail($"Property {t.FullName}.{p.Name} is a type from FFXIVClientStructs: {p.PropertyType.FullName}");
                }
            }

            foreach (var field in t.GetFields())
            {
                if (field.GetCustomAttribute<ObsoleteAttribute>() != null || field.GetCustomAttribute<Api14ToDoAttribute>() != null) continue;
                if (field.IsPrivate) continue;

                if (field.FieldType.Assembly == clientStructsAssembly)
                {
                    Assert.Fail($"Field {t.FullName}.{field.Name} is of a type from FFXIVClientStructs: {field.FieldType.FullName}");
                }
            }
        }
    }
}
