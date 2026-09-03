using System;
using System.Collections.Generic;
using System.IO;

internal static class LocatorTests
{
    private static int Main(string[] args)
    {
        List<string> candidates = TyporaLocator.FindCandidates();
        foreach (string candidate in candidates)
        {
            if (!TyporaLocator.IsTypora(candidate))
            {
                Console.Error.WriteLine("Invalid candidate: " + candidate);
                return 2;
            }
            Console.WriteLine(TyporaLocator.GetDisplayVersion(candidate) + " | " + candidate);
        }

        Console.WriteLine(candidates.Count == 0
            ? "No registered or running Typora was detected; installer will use manual selection."
            : "Automatic detection returned " + candidates.Count + " candidate(s).");

        if (args.Length != 2)
        {
            Console.Error.WriteLine("Expected a test directory and a Typora.exe fixture path.");
            return 3;
        }

        if (!TyporaLocator.IsTypora(args[1]))
        {
            Console.Error.WriteLine("Manual-selection validation rejected the fixture.");
            return 4;
        }

        Directory.CreateDirectory(args[0]);
        TyporaLocator.WriteConfiguredPath(args[0], args[1]);
        string roundTrip = TyporaLocator.ReadConfiguredPath(args[0]);
        if (!string.Equals(roundTrip, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Configured path round-trip failed.");
            return 5;
        }

        Console.WriteLine("Locator and configuration tests passed.");
        return 0;
    }
}
