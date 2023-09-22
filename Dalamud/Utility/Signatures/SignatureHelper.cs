using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using Dalamud.Utility.Signatures.Wrappers;

namespace Dalamud.Utility.Signatures;

/// <summary>
/// A utility class to help reduce signature boilerplate code.
/// </summary>
public static class SignatureHelper
{
    private static readonly ModuleLog Log = new("SignatureHelper");
    
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Initialises an object's fields and properties that are annotated with a
    /// <see cref="SignatureAttribute"/>.
    /// </summary>
    /// <param name="self">The object to initialise.</param>
    /// <param name="log">If warnings should be logged using <see cref="PluginLog"/>.</param>
    public static void Initialise(object self, bool log = true)
    {
        var scanner = Service<SigScanner>.Get();
        var selfType = self.GetType();
        var fields = selfType.GetFields(Flags).Select(field => (IFieldOrPropertyInfo)new FieldInfoWrapper(field))
                             .Concat(selfType.GetProperties(Flags).Select(prop => new PropertyInfoWrapper(prop)))
                             .Select(field => (field, field.GetCustomAttribute<SignatureAttribute>()))
                             .Where(field => field.Item2 != null);

        foreach (var (info, sig) in fields)
        {
            var wasWrapped = false;
            var actualType = info.ActualType;
            if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // unwrap the nullable
                actualType = actualType.GetGenericArguments()[0];
                wasWrapped = true;
            }

            var fallibility = sig!.Fallibility;
            if (fallibility == Fallibility.Auto)
            {
                fallibility = info.IsNullable || wasWrapped
                                  ? Fallibility.Fallible
                                  : Fallibility.Infallible;
            }

            var fallible = fallibility == Fallibility.Fallible;

            void Invalid(string message, bool prepend = true)
            {
                var errorMsg = prepend
                                   ? $"Invalid Signature attribute for {selfType.FullName}.{info.Name}: {message}"
                                   : message;
                if (fallible)
                {
                    Log.Warning(errorMsg);
                }
                else
                {
                    throw new SignatureException(errorMsg);
                }
            }

            IntPtr ptr;
            var success = sig.ScanType == ScanType.Text
                              ? scanner.TryScanText(sig.Signature, out ptr)
                              : scanner.TryGetStaticAddressFromSig(sig.Signature, out ptr);
            if (!success)
            {
                if (log)
                {
                    Invalid($"Failed to find {sig.ScanType} signature \"{info.Name}\" for {selfType.FullName} ({sig.Signature})", false);
                }

                continue;
            }

            switch (sig.UseFlags)
            {
                case SignatureUseFlags.Auto when actualType == typeof(IntPtr) || actualType.IsPointer || actualType.IsAssignableTo(typeof(Delegate)):
                case SignatureUseFlags.Pointer:
                {
                    if (actualType.IsAssignableTo(typeof(Delegate)))
                    {
                        info.SetValue(self, Marshal.GetDelegateForFunctionPointer(ptr, actualType));
                    }
                    else
                    {
                        info.SetValue(self, ptr);
                    }

                    break;
                }

                case SignatureUseFlags.Auto when actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Hook<>):
                case SignatureUseFlags.Hook:
                {
                    if (!actualType.IsGenericType || actualType.GetGenericTypeDefinition() != typeof(Hook<>))
                    {
                        Invalid($"{actualType.Name} is not a Hook<T>");
                        continue;
                    }

                    var hookDelegateType = actualType.GenericTypeArguments[0];

                    Delegate? detour;
                    if (sig.DetourName == null)
                    {
                        var matches = selfType.GetMethods(Flags)
                                              .Select(method => method.IsStatic
                                                                    ? Delegate.CreateDelegate(hookDelegateType, method, false)
                                                                    : Delegate.CreateDelegate(hookDelegateType, self, method, false))
                                              .Where(del => del != null)
                                              .ToArray();
                        if (matches.Length != 1)
                        {
                            Invalid("Either found no matching detours or found more than one: specify a detour name");
                            continue;
                        }

                        detour = matches[0]!;
                    }
                    else
                    {
                        var method = selfType.GetMethod(sig.DetourName, Flags);
                        if (method == null)
                        {
                            Invalid($"Could not find detour \"{sig.DetourName}\"");
                            continue;
                        }

                        var del = method.IsStatic
                                      ? Delegate.CreateDelegate(hookDelegateType, method, false)
                                      : Delegate.CreateDelegate(hookDelegateType, self, method, false);
                        if (del == null)
                        {
                            Invalid($"Method {sig.DetourName} was not compatible with delegate {hookDelegateType.Name}");
                            continue;
                        }

                        detour = del;
                    }

                    var ctor = actualType.GetConstructor(new[] { typeof(IntPtr), hookDelegateType });
                    if (ctor == null)
                    {
                        Log.Error("Error in SignatureHelper: could not find Hook constructor");
                        continue;
                    }

                    var hook = ctor.Invoke(new object?[] { ptr, detour });
                    info.SetValue(self, hook);

                    break;
                }

                case SignatureUseFlags.Auto when actualType.IsPrimitive:
                case SignatureUseFlags.Offset:
                {
                    var offset = Marshal.PtrToStructure(ptr + sig.Offset, actualType);
                    info.SetValue(self, offset);

                    break;
                }

                default:
                {
                    if (log)
                    {
                        Invalid("could not detect desired signature use, set SignatureUseFlags manually");
                    }

                    break;
                }
            }
        }
    }
}
