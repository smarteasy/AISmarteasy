using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;


#pragma warning disable CA1710 // ContextVariables doesn't end in Dictionary or Collection
#pragma warning disable CA1725, RCS1168 // Uses "name" instead of "key" for some public APIs
#pragma warning disable CS8767 // Reference type nullability doesn't match because netstandard2.0 surface area isn't nullable reference type annotated
// TODO: support more complex data types, and plan for rendering these values into prompt templates.

namespace SemanticKernel;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(ContextVariables.TypeProxy))]
public sealed class ContextVariables : IDictionary<string, string>
{
    public ContextVariables(string? value = null)
    {
        this._variables[MainKey] = value ?? string.Empty;
    }

    public ContextVariables Clone()
    {
        var clone = new ContextVariables();
        foreach (KeyValuePair<string, string> x in this._variables)
        {
            clone.Set(x.Key, x.Value);
        }

        return clone;
    }

    public string Input => this._variables.TryGetValue(MainKey, out string? value) ? value : string.Empty;

    public ContextVariables Update(string? value)
    {
        this._variables[MainKey] = value ?? string.Empty;
        return this;
    }

    public ContextVariables Update(ContextVariables newData, bool merge = true)
    {
        if (!object.ReferenceEquals(this, newData))
        {
            if (!merge) { this._variables.Clear(); }

            foreach (KeyValuePair<string, string> varData in newData._variables)
            {
                this._variables[varData.Key] = varData.Value;
            }
        }

        return this;
    }

    public void Set(string name, string? value)
    {
        Verify.NotNullOrWhiteSpace(name);
        if (value != null)
        {
            this._variables[name] = value;
        }
        else
        {
            this._variables.TryRemove(name, out _);
        }
    }

    public bool TryGetValue(string name, [NotNullWhen(true)] out string? value)
    {
        if (this._variables.TryGetValue(name, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    public string this[string name]
    {
        get => this._variables[name];
        set
        {
            this._variables[name] = value;
        }
    }

    public bool ContainsKey(string name)
    {
        return this._variables.ContainsKey(name);
    }

    public override string ToString() => this.Input;

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this._variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this._variables.GetEnumerator();
    void IDictionary<string, string>.Add(string key, string value) => ((IDictionary<string, string>)this._variables).Add(key, value);
    bool IDictionary<string, string>.Remove(string key) => ((IDictionary<string, string>)this._variables).Remove(key);
    void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)this._variables).Add(item);
    void ICollection<KeyValuePair<string, string>>.Clear() => ((ICollection<KeyValuePair<string, string>>)this._variables).Clear();
    bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)this._variables).Contains(item);
    void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)this._variables).CopyTo(array, arrayIndex);
    bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)this._variables).Remove(item);
    ICollection<string> IDictionary<string, string>.Keys => ((IDictionary<string, string>)this._variables).Keys;
    ICollection<string> IDictionary<string, string>.Values => ((IDictionary<string, string>)this._variables).Values;
    int ICollection<KeyValuePair<string, string>>.Count => ((ICollection<KeyValuePair<string, string>>)this._variables).Count;
    bool ICollection<KeyValuePair<string, string>>.IsReadOnly => ((ICollection<KeyValuePair<string, string>>)this._variables).IsReadOnly;

    string IDictionary<string, string>.this[string key]
    {
        get => ((IDictionary<string, string>)this._variables)[key];
        set => ((IDictionary<string, string>)this._variables)[key] = value;
    }

    internal const string MainKey = "INPUT";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay =>
        this.TryGetValue(MainKey, out string? input) && !string.IsNullOrEmpty(input)
            ? $"Variables = {this._variables.Count}, Input = {input}"
            : $"Variables = {this._variables.Count}";

    private readonly ConcurrentDictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    private sealed class TypeProxy
    {
        private readonly ContextVariables _variables;

        public TypeProxy(ContextVariables variables) => this._variables = variables;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, string>[] Items => this._variables._variables.ToArray();
    }
}
