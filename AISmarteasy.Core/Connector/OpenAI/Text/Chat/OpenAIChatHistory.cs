﻿using AISmarteasy.Core.Connector.OpenAI.Text.Chat;
using AISmarteasy.Core.Util;

namespace AISmarteasy.Core.Connector.OpenAI.Completion.Chat;

public class OpenAIChatHistory : ChatHistory
{
    public OpenAIChatHistory(string? systemMessage = null)
    {
        if (!systemMessage.IsNullOrWhitespace())
        {
            AddSystemMessage(systemMessage);
        }
    }
}