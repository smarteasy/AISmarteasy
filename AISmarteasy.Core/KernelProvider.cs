namespace AISmarteasy.Core
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
