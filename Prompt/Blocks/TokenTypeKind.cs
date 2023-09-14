using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Prompt.Blocks
{
    public enum TokenTypeKind
    {
        None = 0,
        Value = 1,
        Variable = 2,
        FunctionId = 3,
        NamedArg = 4,
    }
}
