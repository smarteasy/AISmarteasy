using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel
{
    public static class KernelProvider
    {
        static void Initialize(Kernel kernel)
        {
            Kernel = kernel;
        }

        public static Kernel Kernel { get; set; } = null!;
    }
}
