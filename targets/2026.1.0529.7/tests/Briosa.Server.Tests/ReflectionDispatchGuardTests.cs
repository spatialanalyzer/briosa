using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

namespace Briosa.Server.Tests;

// Inspect compiled calls so aliases, fully qualified names, and async state machines
// cannot hide ordinary reflection or dynamic dispatch from a text search.
public sealed class ReflectionDispatchGuardTests
{
    private static readonly Dictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => unchecked((ushort)opcode.Value));

    [Fact]
    public void HandwrittenRuntimeDoesNotUseReflectionOrDynamicDispatch()
    {
        var workerPath = Path.Combine(AppContext.BaseDirectory, "worker-under-test", "Briosa.Worker.dll");
        var assemblies = new[] { typeof(Program).Assembly, Assembly.LoadFrom(workerPath) };
        var violations = assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace?.StartsWith("Briosa.Server", StringComparison.Ordinal) == true ||
                type.Namespace?.StartsWith("Briosa.Worker", StringComparison.Ordinal) == true)
            .Where(type => !type.Namespace!.Contains(".Diagnostics", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .SelectMany(method => ForbiddenCalls(method).Select(call =>
                    $"{type.FullName}.{method.Name} -> {call}")))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void GuardRejectsReflectionAndAcceptsJsonAndDelegates()
    {
        Assert.NotEmpty(ForbiddenCalls(typeof(ReflectionDispatchGuardTests).GetMethod(
            nameof(ForbiddenSample), BindingFlags.NonPublic | BindingFlags.Static)!));
        Assert.Empty(ForbiddenCalls(typeof(ReflectionDispatchGuardTests).GetMethod(
            nameof(AllowedSample), BindingFlags.NonPublic | BindingFlags.Static)!));
    }

    private static void ForbiddenSample() => _ = typeof(string).GetMethod("ToString");

    private static void AllowedSample(JsonElement element, Action action)
    {
        _ = element.GetProperty("name");
        action();
    }

    private static IEnumerable<string> ForbiddenCalls(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null) yield break;
        for (var offset = 0; offset < il.Length;)
        {
            var code = (ushort)il[offset++];
            if (code == 0xfe) code = (ushort)((code << 8) | il[offset++]);
            var opcode = OpCodesByValue[code];
            if (opcode.OperandType == OperandType.InlineMethod)
            {
                var token = BitConverter.ToInt32(il, offset);
                var called = method.Module.ResolveMethod(token,
                    method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments());
                if (called is not null && IsForbidden(called))
                    yield return $"{called.DeclaringType?.FullName}.{called.Name}";
            }
            offset += OperandBytes(opcode.OperandType, il, offset);
        }
    }

    private static bool IsForbidden(MethodBase method)
    {
        var owner = method.DeclaringType;
        if (owner is null) return false;
        var name = owner.FullName ?? string.Empty;
        return (owner == typeof(Type) && method.Name.StartsWith("Get", StringComparison.Ordinal) &&
                    method.Name is "GetMethod" or "GetMethods" or "GetProperty" or "GetProperties" or
                        "GetField" or "GetFields" or "GetMember" or "GetMembers" or "GetType") ||
            owner == typeof(Activator) ||
            (typeof(MethodBase).IsAssignableFrom(owner) && method.Name == "Invoke") ||
            (typeof(MemberInfo).IsAssignableFrom(owner) && method.Name == "GetValue") ||
            name.StartsWith("Microsoft.CSharp.RuntimeBinder.Binder", StringComparison.Ordinal) ||
            (name.StartsWith("System.Runtime.CompilerServices.CallSite", StringComparison.Ordinal) &&
                method.Name == "Create") ||
            (owner == typeof(Delegate) && method.Name == "DynamicInvoke");
    }

    private static int OperandBytes(OperandType kind, byte[] il, int offset) => kind switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or
            OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or
            OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
        _ => throw new InvalidOperationException($"Unsupported IL operand: {kind}")
    };
}
