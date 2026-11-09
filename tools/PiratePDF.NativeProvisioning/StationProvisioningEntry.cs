using PiratePDF.Fuzz.Runner;

// The station publishing profile excludes the generic development dispatcher.
if (!OperatingSystem.IsWindows() || args.Length != 2 || args[0] != NativeStationPolicy.Operation) return 64;
return await NativeStationTransaction.RunHelperAsync(args[1]);
