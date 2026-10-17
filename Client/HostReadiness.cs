using System.Reflection;

namespace OblivionTravel.Client;

// rc.1 offers no public restore-complete query. This adapter only reads the inspected contract;
// absence or a changed contract defers all native calls. Never writes/invokes any save method.
internal sealed class HostReadiness
{
    PropertyInfo? instanceProperty, saveProperty, stateProperty, areaProperty;
    FieldInfo? completedField;

    public (bool Restored, string Area) Read()
    {
        if (instanceProperty is null)
        {
            var type = Assembly.Load("OblivionMpCSharpMod").GetType("OblivionMpCSharpMod.DI", true)!;
            instanceProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                ?? throw new MissingMemberException("DI.Instance");
            saveProperty = type.GetProperty("PlayerSave") ?? throw new MissingMemberException("DI.PlayerSave");
            stateProperty = type.GetProperty("OblivionClientState") ?? throw new MissingMemberException("DI.OblivionClientState");
        }
        var instance = instanceProperty.GetValue(null);
        if (instance is null) return (false, "");
        var save = saveProperty!.GetValue(instance);
        var state = stateProperty!.GetValue(instance);
        if (save is null || state is null) return (false, "");
        completedField ??= save.GetType().GetField("_restoreCompleted", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException("PlayerSaveCoordinator._restoreCompleted");
        areaProperty ??= state.GetType().GetProperty("CurrentAreaId") ?? throw new MissingMemberException("CurrentAreaId");
        var area = areaProperty.GetValue(state)?.ToString() ?? "";
        if (area.StartsWith("AreaId[", StringComparison.Ordinal) && area.EndsWith(']')) area = area[7..^1];
        return (completedField.GetValue(save) is true, area);
    }
}
