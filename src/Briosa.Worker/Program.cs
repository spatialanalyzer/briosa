using Briosa.Worker;

if (!Environment.Is64BitProcess)
{
    throw new PlatformNotSupportedException("The Briosa SDK worker requires a 64-bit process.");
}

if (TryGetArgument(args, "--control-pipe", out var pipeName))
{
    var parentProcessId = TryGetArgument(args, "--parent-process-id", out var parentValue) &&
        int.TryParse(parentValue, out var parsedParentProcessId)
            ? parsedParentProcessId
            : (int?)null;
    return WorkerControlHost.Run(pipeName, parentProcessId);
}

Console.WriteLine($"Briosa worker scaffold using {InteropMetadata.AssemblyName.FullName}");
return 0;

static bool TryGetArgument(string[] arguments, string name, out string value)
{
    var index = Array.IndexOf(arguments, name);
    if (index >= 0 && index + 1 < arguments.Length)
    {
        value = arguments[index + 1];
        return true;
    }

    value = string.Empty;
    return false;
}
