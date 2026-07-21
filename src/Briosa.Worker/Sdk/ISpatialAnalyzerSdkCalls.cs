namespace Briosa.Worker.Sdk;

/// <summary>
/// Minimal synchronous call surface used to test the production MP sequence without COM activation.
/// </summary>
internal interface ISpatialAnalyzerSdkCalls : IDisposable
{
    bool ConnectEx(string host, ref int statusCode);

    void SetStep(string stepName);

    bool SetBoolArg(string name, bool value);

    bool SetIntegerArg(string name, int value);

    bool SetDoubleArg(string name, double value);

    bool SetStringArg(string name, string value);

    bool SetPointNameArg(
        string name,
        string collectionName,
        string groupName,
        string targetName);

    bool SetVectorArg(string name, double x, double y, double z);

    bool SetToleranceVectorOptionsArg(
        string name,
        bool useHighX,
        double highX,
        bool useHighY,
        double highY,
        bool useHighZ,
        double highZ,
        bool useHighMagnitude,
        double highMagnitude,
        bool useLowX,
        double lowX,
        bool useLowY,
        double lowY,
        bool useLowZ,
        double lowZ,
        bool useLowMagnitude,
        double lowMagnitude);

    bool ExecuteStep();

    bool GetMPStepResult(ref int resultCode);

    bool GetBoolArg(string name, ref bool value);

    bool GetIntegerArg(string name, ref int value);

    bool GetDoubleArg(string name, ref double value);

    bool GetStringArg(string name, ref string value);

    bool GetPointNameArg(
        string name,
        ref string collectionName,
        ref string groupName,
        ref string targetName);

    bool GetVectorArg(string name, ref double x, ref double y, ref double z);

    bool GetToleranceVectorOptionsArg(
        string name,
        ref bool useHighX,
        ref double highX,
        ref bool useHighY,
        ref double highY,
        ref bool useHighZ,
        ref double highZ,
        ref bool useHighMagnitude,
        ref double highMagnitude,
        ref bool useLowX,
        ref double lowX,
        ref bool useLowY,
        ref double lowY,
        ref bool useLowZ,
        ref double lowZ,
        ref bool useLowMagnitude,
        ref double lowMagnitude);
}
