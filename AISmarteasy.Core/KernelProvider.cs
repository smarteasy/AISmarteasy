namespace AISmarteasy.Core
{
    public static class KernelProvider
    {
        public static void Initialize(Kernel kernel)
        {
            Kernel = kernel;
        }

        public static Kernel? Kernel { get; private set; }
    }
}
