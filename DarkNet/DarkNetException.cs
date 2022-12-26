using System;

namespace Dark.Net;

internal class DarkNetException: Exception {

    public DarkNetException(string? message): base(message) { }

    public DarkNetException(string? message, Exception? innerException): base(message, innerException) { }

    internal class LifecycleException: DarkNetException {

        public LifecycleException(string? message): base(message) { }

    }

}