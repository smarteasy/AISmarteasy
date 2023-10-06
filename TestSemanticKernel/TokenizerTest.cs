using AISmarteasy.Core.Prompt;
using AISmarteasy.Core.Prompt.Blocks;

namespace TestSemanticKernel
{
    public class TokenizerTest
    {
        private TemplateTokenizer? _target;

        [SetUp]
        public void Setup()
        {
            _target = new TemplateTokenizer();
        }

        [Test]
        public void TextBolckNull()
        {
            var bloks = _target?.Tokenize(null);
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void TextBolckEmpty()
        {
            var bloks = _target?.Tokenize("");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void TextBolckCase1()
        {
            var bloks = _target?.Tokenize(" ");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" "));
            }
        }

        [Test]
        public void TextBolckCase2()
        {
            var bloks = _target?.Tokenize("   ");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo("   "));
            }
        }

        [Test]
        public void TextBolckCase3()
        {
            var bloks = _target?.Tokenize(" {}  ");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" {}  "));
            }
        }

        [Test]
        public void TextBolckCase4()
        {
            var bloks = _target?.Tokenize(" {{}  ");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" {{}  "));
            }
        }

        [Test]
        public void TextBolckCase5()
        {
            var bloks = _target?.Tokenize(" {{ } } }  ");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" {{ } } }  "));
            }
        }

        [Test]
        public void TextBolckCase6()
        {
            var bloks = _target?.Tokenize(" { { }} }");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" { { }} }"));
            }
        }

        [Test]
        public void TextBolckCase7()
        {
            var bloks = _target?.Tokenize("{{  \"}}x");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo("{{  \"}}x"));
            }
        }

        [Test]
        public void TextBolckCase8()
        {
            var bloks = _target?.Tokenize("{{}}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block = (TextBlock)bloks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo("{{}}"));
            }
        }

        [Test]
        public void TextBolckCase9()
        {
            var bloks = _target?.Tokenize(" {{ }}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(2));

                var block1 = (TextBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block1.Content, Is.EqualTo(" "));

                var block2 = (TextBlock)bloks[1];
                Assert.That(block2.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block2.Content, Is.EqualTo("{{ }}"));
            }
        }

        [Test]
        public void TextBolckCase10()
        {
            var bloks = _target?.Tokenize(" {{  }}}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(3));

                var block1 = (TextBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block1.Content, Is.EqualTo(" "));

                var block2 = (TextBlock)bloks[1];
                Assert.That(block2.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block2.Content, Is.EqualTo("{{  }}"));

                var block3 = (TextBlock)bloks[2];
                Assert.That(block3.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block3.Content, Is.EqualTo("}"));
            }
        }

        [Test]
        public void TextBolckCase11()
        {
            var bloks = _target?.Tokenize("{{  '}}x");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block1 = (TextBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block1.Content, Is.EqualTo("{{  '}}x"));
            }
        }

        [Test]
        public void TextWithoutCode1()
        {
            var bloks = _target?.Tokenize("{{$}}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block1 = (VarBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$"));
            }
        }

        [Test]
        public void TextWithoutCode2()
        {
            var bloks = _target?.Tokenize("{{$a}}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block1 = (VarBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$a"));
                Assert.That(block1.Name, Is.EqualTo("a"));
            }
        }

        [Test]
        public void TextWithoutCode3()
        {
            var bloks = _target?.Tokenize("{{ $a}}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block1 = (VarBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$a"));
                Assert.That(block1.Name, Is.EqualTo("a"));
            }
        }

        [Test]
        public void TextWithoutCode4()
        {
            var bloks = _target?.Tokenize("{{  $a  }}");
            if (bloks != null)
            {
                Assert.That(bloks.Count, Is.EqualTo(1));

                var block1 = (VarBlock)bloks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$a"));
                Assert.That(block1.Name, Is.EqualTo("a"));
            }
        }
    }
}



