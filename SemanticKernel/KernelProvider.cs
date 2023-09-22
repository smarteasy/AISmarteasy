using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel
{
    public static class KernelProvider
    {
        static void Initialize(IKernel kernel)
        {
            Kernel = kernel;
        }

        public static IKernel Kernel { get; set; } = null!;
    }
}
