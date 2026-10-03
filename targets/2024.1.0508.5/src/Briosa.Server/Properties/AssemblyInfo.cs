using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Briosa.Server.Tests")]

// The opt-in licensed probe harness reuses the shipped command builders (#277).
[assembly: InternalsVisibleTo("Briosa.LicensedProbes")]
