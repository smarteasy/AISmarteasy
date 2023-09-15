using System.Diagnostics;

namespace SemanticKernel.Function;

internal sealed class ReadOnlySkillCollectionTypeProxy
{
    private readonly IReadOnlySkillCollection _collection;

    public ReadOnlySkillCollectionTypeProxy(IReadOnlySkillCollection collection) => this._collection = collection;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public SkillProxy[] Items
    {
        get
        {
            var view = this._collection.GetFunctionsView();
            return view.NativeFunctions
                .Concat(view.SemanticFunctions)
                .GroupBy(f => f.Key)
                .Select(g => new SkillProxy(g.SelectMany(f => f.Value)) { Name = g.Key })
                .ToArray();
        }
    }

    [DebuggerDisplay("{Name}")]
    public sealed class SkillProxy : List<FunctionView>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string? Name;

        public SkillProxy(IEnumerable<FunctionView> functions) : base(functions) { }
    }
}
