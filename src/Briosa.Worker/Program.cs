using Briosa.Worker;

if (!Environment.Is64BitProcess)
{
    throw new PlatformNotSupportedException("The Briosa SDK worker requires a 64-bit process.");
}

Console.WriteLine($"Briosa worker scaffold using {InteropMetadata.AssemblyName.FullName}");
