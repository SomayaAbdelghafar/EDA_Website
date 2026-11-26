using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace EDA_Website.Localization;

/// <summary>
/// All translated text of the site, read from Resources/Translations/{language}/*.json.
/// <para>
/// A file's name is the first part of each key, and nested objects add the rest:
/// "ar/Home.json" containing <c>{ "Hero": { "Title": "…" } }</c> defines the key "Home.Hero.Title".
/// Arrays become numbered keys ("Data.Services.split-window.Points.0", ".1", …).
/// </para>
/// <para>
/// A key missing in a language falls back to the default language (and is logged once), so a page never
/// breaks because one translation is missing. In Development the files are reloaded when they change.
/// </para>
/// </summary>
public sealed class TranslationStore : IDisposable
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip, // translators may leave // notes in the files
        AllowTrailingCommas = true
    };

    private readonly string _root;
    private readonly string _defaultLanguage;
    private readonly IReadOnlyList<string> _languages;
    private readonly ILogger<TranslationStore> _logger;
    private readonly ConcurrentDictionary<string, byte> _reported = new();
    private readonly PhysicalFileProvider? _watcher;
    private readonly IDisposable? _watchRegistration;
    private volatile Snapshot _snapshot;

    /// <summary>Texts and list lengths per language.</summary>
    private sealed record Snapshot(
        FrozenDictionary<string, FrozenDictionary<string, string>> Texts,
        FrozenDictionary<string, FrozenDictionary<string, int>> ListLengths);

    public TranslationStore(string root, IReadOnlyList<string> languages, string defaultLanguage, bool reloadOnChange, ILogger<TranslationStore> logger)
    {
        _root = root;
        _languages = languages;
        _defaultLanguage = defaultLanguage;
        _logger = logger;
        _snapshot = Load(); // invalid JSON stops the site at startup instead of showing pages with keys

        if (reloadOnChange && Directory.Exists(root))
        {
            _watcher = new PhysicalFileProvider(root);
            _watchRegistration = ChangeToken.OnChange(() => _watcher.Watch("**/*.json"), Reload);
        }
    }

    public string DefaultLanguage => _defaultLanguage;

    /// <summary>
    /// The text for <paramref name="key"/> in <paramref name="language"/>, else in the default language, else null.
    /// </summary>
    public string? Find(string key, string language, bool reportMissing = true)
    {
        var texts = _snapshot.Texts;
        if (texts.TryGetValue(language, out var own) && own.TryGetValue(key, out var value))
            return value;

        if (language != _defaultLanguage && texts.TryGetValue(_defaultLanguage, out var fallback) && fallback.TryGetValue(key, out value))
        {
            if (reportMissing)
                ReportOnce(language, key, "Missing translation {Language}:{Key}; showing the {Default} text instead");
            return value;
        }

        if (reportMissing)
            ReportOnce(language, key, "Unknown translation key {Language}:{Key} (not in the {Default} files either)");
        return null;
    }

    /// <summary>Number of items in the list <paramref name="key"/> (the language's own list, else the default language's).</summary>
    public int Count(string key, string language)
    {
        var lengths = _snapshot.ListLengths;
        if (lengths.TryGetValue(language, out var own) && own.TryGetValue(key, out var count))
            return count;
        return lengths.TryGetValue(_defaultLanguage, out var fallback) ? fallback.GetValueOrDefault(key) : 0;
    }

    /// <summary>All texts under "<paramref name="prefix"/>." keyed by the rest of their key, with default-language fallback.</summary>
    public IReadOnlyDictionary<string, string> Section(string prefix, string language)
    {
        var start = prefix + ".";
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var lang in language == _defaultLanguage ? new[] { language } : new[] { _defaultLanguage, language })
        {
            if (!_snapshot.Texts.TryGetValue(lang, out var texts))
                continue;
            foreach (var (key, value) in texts)
            {
                if (key.StartsWith(start, StringComparison.Ordinal))
                    result[key[start.Length..]] = value;
            }
        }
        return result;
    }

    public IEnumerable<KeyValuePair<string, string>> All(string language) =>
        _snapshot.Texts.TryGetValue(language, out var texts) ? texts : [];

    /// <summary>Keys of the default language that <paramref name="language"/> does not translate.</summary>
    public IReadOnlyList<string> MissingKeys(string language) => KeysOnlyIn(_defaultLanguage, language);

    /// <summary>Keys of <paramref name="language"/> that the default language does not have (usually typos).</summary>
    public IReadOnlyList<string> UnknownKeys(string language) => KeysOnlyIn(language, _defaultLanguage);

    private IReadOnlyList<string> KeysOnlyIn(string language, string other)
    {
        var texts = _snapshot.Texts;
        if (!texts.TryGetValue(language, out var keys))
            return [];
        var otherKeys = texts.TryGetValue(other, out var o) ? o : FrozenDictionary<string, string>.Empty;
        return keys.Keys.Where(k => !otherKeys.ContainsKey(k)).Order(StringComparer.Ordinal).ToList();
    }

    private void ReportOnce(string language, string key, string message)
    {
        if (_reported.TryAdd(language + ":" + key, 0))
            _logger.LogWarning(message, language, key, _defaultLanguage);
    }

    private void Reload()
    {
        try
        {
            _snapshot = Load();
            _reported.Clear();
            _logger.LogInformation("Translations reloaded from {Root}", _root);
        }
        catch (Exception ex)
        {
            // A half-saved file in the editor: keep serving the previous texts until it is valid again
            _logger.LogError(ex, "Translations not reloaded; the previous texts stay in use");
        }
    }

    private Snapshot Load()
    {
        var texts = new Dictionary<string, FrozenDictionary<string, string>>();
        var lengths = new Dictionary<string, FrozenDictionary<string, int>>();

        foreach (var language in _languages)
        {
            var folder = Path.Combine(_root, language);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var lists = new Dictionary<string, int>(StringComparer.Ordinal);

            if (!Directory.Exists(folder))
                _logger.LogWarning("No translation folder for language {Language}: {Folder}", language, folder);
            else
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal))
                {
                    using var document = ParseFile(file);
                    Flatten(document.RootElement, Path.GetFileNameWithoutExtension(file), values, lists, file);
                }
            }

            texts[language] = values.ToFrozenDictionary(StringComparer.Ordinal);
            lengths[language] = lists.ToFrozenDictionary(StringComparer.Ordinal);
        }

        return new Snapshot(texts.ToFrozenDictionary(), lengths.ToFrozenDictionary());
    }

    private static JsonDocument ParseFile(string file)
    {
        try
        {
            return JsonDocument.Parse(File.ReadAllText(file), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Translation file {file} is not valid JSON: {ex.Message}", ex);
        }
    }

    private void Flatten(JsonElement element, string path, Dictionary<string, string> values, Dictionary<string, int> lists, string file)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    Flatten(property.Value, path + "." + property.Name, values, lists, file);
                break;
            case JsonValueKind.Array:
                lists[path] = element.GetArrayLength();
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    Flatten(item, $"{path}.{index++}", values, lists, file);
                break;
            case JsonValueKind.String:
                Add(path, element.GetString()!);
                break;
            case JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False:
                Add(path, element.GetRawText());
                break;
        }

        void Add(string key, string value)
        {
            if (!values.TryAdd(key, value))
                _logger.LogWarning("Translation key {Key} is defined twice (last one in {File})", key, file);
            values[key] = value;
        }
    }

    public void Dispose()
    {
        _watchRegistration?.Dispose();
        _watcher?.Dispose();
    }
}
