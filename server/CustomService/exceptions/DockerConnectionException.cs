using System;

namespace EmyProject.CustomService.exceptions;

[Serializable]
public class DockerConnectionException : SystemException
{
    public DockerConnectionException(string message)
        : base(message)
    {
    }

    public DockerConnectionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}