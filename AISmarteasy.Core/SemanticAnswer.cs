﻿namespace AISmarteasy.Core;

public class SemanticAnswer
{
    public SemanticAnswer(string answer)
    {
        Text = answer;
    }

    public string Text { get; set; }
}
