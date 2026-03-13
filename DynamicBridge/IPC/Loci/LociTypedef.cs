// TypeDefs copied from LociAPI. Enums can be properly used as they pull from the same API repository.
global using LociStatusSummary = (
    System.Guid ID,
    string FSPath,
    uint IconID,
    string Title,
    string Description
    );

global using LociPresetSummary = (
    System.Guid ID,
    string FSPath, System.Collections.Generic.List<uint> IconIDs,
    string Title,
    string Description
    );

global using LociEventSummary = (
    System.Guid ID,
    string FSPath,
    bool Enabled,
    LociApi.Enums.LociEventType EventType,
    string Title,
    string Description
    );
