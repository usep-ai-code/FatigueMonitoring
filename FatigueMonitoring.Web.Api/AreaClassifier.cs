using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api;

public sealed class AreaClassifier(IOptions<AreaMappingOptions> options)
{
    private readonly AreaMappingOptions _options = options.Value;

    public string Resolve(string? groupName, string? deviceName)
    {
        var source = string.Join(' ', new[] { groupName, deviceName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(source))
        {
            return _options.DefaultArea;
        }

        if (ContainsAny(source, _options.MiningKeywords))
        {
            return "Mining";
        }

        if (ContainsAny(source, _options.HaulingKeywords))
        {
            return "Hauling";
        }

        return _options.DefaultArea;
    }

    private static bool ContainsAny(string source, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
