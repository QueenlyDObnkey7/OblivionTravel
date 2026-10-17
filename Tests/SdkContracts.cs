using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Reads metadata only: never loads or executes game/host assemblies.
static class SdkContracts
{
    public static void Check(string directory, Action<bool, string> check)
    {
        Inspect("ReadyM.Api.Multiplayer.dll", reader =>
        {
            var status = Type(reader, "ReadyM.Api.Multiplayer.ECS.Components.PlayerSaveStatusComponent");
            check((status.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic, "rc.1 save-status component is internal");
            check(PublicGetter(reader, status, "Applied"), "rc.1 save-status Applied public getter exists");
        });
        Inspect("ReadyM.SDK.dll", reader =>
        {
            var handle = Type(reader, "ReadyM.SDK.Entities.EntityHandle");
            bool GenericRead(string name) => handle.GetMethods().Select(reader.GetMethodDefinition).Any(m =>
                reader.GetString(m.Name) == name && m.GetGenericParameters().Count == 1 &&
                (m.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public);
            check(GenericRead("TryGetComponent"), "public generic EntityHandle.TryGetComponent read contract exists");
            check(GenericRead("Of"), "public generic EntityHandle.Of shape contract exists");
        });
        Inspect("OblivionMpCSharpMod.dll", reader =>
        {
            var di = Type(reader, "OblivionMpCSharpMod.DI");
            check(di.GetProperties().Select(reader.GetPropertyDefinition).Any(p => reader.GetString(p.Name) == "Instance"), "rc.1 DI.Instance exists");
            check(PublicGetter(reader, di, "OblivionClientState"), "rc.1 DI native client-state getter exists");
            check(PublicGetter(reader, Type(reader, "OblivionMpCSharpMod.State.NativeClientState"), "CurrentAreaId"), "rc.1 native current-area getter exists");
        });
        return;

        void Inspect(string name, Action<MetadataReader> action)
        {
            using var file = File.OpenRead(Path.Combine(directory, name));
            using var pe = new PEReader(file);
            action(pe.GetMetadataReader());
        }
    }

    static TypeDefinition Type(MetadataReader reader, string fullName) => reader.TypeDefinitions.Select(reader.GetTypeDefinition)
        .Single(t => reader.GetString(t.Namespace) + "." + reader.GetString(t.Name) == fullName);

    static bool PublicGetter(MetadataReader reader, TypeDefinition type, string name)
    {
        var property = type.GetProperties().Select(reader.GetPropertyDefinition).SingleOrDefault(p => reader.GetString(p.Name) == name);
        var getter = property.GetAccessors().Getter;
        return !getter.IsNil && (reader.GetMethodDefinition(getter).Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
    }
}
