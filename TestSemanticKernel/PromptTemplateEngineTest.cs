
using SemanticKernel;
using SemanticKernel.Prompt;

namespace TestSemanticKernel
{
    public class PromptTemplateEngineTest
    {
        private PromptTemplateEngine? _target;

        [SetUp]
        public void Setup()
        {
            _target = new PromptTemplateEngine();
        }


        [Test]
        public async Task ItSupportsVariablesAsync()
        {
            const string Input = "template tests";
            const string Winner = "SK";
            const string Template = "And the winner\n of {{$input}} \nis: {{  $winner }}!";

            var kernel = Kernel.Builder.Build();
            var context = kernel.CreateNewContext();
            context.Variables["input"] = Input;
            context.Variables["winner"] = Winner;

            var result = await _target?.RenderAsync(Template, context)!;

            var expected = Template
                .Replace("{{$input}}", Input, StringComparison.OrdinalIgnoreCase)
                .Replace("{{  $winner }}", Winner, StringComparison.OrdinalIgnoreCase);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