//[Theory]
//[InlineData("{{code}}", BlockTypes.Code)]
//[InlineData("{{code }}", BlockTypes.Code)]
//[InlineData("{{ code }}", BlockTypes.Code)]
//[InlineData("{{  code }}", BlockTypes.Code)]
//[InlineData("{{  code  }}", BlockTypes.Code)]
//[InlineData("{{''}}", BlockTypes.Value)]
//[InlineData("{{' '}}", BlockTypes.Value)]
//[InlineData("{{ ' '}}", BlockTypes.Value)]
//[InlineData("{{ ' ' }}", BlockTypes.Value)]
//[InlineData("{{  ' ' }}", BlockTypes.Value)]
//[InlineData("{{  ' '  }}", BlockTypes.Value)]
//internal void ItParsesBasicBlocks(string? text, BlockTypes type)
//{
//    // Act
//    var blocks = this._target.Tokenize(text);

//    // Assert
//    Assert.Equal(1, blocks.Count);
//    Assert.Equal(type, blocks[0].Type);
//}

//[Theory]
//[InlineData(null, 1)]
//[InlineData("", 1)]
//[InlineData("}}{{a}} {{b}}x", 5)]
//[InlineData("}}{{ -a}} {{b}}x", 5)]
//[InlineData("}}{{ -a\n}} {{b}}x", 5)]
//[InlineData("}}{{ -a\n} } {{b}}x", 3)]
//public void ItTokenizesTheRightTokenCount(string? template, int blockCount)
//{
//    // Act
//    var blocks = this._target.Tokenize(template);

//    // Assert
//    Assert.Equal(blockCount, blocks.Count);
//}

//[Fact]
//public void ItTokenizesEdgeCasesCorrectly1()
//{
//    // Act
//    var blocks1 = this._target.Tokenize("{{{{a}}");
//    var blocks2 = this._target.Tokenize("{{'{{a}}");
//    var blocks3 = this._target.Tokenize("{{'a}}");
//    var blocks4 = this._target.Tokenize("{{a'}}");

//    // Assert - Count
//    Assert.Equal(2, blocks1.Count);
//    Assert.Equal(1, blocks2.Count);
//    Assert.Equal(1, blocks3.Count);
//    Assert.Equal(1, blocks4.Count);

//    // Assert - Type
//    Assert.Equal(BlockTypes.Text, blocks1[0].Type);
//    Assert.Equal(BlockTypes.Code, blocks1[1].Type);

//    // Assert - Content
//    Assert.Equal("{{", blocks1[0].Content);
//    Assert.Equal("a", blocks1[1].Content);
//}

//[Fact]
//public void ItTokenizesEdgeCasesCorrectly2()
//{
//    // Arrange
//    var template = "}}{{{ {$a}}}} {{b}}x}}";

//    // Act
//    var blocks = this._target.Tokenize(template);

//    // Assert
//    Assert.Equal(5, blocks.Count);

//    Assert.Equal("}}{", blocks[0].Content);
//    Assert.Equal(BlockTypes.Text, blocks[0].Type);

//    Assert.Equal("{$a", blocks[1].Content);
//    Assert.Equal(BlockTypes.Code, blocks[1].Type);

//    Assert.Equal("}} ", blocks[2].Content);
//    Assert.Equal(BlockTypes.Text, blocks[2].Type);

//    Assert.Equal("b", blocks[3].Content);
//    Assert.Equal(BlockTypes.Code, blocks[3].Type);

//    Assert.Equal("x}}", blocks[4].Content);
//    Assert.Equal(BlockTypes.Text, blocks[4].Type);
//}

//[Fact]
//public void ItTokenizesEdgeCasesCorrectly3()
//{
//    // Arrange
//    var template = "}}{{{{$a}}}} {{b}}$x}}";

//    // Act
//    var blocks = this._target.Tokenize(template);

//    // Assert
//    Assert.Equal(5, blocks.Count);

//    Assert.Equal("}}{{", blocks[0].Content);
//    Assert.Equal(BlockTypes.Text, blocks[0].Type);

