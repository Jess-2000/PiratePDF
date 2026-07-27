namespace PiratePDF.DocGen.Infrastructure;

internal static class RepositoryLocator
{
    private const string SolutionFileName =
        "PiratePDF.slnx";

    public static string FindRepositoryRoot(
        string startingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            startingDirectory);

        var directory = new DirectoryInfo(
            Path.GetFullPath(startingDirectory));

        while (directory is not null)
        {
            var solutionPath = Path.Combine(
                directory.FullName,
                SolutionFileName);

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate '{SolutionFileName}' from " +
            $"'{startingDirectory}'.");
    }
}