# Reporting-frame getter validation

`GetObjectReportingFrame` and `GetRelationshipReportingFrame` have reviewed MP steps and SDK argument bindings. Their typed mappings do not assume an object type when the SDK omits it. A real fixture is still needed to establish whether this SA release returns a `Frame` type literal for each getter.

On an authorized, licensed 2026.1.0529.7 installation with one ready Briosa server, prepare a local protobuf-JSON request file for one existing object or relationship. Run one command per invocation:

```powershell
dotnet run --project tools/Briosa.SmokeClient -c Release --no-build -- --licensed-reporting-frame object --confirm-licensed-sa2026 --address http://127.0.0.1:5000 --timeout-seconds 30 --request-file C:\fixtures\object-reporting-frame.json
dotnet run --project tools/Briosa.SmokeClient -c Release --no-build -- --licensed-reporting-frame relationship --confirm-licensed-sa2026 --address http://127.0.0.1:5000 --timeout-seconds 30 --request-file C:\fixtures\relationship-reporting-frame.json
```

The probe checks the generated-client response and reports only structural state, result code, and whether a nonempty `Frame` result was verified. It does not print fixture names. Record the exact SA and activated SDK versions with the result. These scenarios are opt-in and have not been run as part of portable validation.
