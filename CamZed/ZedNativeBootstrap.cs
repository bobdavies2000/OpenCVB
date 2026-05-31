using System.Runtime.InteropServices;

/// <summary>
/// Loads ZED native DLLs before any sl.* P/Invoke.
/// sl_zed_c (NuGet 5.3) requires sl_zed64 from ZED SDK 5.3+ (SVO_ENCODING_PRESET).
/// </summary>
internal static class ZedNativeBootstrap
{
    private const string SlZedCName = "sl_zed_c.dll";
    private const string SlZed64Name = "sl_zed64.dll";
    private const long MinBytesForSlZedC53 = 250_000;
    private const string SlZedCExportMarker = "enable_recording_from_params";
    private const string SlZed64EncodingMarker = "SVO_ENCODING_PRESET";

    private static bool _loaded;
    private static string? _slZed64Path;
    private static string? _slZedCPath;

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        lock (typeof(ZedNativeBootstrap))
        {
            if (_loaded) return;

            var baseDir = AppContext.BaseDirectory;
            EnsureSlZed64Loaded(baseDir);

            var slZedCPath = Path.GetFullPath(Path.Combine(baseDir, SlZedCName));
            if (!File.Exists(slZedCPath))
            {
                throw new FileNotFoundException(
                    $"ZED C API wrapper not found: {slZedCPath}. Rebuild so Stereolabs.zed 5.3 copies {SlZedCName} next to the executable.",
                    slZedCPath);
            }

            ValidateSlZedC(slZedCPath);
            NativeLibrary.Load(slZedCPath);
            _slZedCPath = slZedCPath;
            _loaded = true;
        }
    }

    public static string LoadedPath => _slZedCPath ?? "(not loaded)";

    public static string LoadedSlZed64Path => _slZed64Path ?? "(not loaded)";

    private static void EnsureSlZed64Loaded(string baseDir)
    {
        foreach (var candidate in GetSlZed64Candidates(baseDir))
        {
            if (!File.Exists(candidate) || !DllContainsAsciiMarker(candidate, SlZed64EncodingMarker))
                continue;

            NativeLibrary.Load(candidate);
            _slZed64Path = candidate;
            return;
        }

        throw new InvalidOperationException(
            $"No compatible {SlZed64Name} (ZED SDK 5.3+ required). Run: .\\ZED_SDK\\Install-ZedSdk.ps1 " +
            $"Searched: {string.Join("; ", GetSlZed64Candidates(baseDir))}");
    }

    private static IEnumerable<string> GetSlZed64Candidates(string baseDir)
    {
        yield return Path.GetFullPath(Path.Combine(baseDir, SlZed64Name));

        var repoNative = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "ZED_SDK", "native", "bin", SlZed64Name));
        yield return repoNative;

        var sdkRoot = Environment.GetEnvironmentVariable("ZED_SDK_ROOT");
        if (!string.IsNullOrWhiteSpace(sdkRoot))
            yield return Path.Combine(sdkRoot, "bin", SlZed64Name);

        foreach (var sdkBin in new[]
        {
            @"C:\Program Files (x86)\ZED SDK\bin",
            @"C:\Program Files\ZED SDK\bin",
        })
            yield return Path.Combine(sdkBin, SlZed64Name);
    }

    private static void ValidateSlZedC(string dllPath)
    {
        var info = new FileInfo(dllPath);
        if (info.Length < MinBytesForSlZedC53)
        {
            throw new InvalidOperationException(
                $"Stale {SlZedCName} at {dllPath} ({info.Length} bytes). Need ~276 KB from Stereolabs.zed 5.3 NuGet.");
        }

        if (!DllContainsAsciiMarker(dllPath, SlZedCExportMarker))
        {
            throw new InvalidOperationException(
                $"{dllPath} is not sl_zed_c 5.3 (missing '{SlZedCExportMarker}').");
        }
    }

    private static bool DllContainsAsciiMarker(string path, string marker) =>
        System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(path))
            .Contains(marker, StringComparison.Ordinal);
}
