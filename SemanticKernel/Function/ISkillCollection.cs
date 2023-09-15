using System.Diagnostics.CodeAnalysis;

namespace SemanticKernel.Function;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public interface ISkillCollection : IReadOnlySkillCollection
{
    ISkillCollection AddFunction(ISKFunction functionInstance);
}
