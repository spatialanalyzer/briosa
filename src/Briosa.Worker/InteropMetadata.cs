using System.Reflection;
using Briosa.SpatialAnalyzer.Interop;

namespace Briosa.Worker;

/// <summary>
/// Exposes non-executing metadata about the generated SDK interop boundary.
/// </summary>
public static class InteropMetadata
{
    public static AssemblyName AssemblyName => typeof(ISpatialAnalyzerSDK).Assembly.GetName();
}
