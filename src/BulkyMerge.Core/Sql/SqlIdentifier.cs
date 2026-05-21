using System.Text.RegularExpressions;

namespace BulkyMerge.Sql;

public static partial class SqlIdentifier
{
    private static readonly Regex ValidName = ValidNameRegex();

    public static string RequireValid(string name, string paramName = "identifier")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BulkyMergeException($"{paramName} cannot be empty.");

        if (!ValidName.IsMatch(name))
            throw new BulkyMergeException(
                $"Invalid SQL identifier '{name}'. Use letters, digits, and underscores; max length 128.");

        return name;
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidNameRegex();
}
