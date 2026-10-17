using System.Runtime.InteropServices;

namespace OblivionTravel.Client;

// Narrow UE4SS x64 adapter for the same ObvConsole path used by Characters.MapClient/NativeScene.
// Only two fixed map policy commands can enter it. No game data, ESP, offsets, or marker catalogue is embedded.
internal sealed class NativeConsole
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)] delegate nint Named(nint self, string name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)] delegate nint Find(nint type, nint outer, string path, [MarshalAs(UnmanagedType.I1)] bool exact);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate nint Ref(nint self);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate uint NameId(nint name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Event(nint self, nint function, nint arguments);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] static extern nint GetModuleHandle(string name);

    readonly Named property, function;
    readonly Find find;
    readonly Ref offset, width, parameterSize, children, next, name;
    readonly NameId nameId;
    readonly Event invoke;
    readonly Dictionary<nint, Dictionary<string, (int Offset, int Size)>> layouts = [];

    public NativeConsole()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) throw new PlatformNotSupportedException("Windows x64 is required.");
        var module = GetModuleHandle("UE4SS.dll");
        if (module == 0) throw new NotSupportedException("UE4SS.dll is unavailable.");
        T Bind<T>(string symbol) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(module, symbol));
        find = Bind<Find>("?StaticFindObject_InternalSlow@UObjectGlobals@Unreal@RC@@YAPEAVUObject@23@PEAVUClass@23@PEAV423@PEB_W_N@Z");
        property = Bind<Named>("?GetPropertyByNameInChain@UObject@Unreal@RC@@QEAAPEAVFProperty@23@PEB_W@Z");
        function = Bind<Named>("?GetFunctionByNameInChain@UObject@Unreal@RC@@QEAAPEAVUFunction@23@PEB_W@Z");
        offset = Bind<Ref>("?GetOffset_Internal@FProperty@Unreal@RC@@QEAAAEAHXZ");
        width = Bind<Ref>("?GetElementSize@FProperty@Unreal@RC@@QEAAAEAHXZ");
        parameterSize = Bind<Ref>("?GetParmsSize@UFunction@Unreal@RC@@QEAAAEAGXZ");
        children = Bind<Ref>("?GetChildProperties@UStruct@Unreal@RC@@QEAAAEAPEAVFField@23@XZ");
        next = Bind<Ref>("?GetNext@FField@Unreal@RC@@AEAAAEAPEAV123@XZ");
        name = Bind<Ref>("?GetNamePrivate@FField@Unreal@RC@@AEAAAEAVFName@23@XZ");
        nameId = Bind<NameId>("?GetComparisonIndex@FName@Unreal@RC@@QEBAIXZ");
        invoke = Bind<Event>("?ProcessEvent@UObject@Unreal@RC@@QEAAXPEAVUFunction@23@PEAX@Z");
    }

    nint Default(string path) => find(0, 0, path, false) is var value && value != 0 ? value : throw new MissingMemberException(path);

    public void Run(nint pawn, string command)
    {
        if (command is not ("EnableFastTravel 0" or "EnableFastTravel 1")) throw new ArgumentException("Map command not allowed.");
        if (pawn == 0) throw new InvalidOperationException("Local pawn unavailable.");
        var controllerBytes = Call(Default("/Script/Engine.Default__GameplayStatics"), "GetPlayerController",
            [("WorldContextObject", 8), ("PlayerIndex", 4), ("ReturnValue", 8)],
            new Dictionary<string, byte[]> { ["WorldContextObject"] = BitConverter.GetBytes((long)pawn), ["PlayerIndex"] = BitConverter.GetBytes(0) });
        var controller = (nint)BitConverter.ToInt64(controllerBytes["ReturnValue"]);
        if (controller == 0) throw new InvalidOperationException("Local player controller unavailable.");
        var field = property(controller, "CheatManager");
        if (field == 0 || Marshal.ReadInt32(width(field)) != 8) throw new NotSupportedException("CheatManager pointer ABI changed.");
        var fieldOffset = Marshal.ReadInt32(offset(field));
        if (fieldOffset < 0 || fieldOffset > 1_048_576) throw new NotSupportedException("CheatManager bounds invalid.");
        var cheat = Marshal.ReadIntPtr(controller + fieldOffset);
        if (cheat == 0) throw new InvalidOperationException("Native console unavailable.");
        var text = Marshal.StringToHGlobalUni(command);
        try
        {
            var data = new byte[16];
            BitConverter.GetBytes((long)text).CopyTo(data, 0);
            BitConverter.GetBytes(command.Length + 1).CopyTo(data, 8);
            BitConverter.GetBytes(command.Length + 1).CopyTo(data, 12);
            Call(cheat, "ObvConsole", [("Arg", 16)], new Dictionary<string, byte[]> { ["Arg"] = data });
        }
        finally { Marshal.FreeHGlobal(text); }
    }

    Dictionary<string, byte[]> Call(nint target, string method, (string Name, int Width)[] expected, Dictionary<string, byte[]> inputs)
    {
        var fn = function(target, method);
        if (fn == 0) throw new MissingMethodException(method);
        var size = (ushort)Marshal.ReadInt16(parameterSize(fn));
        if (size == 0 || size > 4096) throw new NotSupportedException("Native parameter bounds invalid.");
        if (!layouts.TryGetValue(fn, out var fields))
        {
            var reflected = new List<(uint Id, int Offset, int Size)>();
            for (var p = Marshal.ReadIntPtr(children(fn)); p != 0; p = Marshal.ReadIntPtr(next(p)))
            {
                if (reflected.Count >= 32) throw new NotSupportedException("Native parameter chain invalid.");
                reflected.Add((nameId(name(p)), Marshal.ReadInt32(offset(p)), Marshal.ReadInt32(width(p))));
            }
            fields = [];
            if (method == "Conv_StringToName")
            {
                // Bootstrap FName conversion without assuming the engine's FName width.
                if (reflected.Count != 2) throw new NotSupportedException("FName conversion ABI changed.");
                var input = reflected.Single(p => p.Offset == 0 && p.Size == 16);
                var output = reflected.Single(p => p.Offset == 16 && p.Size is 8 or 12);
                fields.Add("InString", (input.Offset, input.Size));
                fields.Add("ReturnValue", (output.Offset, output.Size));
            }
            else
            {
                if (reflected.Count != expected.Length) throw new NotSupportedException($"Native parameter count changed: {method}.");
                foreach (var item in expected)
                {
                    var id = GetNameId(item.Name);
                    var actual = reflected.Single(p => p.Id == id && p.Size == item.Width);
                    fields.Add(item.Name, (actual.Offset, actual.Size));
                }
            }
            foreach (var f in fields.Values)
                if (f.Offset < 0 || f.Size <= 0 || f.Offset + f.Size > size) throw new NotSupportedException("Native field bounds invalid.");
            layouts.Add(fn, fields);
        }
        var bytes = new byte[size];
        foreach (var (key, value) in inputs)
        {
            var field = fields[key];
            if (field.Size != value.Length) throw new NotSupportedException("Native argument width mismatch.");
            value.CopyTo(bytes, field.Offset);
        }
        var memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, 0, memory, size);
            invoke(target, fn, memory);
            Marshal.Copy(memory, bytes, 0, size);
            return fields.ToDictionary(f => f.Key, f => bytes.AsSpan(f.Value.Offset, f.Value.Size).ToArray());
        }
        finally { Marshal.FreeHGlobal(memory); }
    }

    uint GetNameId(string value)
    {
        var text = Marshal.StringToHGlobalUni(value);
        try
        {
            var bytes = new byte[16];
            BitConverter.GetBytes((long)text).CopyTo(bytes, 0);
            BitConverter.GetBytes(value.Length + 1).CopyTo(bytes, 8);
            BitConverter.GetBytes(value.Length + 1).CopyTo(bytes, 12);
            var result = Call(Default("/Script/Engine.Default__KismetStringLibrary"), "Conv_StringToName", [],
                new Dictionary<string, byte[]> { ["InString"] = bytes })["ReturnValue"];
            var memory = Marshal.AllocHGlobal(result.Length);
            try { Marshal.Copy(result, 0, memory, result.Length); return nameId(memory); }
            finally { Marshal.FreeHGlobal(memory); }
        }
        finally { Marshal.FreeHGlobal(text); }
    }
}
