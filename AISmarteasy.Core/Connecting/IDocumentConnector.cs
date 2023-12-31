﻿namespace AISmarteasy.Core.Connecting;

public interface IDocumentConnector
{
    public string ReadText(Stream stream);

    public void Initialize(Stream stream);

    public void AppendText(Stream stream, string text);
}
