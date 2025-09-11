using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Bindings.ImPlot;

using Xunit;


namespace Dalamud.Test.Compliance;

public class PublicApiTests
{
    private static List<Assembly> PermittedAssemblies { get; } =
    [
        typeof(object).Assembly,
        typeof(Dalamud).Assembly,

        typeof(SharpDX.Color).Assembly,
        typeof(ImGui).Assembly,
        typeof(ImGuizmo).Assembly,
        typeof(ImPlot).Assembly,

        // exposed to plugins via API
        typeof(Lumina.GameData).Assembly,
        typeof(Lumina.Excel.Sheets.Action).Assembly,
    ];

    private static List<Type> PermittedTypes { get; } = [
        typeof(Serilog.ILogger),
        typeof(Serilog.Core.LoggingLevelSwitch),
        typeof(Serilog.Events.LogEventLevel),
    ];

    [Fact]
    public void NoRestrictedTypes()
    {
        foreach (var type in typeof(Dalamud).Assembly.GetTypes().Where(t => t.IsPublic))
        {
            if (type.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

            foreach (var m in type.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName))
            {
                if (m.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                if (!this.IsPermittedType(m.ReturnType))
                {
                    Assert.Fail($"Method {type.FullName}.{m.Name} returns invalid type: {m.ReturnType.FullName}");
                }

                foreach (var param in m.GetParameters())
                {
                    if (!this.IsPermittedType(param.ParameterType))
                    {
                        Assert.Fail($"Method {type.FullName}.{m.Name} uses invalid type: {param.ParameterType.FullName}");
                    }
                }
            }

            foreach (var p in type.GetProperties())
            {
                if (p.GetCustomAttribute<ObsoleteAttribute>() != null) continue;
                if (p.GetMethod?.IsPrivate == true && p.SetMethod?.IsPrivate == true) continue;

                if (!this.IsPermittedType(p.PropertyType))
                {
                    Assert.Fail(
                        $"Property {type.FullName}.{p.Name} is invalid type: {p.PropertyType.FullName}");
                }
            }

            foreach (var f in type.GetFields().Where(f => f.IsPublic && !f.IsSpecialName))
            {
                if (f.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                if (!this.IsPermittedType(f.FieldType))
                {
                    Assert.Fail(
                        $"Field {type.FullName}.{f.Name} is invalid type: {f.FieldType.FullName}");
                }
            }
        }
    }

    private bool IsPermittedType(Type subject)
    {
        if (subject.IsGenericType && !subject.GetGenericArguments().All(this.IsPermittedType))
        {
            return false;
        }

        return subject.Namespace?.StartsWith("System") == true ||
            PermittedTypes.Any(t => t == subject) ||
            PermittedAssemblies.Any(a => a == subject.Assembly);
    }
}
