using System;

// ReSharper disable once CheckNamespace
namespace Dalamud;

// TODO: Get rid of this! Move StartInfo to another assembly, make this good

/// <summary>
/// Class to initialize Service&lt;T&gt;s.
/// </summary>
internal static class ServiceManager
{
    /// <summary>
    /// Indicates that the class is a service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Service : Attribute
    {
    }
}
