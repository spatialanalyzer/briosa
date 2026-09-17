namespace Briosa.Worker.Sdk;

/// <summary>
/// Minimal synchronous call surface used to test the production MP sequence without COM activation.
/// </summary>
internal partial interface ISpatialAnalyzerSdkCalls : IDisposable
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

    bool SetChartNameArg(string name, string chartName);

    bool SetCloudNameArg(string name, string cloudName);

    bool SetColInstIdArg(string name, string collectionName, int instrumentId);

    bool SetColInstIdRefListArg(string name, ref object values);

    bool SetColMachineIdArg(string name, string collectionName, int machineId);

    bool SetCollectionGroupNameRefListArg(string name, ref object values);

    bool SetCollectionNameArg(string name, string collectionName);

    bool SetCollectionObjectNameArg2(
        string name,
        string collectionName,
        string objectName,
        string objectType);

    bool SetCollectionObjectNameRefListArg(string name, ref object values);

    bool SetCollectionVectorGroupNameRefListArg(string name, ref object values);

    bool SetColVectorGroupNameArg(
        string name,
        string collectionName,
        string vectorGroupName);

    bool SetFrameNameArg(string name, string frameName);

    bool SetPointNameRefListArg(string name, ref object values);

    bool SetStringRefListArg(string name, ref object values);

    bool SetVectorGroupNameArg(string name, string vectorGroupName);

    bool SetVectorNameRefListArg(string name, ref object values);

    bool SetViewNameArg(string name, string viewName);
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

    bool SetDoubleArrayArg(string name, int arraySize, ref object values);

    bool SetEditTextArg(string name, ref object values);

    bool SetTransformArg(string name, ref object transform);

    bool SetWorldTransformArg(string name, ref object transform, double scaleFactor);

    bool SetColorArg(string name, byte red, byte green, byte blue);

    bool SetFilePathArg(string name, string path, bool embeddedFile);

    bool SetAngularUnitsArg(string name, string angularUnits);

    bool SetDistanceUnitsArg(string name, string distanceUnits);

    bool SetTemperatureUnitsArg(string name, string temperatureUnits);

    bool SetFontTypeArg(
        string name,
        string fontName,
        byte fontSize,
        byte red,
        byte green,
        byte blue);

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

    bool GetColInstIdArg(
        string name,
        ref string collectionName,
        ref int instrumentId);

    bool GetColInstIdRefListArg(string name, ref object values);

    bool GetCollectionNameArg(string name, ref string collectionName);

    bool GetCollectionObjectNameArg(
        string name,
        ref string collectionName,
        ref string objectName);

    bool GetCollectionObjectNameRefListArg(string name, ref object values);

    bool GetPointNameRefListArg(string name, ref object values);

    bool GetStringRefListArg(string name, ref object values);

    bool GetVectorNameRefListArg(string name, ref object values);
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

    bool GetDoubleArrayArg(string name, ref int arraySize, ref object values);

    bool GetEditTextArg(string name, ref object values);

    bool GetTransformArg(string name, ref object transform);

    bool GetWorldTransformArg(string name, ref object transform, ref double scaleFactor);

    bool GetFilePathArg(string name, ref string path, ref bool embeddedFile);
}
