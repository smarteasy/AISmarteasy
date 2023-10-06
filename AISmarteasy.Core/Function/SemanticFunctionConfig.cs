﻿using AISmarteasy.Core.Prompt;

namespace AISmarteasy.Core.Function;

public sealed class SemanticFunctionConfig
{
    public PromptTemplateConfig PromptTemplateConfig { get; }

    public IPromptTemplate PromptTemplate { get; }

    public SemanticFunctionConfig(
        PromptTemplateConfig config,
        IPromptTemplate template)
    {
        PromptTemplateConfig = config;
        PromptTemplate = template;
    }
}