using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Security;
using System.Security.Cryptography;

if (args.Length == 3 && args[0] == "h2b-probe") return await H2bProbe.RunAsync(args[1], args[2]);
if (args.Length == 5 && args[0] == "station-inspect" && OperatingSystem.IsWindows() && (args[4] is "read-only" or "create-only"))
    return await LpacFeasibility.InspectStationsAsync(args[1], args[2], args[3], args[4] == "create-only");
if (args.Length == 5 && args[0] == "nested-job-feasibility" && OperatingSystem.IsWindows())
    return await LpacFeasibility.RunAsync(args[1], args[2], args[3], args[4], jobOnly: true);
if (args.Length == 5 && args[0] == "lpac-feasibility" && OperatingSystem.IsWindows())
    return await LpacFeasibility.RunAsync(args[1], args[2], args[3], args[4]);
if (args.Length == 5 && args[0] == "lpac-admission-probe" && OperatingSystem.IsWindows())
    return await LpacFeasibility.RunAsync(args[1], args[2], args[3], args[4], admissionOnly: true);
if (args.Length == 5 && args[0] == "lpac-profile-admission-probe" && OperatingSystem.IsWindows())
    return await LpacFeasibility.RunAsync(args[1], args[2], args[3], args[4], admissionOnly: true, createProfile: true);
if (args.Length != 5 || args[0] is not ("Inspect" or "Install" or "Verify" or "Recover" or "Remove" or "Backing")) return 64;
// Operation, plan path, approved plan hash, runner path, runner hash. No credential arguments/environment.
try
{
    string runner = Path.GetFullPath(args[3]);
    using var lease = new FileStream(runner, FileMode.Open, FileAccess.Read, FileShare.Read);
    if (!Convert.ToHexString(SHA256.HashData(lease)).Equals(args[4], StringComparison.OrdinalIgnoreCase)) throw new SecurityException("Provisioning runner identity mismatch.");
    Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(runner);
    Type operations = assembly.GetType("PiratePDF.Fuzz.Runner.NativeProvisioningOperations", true)!;
    using var secret = new SecureString();
    if (args[0] == "Verify")
    {
        if (Console.IsInputRedirected) throw new SecurityException("Credential entry requires an interactive protected console.");
        Console.Error.Write("Dedicated execution account password: ");
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (secret.Length > 0) secret.RemoveAt(secret.Length - 1); }
            else if (!char.IsControl(key.KeyChar)) secret.AppendChar(key.KeyChar);
        }
        Console.Error.WriteLine();
        secret.MakeReadOnly();
    }
    MethodInfo method = operations.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic)!;
    string result;
    try { result = (string)method.Invoke(null, [args[0], args[1], args[2], secret])!; }
    catch (TargetInvocationException failure) when (failure.InnerException is not null)
    { ExceptionDispatchInfo.Capture(failure.InnerException).Throw(); throw; }
    Console.WriteLine(result);
    return 0;
}
catch (Exception failure)
{
    Console.Error.WriteLine("Provisioning refused: " + failure.GetType().Name);
    return 2;
}
