# SA 2026.1.0529.7 value-family evidence

This generated report summarizes the reviewed exact-target value-family source of truth. Inventory membership and evidence do not approve a public operation.

- Families: 115
- Exact enum types: 42
- Exact enum members and SDK literals: 470
- Structured value types: 35
- Public structured fields: 108
- Worker structured fields: 108
- Shared SDK methods: 6
- Exact command assignments: 995
- ObjectiveSA corroborated defaults: 421
- Reviewed candidates retaining required input: 314
- Defaults awaiting #82 review: 0

## Shared-method domains

| SDK method | Reviewed families | Assignments |
| --- | --- | ---: |
| `GetCollectionObjectNameArg` | `collection_item_name`, `collection_object_name` | 39 |
| `GetCollectionObjectNameRefListArg` | `collection_item_name_list`, `collection_object_name_list` | 60 |
| `SetAsciiFileFormatArg` | `ascii_frame_set_format`, `ascii_import_file_format` | 3 |
| `SetAxisNameArg` | `axis_identifier`, `wcf_axis_identifier` | 6 |
| `SetCollectionObjectNameArg2` | `collection_item_name`, `collection_object_name` | 664 |
| `SetCollectionObjectNameRefListArg` | `collection_item_name_list`, `collection_object_name_list` | 223 |

## Evidence sources

| Source | Kind | Fingerprint | Raw material committed |
| --- | --- | --- | --- |
| `exact_target_interop` | `exact_target_interop_public_api` | `e2cdb8a2aa53b55cc96c94d91d537ca1c1f25a39402cf91abf11b053464b9f42` | `True` |
| `installed_command_documentation` | `installed_mp_documentation` | `21d20f9cc79c37ca3515d184a5de3d820b8ecabff4a2da4f24977628d79b8d3a` | `False` |
| `instrument_list` | `installed_instrument_model_list` | `0e0e31124355c5b3ec02f8510e2de1d22fd993471024d6210178114264b490f7` | `False` |
| `objectivesa_secondary` | `prior_release_secondary_evidence` | `d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b` | `False` |
| `view_sdk_code` | `generated_sdk_sample` | `cc12ba5bd8ded0e9af45eecb59c7894b1f19d0e45aa961cebb60c877cc72ef86` | `False` |
