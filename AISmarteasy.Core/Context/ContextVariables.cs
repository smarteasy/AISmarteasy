using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AISmarteasy.Core.Function;

namespace AISmarteasy.Core.Context;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(TypeProxy))]
public sealed class ContextVariables : IDictionary<string, string>
{
    public ContextVariables(string? value = null)
    {
        _variables[MainKey] = value ?? string.Empty;
    }

    public ContextVariables Clone()
    {
        var clone = new ContextVariables();
        foreach (KeyValuePair<string, string> x in _variables)
        {
            clone.Set(x.Key, x.Value);
        }

        return clone;
    }

    public string Input => _variables.TryGetValue(MainKey, out string? value) ? value : string.Empty;

    public ContextVariables Update(string? value)
    {
        _variables[MainKey] = value ?? string.Empty;
        return this;
    }

    public ContextVariables Update(ContextVariables newData, bool merge = true)
    {
        if (ReferenceEquals(this, newData)) return this;

        if (!merge) { _variables.Clear(); }

        foreach (KeyValuePair<string, string> varData in newData._variables)
        {
            _variables[varData.Key] = varData.Value;
        }

        return this;
    }

    public void Set(string name, string? value)
    {
        Verify.NotNullOrWhitespace(name);
        if (value != null)
        {
            _variables[name] = value;
        }
        else
        {
            _variables.TryRemove(name, out _);
        }
    }

    public bool TryGetValue(string name, [NotNullWhen(true)] out string? value)
    {
        if (_variables.TryGetValue(name, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    public string this[string name]
    {
        get => _variables[name];
        set => _variables[name] = value;
    }

    public bool ContainsKey(string name)
    {
        return _variables.ContainsKey(name);
    }

    public override string ToString() => Input;

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _variables.GetEnumerator();
    void IDictionary<string, string>.Add(string key, string value) => ((IDictionary<string, string>)_variables).Add(key, value);
    bool IDictionary<string, string>.Remove(string key) => ((IDictionary<string, string>)_variables).Remove(key);
    void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_variables).Add(item);
    void ICollection<KeyValuePair<string, string>>.Clear() => ((ICollection<KeyValuePair<string, string>>)_variables).Clear();
    bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_variables).Contains(item);
    void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_variables).CopyTo(array, arrayIndex);
    bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_variables).Remove(item);
    ICollection<string> IDictionary<string, string>.Keys => ((IDictionary<string, string>)_variables).Keys;
    ICollection<string> IDictionary<string, string>.Values => ((IDictionary<string, string>)_variables).Values;
    int ICollection<KeyValuePair<string, string>>.Count => ((ICollection<KeyValuePair<string, string>>)_variables).Count;
    bool ICollection<KeyValuePair<string, string>>.IsReadOnly => ((ICollection<KeyValuePair<string, string>>)_variables).IsReadOnly;

    string IDictionary<string, string>.this[string key]
    {
        get => ((IDictionary<string, string>)_variables)[key];
        set => ((IDictionary<string, string>)_variables)[key] = value;
    }

    internal const string MainKey = "INPUT";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay =>
        TryGetValue(MainKey, out string? input) && !string.IsNullOrEmpty(input)
            ? $"Variables = {_variables.Count}, Input = {input}"
            : $"Variables = {_variables.Count}";

    private readonly ConcurrentDictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    private sealed class TypeProxy
    {
        private readonly ContextVariables _variables;

        public TypeProxy(ContextVariables variables) => _variables = variables;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, string>[] Items => _variables._variables.ToArray();
    }
}
