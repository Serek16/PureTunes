using System;

namespace EmySoundProject.Exceptions;

public class DockerConnectionException : Exception
{
    public DockerConnectionException() { }

    public DockerConnectionException(string message) : base(message) { }

    public DockerConnectionException(string message, Exception inner) : base(message, inner) { }
}