using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using dnlib.DotNet;
using PiratePDF.Fuzz.Targets;
using SharpFuzz;

namespace PiratePDF.Fuzz.SharpFuzz;

internal static class RegistryDiagnostics
{
    internal static int Run(string mode)
    {
        if (mode == "inspect-il")
        {
            Dump(Path.Combine(AppContext.BaseDirectory, "PiratePDF.Fuzz.Targets.dll"),
                "PiratePDF.Fuzz.Targets.TargetRegistry", ["Get", ".cctor"]);
            Dump(Path.Combine(AppContext.BaseDirectory, "SharpFuzz.Common.dll"),
                "SharpFuzz.Common.Trace", [".cctor"]);
            Dump(Path.Combine(AppContext.BaseDirectory, "SharpFuzz.dll"),
                "SharpFuzz.TraceWrapper", [".ctor"]);
            Dump(Path.Combine(AppContext.BaseDirectory, "SharpFuzz.dll"),
                "SharpFuzz.Fuzzer/LibFuzzer", ["Run"]);
            return 0;
        }
        if (mode == "registry-runtime")
        {
            if (new FileInfo(mode).Length != 0) throw new InvalidOperationException("Control fixture must be empty.");
            var session = new SharpFuzzTargetSession("filter-asciihex");
            Console.WriteLine(JsonSerializer.Serialize(new { beforeRuntimeSharedMemoryIsZero = SharedMemoryIsZero() }));
            Fuzzer.LibFuzzer.Run(_ =>
            {
                if (SharedMemoryIsZero()) throw new InvalidOperationException("Coverage was not initialized.");
                WriteRegistry();
                if (session.Invoke(">"u8).Outcome != "success" || session.Invoke("G>"u8).Outcome != "rejected")
                    throw new InvalidOperationException("Control target outcomes differ.");
                Console.WriteLine("{\"runtimeCallback\":true,\"sharedMemoryIsZero\":false,\"success\":true,\"rejection\":true}");
            });
            return 0;
        }
        if (mode != "registry-direct") return 2;
        Console.WriteLine(JsonSerializer.Serialize(new { sharedMemoryIsZero = SharedMemoryIsZero() }));
        WriteRegistry();
        return 0;
    }

    private static bool SharedMemoryIsZero()
    {
        // Reflection boxes a native pointer even when it is zero. Compare the pointer value,
        // without dereferencing it or printing a process address. Diagnostic code only.
        Type trace = Assembly.Load("SharpFuzz.Common").GetType("SharpFuzz.Common.Trace", throwOnError: true)!;
        var compare = new DynamicMethod("CoveragePointerIsZero", typeof(bool), Type.EmptyTypes);
        ILGenerator il = compare.GetILGenerator();
        il.Emit(OpCodes.Ldsfld, trace.GetField("SharedMem")!);
        il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Ldc_I8, 0L);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Ret);
        return compare.CreateDelegate<Func<bool>>()();
    }

    internal static void WriteRegistry()
    {
        FuzzTarget target = TargetRegistry.Get("filter-asciihex");
        string[] ids = TargetRegistry.All.Select(static entry => entry.Id).ToArray();
        if (target.Id != "filter-asciihex" || target.ConfigurationVersion != 1 || target.MaximumInputBytes != 16384
            || target.Isolation != FuzzIsolation.SupervisedManagedWorker
            || !ids.SequenceEqual(new[] { "filter-asciihex", "filter-flate", "font-sanitize" }))
            throw new InvalidOperationException("Registry diagnostic contract mismatch.");
        Console.WriteLine(JsonSerializer.Serialize(new { target.Id, target.ConfigurationVersion,
            target.MaximumInputBytes, isolation = target.Isolation.ToString(), ids }));
    }

    private static void Dump(string path, string typeName, string[] methodNames)
    {
        using ModuleDefMD module = ModuleDefMD.Load(path);
        TypeDef? type = module.GetTypes().SingleOrDefault(type => type.FullName == typeName);
        if (type is null) { Console.WriteLine("Missing diagnostic type: " + typeName); return; }
        Console.WriteLine(type.FullName + " attributes=" + type.Attributes);
        foreach (FieldDef field in type.Fields.Take(16)) Console.WriteLine("field " + field.FullName);
        foreach (MethodDef method in type.Methods.Where(method => methodNames.Contains(method.Name.String)))
        {
            Console.WriteLine(method.FullName);
            if (!method.HasBody) continue;
            foreach (var instruction in method.Body.Instructions.Take(128)) Console.WriteLine(instruction.ToString());
        }
    }
}
