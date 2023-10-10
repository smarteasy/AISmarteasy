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
            var blocks = _target?.Tokenize(null);
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void TextBolckEmpty()
        {
            var blocks = _target?.Tokenize("");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void TextBolckCase1()
        {
            var blocks = _target?.Tokenize(" ");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" "));
            }
        }

        [Test]
        public void TextBolckCase2()
        {
            var blocks = _target?.Tokenize("   ");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo("   "));
            }
        }

        [Test]
        public void TextBolckCase3()
        {
            var blocks = _target?.Tokenize(" {}  ");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" {}  "));
            }
        }

        [Test]
        public void TextBolckCase4()
        {
            var blocks = _target?.Tokenize(" {{}  ");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" {{}  "));
            }
        }

        [Test]
        public void TextBolckCase5()
        {
            var blocks = _target?.Tokenize(" {{ } } }  ");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" {{ } } }  "));
            }
        }

        [Test]
        public void TextBolckCase6()
        {
            var blocks = _target?.Tokenize(" { { }} }");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo(" { { }} }"));
            }
        }

        [Test]
        public void TextBolckCase7()
        {
            var blocks = _target?.Tokenize("{{  \"}}x");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo("{{  \"}}x"));
            }
        }

        [Test]
        public void TextBolckCase8()
        {
            var blocks = _target?.Tokenize("{{}}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block = (TextBlock)blocks[0];
                Assert.That(block.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block.Content, Is.EqualTo("{{}}"));
            }
        }

        [Test]
        public void TextBolckCase9()
        {
            var blocks = _target?.Tokenize(" {{ }}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(2));

                var block1 = (TextBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block1.Content, Is.EqualTo(" "));

                var block2 = (TextBlock)blocks[1];
                Assert.That(block2.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block2.Content, Is.EqualTo("{{ }}"));
            }
        }

        [Test]
        public void TextBolckCase10()
        {
            var blocks = _target?.Tokenize(" {{  }}}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(3));

                var block1 = (TextBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block1.Content, Is.EqualTo(" "));

                var block2 = (TextBlock)blocks[1];
                Assert.That(block2.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block2.Content, Is.EqualTo("{{  }}"));

                var block3 = (TextBlock)blocks[2];
                Assert.That(block3.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block3.Content, Is.EqualTo("}"));
            }
        }

        [Test]
        public void TextBolckCase11()
        {
            var blocks = _target?.Tokenize("{{  '}}x");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (TextBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Text));
                Assert.That(block1.Content, Is.EqualTo("{{  '}}x"));
            }
        }

        [Test]
        public void TextWithVariable1()
        {
            var blocks = _target?.Tokenize("{{$}}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (VariableBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$"));
            }
        }

        [Test]
        public void TextWithVariable2()
        {
            var blocks = _target?.Tokenize("{{$a}}");

            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (VariableBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$a"));
                Assert.That(block1.Name, Is.EqualTo("a"));
            }
        }

        [Test]
        public void TextWithVariable3()
        {
            var blocks = _target?.Tokenize("{{ $a}}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (VariableBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$a"));
                Assert.That(block1.Name, Is.EqualTo("a"));
            }
        }

        [Test]
        public void TextWithVariable4()
        {
            var blocks = _target?.Tokenize("{{  $a  }}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (VariableBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Variable));
                Assert.That(block1.Content, Is.EqualTo("$a"));
                Assert.That(block1.Name, Is.EqualTo("a"));
            }
        }

        [Test]
        public void TextWithCode()
        {
            var blocks = _target?.Tokenize("{{ code }}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (CodeBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Code));
                Assert.That(block1.Content, Is.EqualTo("code"));
            }
        }

        [Test]
        public void TextWithValue1()
        {
            var blocks = _target?.Tokenize("{{ ' ' }}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (ValueBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Value));
                Assert.That(block1.Content, Is.EqualTo("' '"));
            }
        }

        [Test]
        public void TextWithValue2()
        {
            var blocks = _target?.Tokenize("{{ 'good' }}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(1));

                var block1 = (ValueBlock)blocks[0];
                Assert.That(block1.Type, Is.EqualTo(BlockTypeKind.Value));
                Assert.That(block1.Content, Is.EqualTo("good"));
            }
        }

        [Test]
        public void TextWithCodeBlock1()
        {
            var blocks = _target?.Tokenize("}}{{{ {$a}}}}");
            //}}{{{ {$a}}}} {{b}}x}}
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(3));

                Assert.That(blocks[0].Content, Is.EqualTo("}}{"));
                Assert.That(blocks[1].Content, Is.EqualTo("{$a"));
                Assert.That(blocks[2].Content, Is.EqualTo("}}"));
            }
        }

        [Test]
        public void TextWithCodeBlock2()
        {
            var blocks = _target?.Tokenize("//}}{{{ {$a}}}} {{b}}x}}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(5));

                Assert.That(blocks[0].Content, Is.EqualTo("//}}{"));
                Assert.That(blocks[1].Content, Is.EqualTo("{$a"));
                Assert.That(blocks[2].Content, Is.EqualTo("}} "));
                Assert.That(blocks[3].Content, Is.EqualTo("b")); 
                Assert.That(blocks[4].Content, Is.EqualTo("x}}"));
            }
        }

        [Test]
        public void TextWithCodeBlock3()
        {
            var blocks = _target?.Tokenize("{{ not='valid' }}");
            if (blocks != null)
            {
                Assert.That(blocks.Count, Is.EqualTo(5));

                Assert.That(blocks[0].Content, Is.EqualTo("//}}{"));
                Assert.That(blocks[1].Content, Is.EqualTo("{$a"));
                Assert.That(blocks[2].Content, Is.EqualTo("}} "));
                Assert.That(blocks[3].Content, Is.EqualTo("b"));
                Assert.That(blocks[4].Content, Is.EqualTo("x}}"));
            }
        }
    }
}


//    var ex = Assert.Throws<SKException>(() =>
//    {
//        // Act
//        this._target.Tokenize(template);
//    });
//    Assert.Equal(ex.Message, "Code tokenizer returned an incorrect first token type NamedArg");
//}
//}