//    Assert.Equal("$a", blocks[1].Content);
//    Assert.Equal(BlockTypes.Variable, blocks[1].Type);

//    Assert.Equal("}} ", blocks[2].Content);
//    Assert.Equal(BlockTypes.Text, blocks[2].Type);

//    Assert.Equal("b", blocks[3].Content);
//    Assert.Equal(BlockTypes.Code, blocks[3].Type);

//    Assert.Equal("$x}}", blocks[4].Content);
//    Assert.Equal(BlockTypes.Text, blocks[4].Type);
//}

//[Theory]
//[InlineData("{{a$}}")]
//[InlineData("{{a$a}}")]
//[InlineData("{{a''}}")]
//[InlineData("{{a\"\"}}")]
//[InlineData("{{a'b'}}")]
//[InlineData("{{a\"b\"}}")]
//[InlineData("{{a'b'   }}")]
//[InlineData("{{a\"b\"    }}")]
//[InlineData("{{ asis 'f\\'oo' }}")]
//public void ItTokenizesEdgeCasesCorrectly4(string template)
//{
//    // Act
//    var blocks = this._target.Tokenize(template);

//    // Assert
//    Assert.Equal(1, blocks.Count);
//    Assert.Equal(BlockTypes.Code, blocks[0].Type);
//    Assert.Equal(template[2..^2].Trim(), blocks[0].Content);
//}

//[Fact]
//public void ItTokenizesATypicalPrompt()
//{
//    // Arrange
//    var template = "this is a {{ $prompt }} with {{$some}} variables " +
//                   "and {{function $calls}} {{ and 'values' }}";

//    // Act
//    var blocks = this._target.Tokenize(template);

//    // Assert
//    Assert.Equal(8, blocks.Count);

//    Assert.Equal("this is a ", blocks[0].Content);
//    Assert.Equal(BlockTypes.Text, blocks[0].Type);

//    Assert.Equal("$prompt", blocks[1].Content);
//    Assert.Equal(BlockTypes.Variable, blocks[1].Type);

//    Assert.Equal(" with ", blocks[2].Content);
//    Assert.Equal(BlockTypes.Text, blocks[2].Type);

//    Assert.Equal("$some", blocks[3].Content);
//    Assert.Equal(BlockTypes.Variable, blocks[3].Type);

//    Assert.Equal(" variables and ", blocks[4].Content);
//    Assert.Equal(BlockTypes.Text, blocks[4].Type);

//    Assert.Equal("function $calls", blocks[5].Content);
//    Assert.Equal(BlockTypes.Code, blocks[5].Type);

//    Assert.Equal(" ", blocks[6].Content);
//    Assert.Equal(BlockTypes.Text, blocks[6].Type);

//    Assert.Equal("and 'values'", blocks[7].Content);
//    Assert.Equal(BlockTypes.Code, blocks[7].Type);
//}

//[Fact]
//public void ItTokenizesAFunctionCallWithMultipleArguments()
//{
//    // Arrange
//    var template = "this is a {{ function with='many' named=$arguments }}";

//    // Act
//    var blocks = this._target.Tokenize(template);

//    // Assert
//    Assert.Equal(2, blocks.Count);

//    Assert.Equal("this is a ", blocks[0].Content);
//    Assert.Equal(BlockTypes.Text, blocks[0].Type);

//    Assert.Equal("function with='many' named=$arguments", blocks[1].Content);
//    Assert.Equal(BlockTypes.Code, blocks[1].Type);
//}

//[Fact]
//public void ItThrowsWhenCodeBlockStartsWithNamedArg()
//{
//    // Arrange
//    var template = "{{ not='valid' }}";

//    // Assert
//    var ex = Assert.Throws<SKException>(() =>
//    {
//        // Act
//        this._target.Tokenize(template);
//    });
//    Assert.Equal(ex.Message, "Code tokenizer returned an incorrect first token type NamedArg");
//}
//}
