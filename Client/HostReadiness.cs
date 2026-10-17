using System.Reflection;
using OblivionMp.Sdk.Common.Archetypes;
using ReadyM.SDK.Entities;

namespace OblivionTravel.Client;

// rc.1's save-status component is internal. Read only; no save/load method or component mutation.
internal sealed class HostReadiness
{
    PropertyInfo? instanceProperty, stateProperty, areaProperty, appliedProperty;
    MethodInfo? tryGetStatus;

    public (bool Restored, string Area) Read(MainCharacter player)
    {
        if (instanceProperty is null)
        {
            var type = Assembly.Load("OblivionMpCSharpMod").GetType("OblivionMpCSharpMod.DI", true)!;
            instanceProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                ?? throw new MissingMemberException("DI.Instance");
            stateProperty = type.GetProperty("OblivionClientState") ?? throw new MissingMemberException("DI.OblivionClientState");
        }
        if (tryGetStatus is null)
        {
            var statusType = Assembly.Load("ReadyM.Api.Multiplayer")
                .GetType("ReadyM.Api.Multiplayer.ECS.Components.PlayerSaveStatusComponent", true)!;
            appliedProperty = statusType.GetProperty("Applied") ?? throw new MissingMemberException("PlayerSaveStatusComponent.Applied");
            tryGetStatus = typeof(EntityHandle).GetMethod("TryGetComponent")!.MakeGenericMethod(statusType);
        }
        // Public generic read method, closed over the internal component type. No private entity fields are needed.
        object handle = EntityHandle.Of(in player);
        object?[] args = [null];
        var applied = tryGetStatus.Invoke(handle, args) is true && args[0] is { } status && appliedProperty!.GetValue(status) is true;
        var instance = instanceProperty.GetValue(null);
        if (instance is null) return (false, "");
        var state = stateProperty!.GetValue(instance);
        if (state is null) return (false, "");
        areaProperty ??= state.GetType().GetProperty("CurrentAreaId") ?? throw new MissingMemberException("CurrentAreaId");
        var area = areaProperty.GetValue(state)?.ToString() ?? "";
        if (area.StartsWith("AreaId[", StringComparison.Ordinal) && area.EndsWith(']')) area = area[7..^1];
        return (applied, area);
    }
}
