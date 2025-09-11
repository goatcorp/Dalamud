using System;
using System.Linq;
using System.Reflection;

using Dalamud.Utility;

using Xunit;


namespace Dalamud.Test.Compliance;

public class PublicApiTests
{
    [Fact]
    public void NoClientStructsTypes()
    {
        var clientStructsAssembly = typeof(FFXIVClientStructs.ThisAssembly).Assembly;

        var publicTypes = typeof(Dalamud).Assembly.GetTypes().Where(t => t.IsPublic);

        foreach (var t in publicTypes)
        {
            if (t.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

            foreach (var m in t.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName))
            {
                if (m.GetCustomAttribute<ObsoleteAttribute>() != null ||
                    m.GetCustomAttribute<Api14ToDoAttribute>() != null) continue;

                if (m.ReturnType.Assembly == clientStructsAssembly)
                {
                    Assert.Fail(
                        $"Method {t.FullName}.{m.Name} returns a type from FFXIVClientStructs: {m.ReturnType.FullName}");
                }

                foreach (var param in m.GetParameters())
                {
                    if (param.ParameterType.Assembly == clientStructsAssembly)
                    {
                        Assert.Fail(
                            $"Method {t.FullName}.{m.Name} has a parameter from FFXIVClientStructs: {param.ParameterType.FullName}");
                    }
                }
            }

            foreach (var p in t.GetProperties())
            {
                if (p.GetCustomAttribute<ObsoleteAttribute>() != null ||
                    p.GetCustomAttribute<Api14ToDoAttribute>() != null) continue;
                if (p.GetMethod?.IsPrivate == true && p.SetMethod?.IsPrivate == true) continue;

                if (p.PropertyType.Assembly == clientStructsAssembly)
                {
                    Assert.Fail(
                        $"Property {t.FullName}.{p.Name} is a type from FFXIVClientStructs: {p.PropertyType.FullName}");
                }
            }

            foreach (var f in t.GetFields().Where(f => f.IsPublic && !f.IsSpecialName))
            {
                if (f.GetCustomAttribute<ObsoleteAttribute>() != null ||
                    f.GetCustomAttribute<Api14ToDoAttribute>() != null) continue;

                if (f.FieldType.Assembly == clientStructsAssembly)
                {
                    Assert.Fail(
                        $"Field {t.FullName}.{f.Name} is of a type from FFXIVClientStructs: {f.FieldType.FullName}");
                }
            }
        }
    }
}
