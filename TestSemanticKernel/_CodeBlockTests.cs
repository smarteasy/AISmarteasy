﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine.Prompt.Blocks;
using Moq;
using Xunit;

namespace SemanticKernel.Extensions.UnitTests.TemplateEngine.Prompt.Blocks;

public class CodeBlockTests
{
    private readonly Mock<IReadOnlySkillCollection> _skills;
    private readonly ILoggerFactory _logger = NullLoggerFactory.Instance;

    public CodeBlockTests()
    {
        this._skills = new Mock<IReadOnlySkillCollection>();
    }

    [Fact]
    public async Task ItThrowsIfAFunctionDoesntExistAsync()
    {
        // Arrange
        var context = new SKContext(skills: this._skills.Object, loggerFactory: this._logger);
        this._skills.Setup(x => x.TryGetFunction("functionName", out It.Ref<ISKFunction?>.IsAny)).Returns(false);
        var target = new CodeBlock("functionName", this._logger);

        // Act & Assert
        await Assert.ThrowsAsync<SKException>(() => target.RenderCodeAsync(context));
    }

    [Fact]
    public async Task ItThrowsIfAFunctionCallThrowsAsync()
    {
        // Arrange
        var context = new SKContext(skills: this._skills.Object, loggerFactory: this._logger);
        var function = new Mock<ISKFunction>();
        function
            .Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), It.IsAny<CompleteRequestSettings?>(), It.IsAny<CancellationToken>()))
            .Throws(new RuntimeWrappedException("error"));
        ISKFunction? outFunc = function.Object;
        this._skills.Setup(x => x.TryGetFunction("functionName", out outFunc)).Returns(true);
        this._skills.Setup(x => x.GetFunction("functionName")).Returns(function.Object);
        var target = new CodeBlock("functionName", this._logger);

        // Act & Assert
        await Assert.ThrowsAsync<RuntimeWrappedException>(() => target.RenderCodeAsync(context));
    }

    [Fact]
    public void ItHasTheCorrectType()
    {
        // Act
        var target = new CodeBlock("", NullLoggerFactory.Instance);

        // Assert
        Assert.Equal(BlockTypes.Code, target.Type);
    }

    [Fact]
    public void ItTrimsSpaces()
    {
        // Act + Assert
        Assert.Equal("aa", new CodeBlock("  aa  ", NullLoggerFactory.Instance).Content);
    }

    [Fact]
    public void ItChecksValidityOfInternalBlocks()
    {
        // Arrange
        var validBlock1 = new FunctionIdBlock("x");
        var validBlock2 = new ValBlock("''");
        var invalidBlock = new VarBlock("");

        // Act
        var codeBlock1 = new CodeBlock(new List<Block> { validBlock1, validBlock2 }, "", NullLoggerFactory.Instance);
        var codeBlock2 = new CodeBlock(new List<Block> { validBlock1, invalidBlock }, "", NullLoggerFactory.Instance);

        // Assert
        Assert.True(codeBlock1.IsValid(out _));
        Assert.False(codeBlock2.IsValid(out _));
    }

    [Fact]
    public void ItRequiresAValidFunctionCall()
    {
        // Arrange
        var funcId = new FunctionIdBlock("funcName");
        var valBlock = new ValBlock("'value'");
        var varBlock = new VarBlock("$var");
        var namedArgBlock = new NamedArgBlock("varName='foo'");

        // Act
        var codeBlock1 = new CodeBlock(new List<Block> { funcId, valBlock }, "", NullLoggerFactory.Instance);
        var codeBlock2 = new CodeBlock(new List<Block> { funcId, varBlock }, "", NullLoggerFactory.Instance);
        var codeBlock3 = new CodeBlock(new List<Block> { funcId, funcId }, "", NullLoggerFactory.Instance);
        var codeBlock4 = new CodeBlock(new List<Block> { funcId, varBlock, varBlock }, "", NullLoggerFactory.Instance);
        var codeBlock5 = new CodeBlock(new List<Block> { funcId, varBlock, namedArgBlock }, "", NullLoggerFactory.Instance);
        var codeBlock6 = new CodeBlock(new List<Block> { varBlock, valBlock }, "", NullLoggerFactory.Instance);
        var codeBlock7 = new CodeBlock(new List<Block> { namedArgBlock }, "", NullLoggerFactory.Instance);

        // Assert
        Assert.True(codeBlock1.IsValid(out _));
        Assert.True(codeBlock2.IsValid(out _));

        // Assert - Can't pass a function to a function
        Assert.False(codeBlock3.IsValid(out var errorMessage3));
        Assert.Equal(errorMessage3, "The first arg of a function must be a quoted string, variable or named argument");

        // Assert - Can't pass more than one unnamed param
        Assert.False(codeBlock4.IsValid(out var errorMessage4));
        Assert.Equal(errorMessage4, "Functions only support named arguments after the first argument. Argument 2 is not named.");

        // Assert - Can pass one unnamed param and named args
        Assert.True(codeBlock5.IsValid(out var errorMessage5));
        Assert.Empty(errorMessage5);

        // Assert - Can't use > 1 block if not a function call
        Assert.False(codeBlock6.IsValid(out var errorMessage6));
        Assert.Equal(errorMessage6, "Unexpected second token found: 'value'");

        // Assert - Can't use a named argument without a function block
        Assert.False(codeBlock7.IsValid(out var errorMessage7));
        Assert.Equal(errorMessage7, "Unexpected named argument found. Expected function name first.");
    }

    [Fact]
    public async Task ItRendersCodeBlockConsistingOfJustAVarBlock1Async()
    {
        // Arrange
        var variables = new ContextVariables { ["varName"] = "foo" };
        var context = new SKContext(variables);

        // Act
        var codeBlock = new CodeBlock("$varName", NullLoggerFactory.Instance);
        var result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal("foo", result);
    }

    [Fact]
    public async Task ItRendersCodeBlockConsistingOfJustAVarBlock2Async()
    {
        // Arrange
        var variables = new ContextVariables { ["varName"] = "bar" };
        var context = new SKContext(variables);
        var varBlock = new VarBlock("$varName");

        // Act
        var codeBlock = new CodeBlock(new List<Block> { varBlock }, "", NullLoggerFactory.Instance);
        var result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal("bar", result);
    }

    [Fact]
    public async Task ItRendersCodeBlockConsistingOfJustAValBlock1Async()
    {
        // Arrange
        var context = new SKContext();

        // Act
        var codeBlock = new CodeBlock("'ciao'", NullLoggerFactory.Instance);
        var result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal("ciao", result);
    }

    [Fact]
    public async Task ItRendersCodeBlockConsistingOfJustAValBlock2Async()
    {
        // Arrange
        var context = new SKContext();
        var valBlock = new ValBlock("'arrivederci'");

        // Act
        var codeBlock = new CodeBlock(new List<Block> { valBlock }, "", NullLoggerFactory.Instance);
        var result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal("arrivederci", result);
    }

    [Fact]
    public async Task ItInvokesFunctionCloningAllVariablesAsync()
    {
        // Arrange
        const string Func = "funcName";

        var variables = new ContextVariables { ["input"] = "zero", ["var1"] = "uno", ["var2"] = "due" };
        var context = new SKContext(variables, skills: this._skills.Object);
        var funcId = new FunctionIdBlock(Func);

        var canary0 = string.Empty;
        var canary1 = string.Empty;
        var canary2 = string.Empty;
        var function = new Mock<ISKFunction>();
        function
            .Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), It.IsAny<CompleteRequestSettings?>(), It.IsAny<CancellationToken>()))
            .Callback<SKContext, CompleteRequestSettings?, CancellationToken>((context, _, _) =>
            {
                canary0 = context!.Variables["input"];
                canary1 = context.Variables["var1"];
                canary2 = context.Variables["var2"];

                context.Variables["input"] = "overridden";
                context.Variables["var1"] = "overridden";
                context.Variables["var2"] = "overridden";
            })
            .ReturnsAsync((SKContext inputcontext, CompleteRequestSettings _, CancellationToken _) => inputcontext);

        ISKFunction? outFunc = function.Object;
        this._skills.Setup(x => x.TryGetFunction(Func, out outFunc)).Returns(true);
        this._skills.Setup(x => x.GetFunction(Func)).Returns(function.Object);

        // Act
        var codeBlock = new CodeBlock(new List<Block> { funcId }, "", NullLoggerFactory.Instance);
        string result = await codeBlock.RenderCodeAsync(context);

        // Assert - Values are received
        Assert.Equal("zero", canary0);
        Assert.Equal("uno", canary1);
        Assert.Equal("due", canary2);

        // Assert - Original context is intact
        Assert.Equal("zero", variables["input"]);
        Assert.Equal("uno", variables["var1"]);
        Assert.Equal("due", variables["var2"]);
    }

    [Fact]
    public async Task ItInvokesFunctionWithCustomVariableAsync()
    {
        // Arrange
        const string Func = "funcName";
        const string Var = "varName";
        const string VarValue = "varValue";

        var variables = new ContextVariables { [Var] = VarValue };
        var context = new SKContext(variables, skills: this._skills.Object);
        var funcId = new FunctionIdBlock(Func);
        var varBlock = new VarBlock($"${Var}");

        var canary = string.Empty;
        var function = new Mock<ISKFunction>();
        function
            .Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), It.IsAny<CompleteRequestSettings?>(), It.IsAny<CancellationToken>()))
            .Callback<SKContext, CompleteRequestSettings?, CancellationToken>((context, _, _) =>
            {
                canary = context!.Variables["input"];
            })
            .ReturnsAsync((SKContext inputcontext, CompleteRequestSettings _, CancellationToken _) => inputcontext);

        ISKFunction? outFunc = function.Object;
        this._skills.Setup(x => x.TryGetFunction(Func, out outFunc)).Returns(true);
        this._skills.Setup(x => x.GetFunction(Func)).Returns(function.Object);

        // Act
        var codeBlock = new CodeBlock(new List<Block> { funcId, varBlock }, "", NullLoggerFactory.Instance);
        string result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal(VarValue, result);
        Assert.Equal(VarValue, canary);
    }

    [Fact]
    public async Task ItInvokesFunctionWithCustomValueAsync()
    {
        // Arrange
        const string Func = "funcName";
        const string Value = "value";

        var context = new SKContext(skills: this._skills.Object);
        var funcId = new FunctionIdBlock(Func);
        var valBlock = new ValBlock($"'{Value}'");

        var canary = string.Empty;
        var function = new Mock<ISKFunction>();
        function
            .Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), It.IsAny<CompleteRequestSettings?>(), It.IsAny<CancellationToken>()))
            .Callback<SKContext, CompleteRequestSettings?, CancellationToken>((context, _, _) =>
            {
                canary = context!.Variables["input"];
            })
            .ReturnsAsync((SKContext inputcontext, CompleteRequestSettings _, CancellationToken _) => inputcontext);

        ISKFunction? outFunc = function.Object;
        this._skills.Setup(x => x.TryGetFunction(Func, out outFunc)).Returns(true);
        this._skills.Setup(x => x.GetFunction(Func)).Returns(function.Object);

        // Act
        var codeBlock = new CodeBlock(new List<Block> { funcId, valBlock }, "", NullLoggerFactory.Instance);
        string result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal(Value, result);
        Assert.Equal(Value, canary);
    }

    [Fact]
    public async Task ItInvokesFunctionWithNamedArgsAsync()
    {
        // Arrange
        const string Func = "funcName";
        const string Value = "value";
        const string FooValue = "bar";
        const string BobValue = "bob's value";
        var variables = new ContextVariables();
        variables.Set("bob", BobValue);
        variables.Set("input", Value);
        var context = new SKContext(variables: variables, skills: this._skills.Object);
        var funcId = new FunctionIdBlock(Func);
        var namedArgBlock1 = new NamedArgBlock($"foo='{FooValue}'");
        var namedArgBlock2 = new NamedArgBlock("baz=$bob");

        var foo = string.Empty;
        var baz = string.Empty;
        var function = new Mock<ISKFunction>();
        function
            .Setup(x => x.InvokeAsync(It.IsAny<SKContext>(), It.IsAny<CompleteRequestSettings?>(), It.IsAny<CancellationToken>()))
            .Callback<SKContext, CompleteRequestSettings?, CancellationToken>((context, _, _) =>
            {
                foo = context!.Variables["foo"];
                baz = context!.Variables["baz"];
            })
            .ReturnsAsync((SKContext inputcontext, CompleteRequestSettings _, CancellationToken _) => inputcontext);

        ISKFunction? outFunc = function.Object;
        this._skills.Setup(x => x.TryGetFunction(Func, out outFunc)).Returns(true);
        this._skills.Setup(x => x.GetFunction(Func)).Returns(function.Object);

        // Act
        var codeBlock = new CodeBlock(new List<Block> { funcId, namedArgBlock1, namedArgBlock2 }, "", NullLoggerFactory.Instance);
        string result = await codeBlock.RenderCodeAsync(context);

        // Assert
        Assert.Equal(FooValue, foo);
        Assert.Equal(BobValue, baz);
        Assert.Equal(Value, result);
    }
}
