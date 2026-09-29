using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Briosa.Desktop;

public sealed record ActivityEntry(DateTimeOffset Time, string Level, string Category, string Message,
    string Correlation, string Operation, string Details);
