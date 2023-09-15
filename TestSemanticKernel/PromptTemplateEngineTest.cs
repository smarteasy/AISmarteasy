
using SemanticKernel;
using SemanticKernel.Function;
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

        [Test]
        public async Task ItSupportsValuesAsync()
        {
            const string Template = "And the winner\n of {{'template\ntests'}} \nis: {{  \"SK\" }}!";
            const string Expected = "And the winner\n of template\ntests \nis: SK!";

            var kernel = Kernel.Builder.Build();
            var context = kernel.CreateNewContext();

            var result = await _target?.RenderAsync(Template, context)!;

            Assert.That(result, Is.EqualTo(Expected));
        }

        [Test]
        public async Task ItAllowsToPassVariablesToFunctionsAsync()
        {
            const string Template = "== {{my.check123 $call}} ==";
            var kernel = Kernel.Builder.Build();
            kernel.ImportSkill(new MySkill(), "my");
            var context = kernel.CreateNewContext();
            context.Variables["call"] = "123";

            var result = await _target?.RenderAsync(Template, context)!;

            Assert.That(result, Is.EqualTo("== 123 ok =="));
        }

        [Test]
        public async Task ItAllowsToPassValuesToFunctionsAsync()
        {
            const string Template = "== {{my.check123 '234'}} ==";
            var kernel = Kernel.Builder.Build();
            kernel.ImportSkill(new MySkill(), "my");
            var context = kernel.CreateNewContext();

            var result = await _target?.RenderAsync(Template, context)!;

            Assert.That(result, Is.EqualTo("== 234 != 123 =="));
        }

        [Test]
        public async Task ItAllowsToPassEscapedValues1ToFunctionsAsync()
        {
            const char Esc = '\\';
            string template = "== {{my.check123 'a" + Esc + "'b'}} ==";
            var kernel = Kernel.Builder.Build();
            kernel.ImportSkill(new MySkill(), "my");
            var context = kernel.CreateNewContext();

            var result = await _target?.RenderAsync(template, context)!;

            Assert.That(result, Is.EqualTo("== a'b != 123 =="));
        }

        [Test]
        public async Task ItAllowsToPassEscapedValues2ToFunctionsAsync()
        {
            const char Esc = '\\';
            string template = "== {{my.check123 \"a" + Esc + "\"b\"}} ==";
            var kernel = Kernel.Builder.Build();
            kernel.ImportSkill(new MySkill(), "my");
            var context = kernel.CreateNewContext();

            var result = await _target?.RenderAsync(template, context)!;

            Assert.That(result, Is.EqualTo("== a\"b != 123 =="));
        }

        [Test]
        public async Task ItHandlesNamedArgsAsync()
        {
            // Arrange
            string template = "Output: {{my.sayAge name=\"Mario\" birthdate=$birthdate exclamation='Wow, that\\'s surprising'}}";
            var kernel = Kernel.Builder.Build();
            kernel.ImportSkill(new MySkill(), "my");
            var context = kernel.CreateNewContext();
            context.Variables["birthdate"] = "1981-08-20T00:00:00";

            var result = await _target?.RenderAsync(template, context)!;

            Assert.That(result, Is.EqualTo("Output: Mario is 42 today. Wow, that's surprising!"));
        }
    }

    public class MySkill
    {
        [SKFunction, Description("This is a test"), SKName("check123")]
        public string MyFunction(string input)
        {
            return input == "123" ? "123 ok" : input + " != 123";
        }

        [SKFunction, Description("This is a test"), SKName("asis")]
        public string MyFunction2(string input)
        {
            return input;
        }

        [SKFunction, Description("This is a test"), SKName("sayAge")]
        public string MyFunction3(string name, DateTime birthdate, string exclamation)
        {
            var today = new DateTime(2023, 8, 25);
            TimeSpan timespan = today - birthdate;
            int age = (int)(timespan.TotalDays / 365.25);
            return $"{name} is {age} today. {exclamation}!";
        }
    }
}
