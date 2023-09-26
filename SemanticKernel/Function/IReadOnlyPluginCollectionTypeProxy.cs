using System.Diagnostics;

namespace SemanticKernel.Function;

internal sealed class ReadOnlyPluginCollectionTypeProxy
{
    private readonly IReadOnlyPluginCollection _collection;

    public ReadOnlyPluginCollectionTypeProxy(IReadOnlyPluginCollection collection) => this._collection = collection;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public PluginProxy[] Items
    {
        get
        {
            var view = this._collection.GetFunctionsView();
            return view.FunctionViews
                .GroupBy(f => f.Key)
                .Select(g => new PluginProxy(g.SelectMany(f => f.Value)) { Name = g.Key })
                .ToArray();
        }
    }

    [DebuggerDisplay("{Name}")]
    public sealed class PluginProxy : List<FunctionView>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string? Name;

        public PluginProxy(IEnumerable<FunctionView> functions) : base(functions) { }
    }
}
