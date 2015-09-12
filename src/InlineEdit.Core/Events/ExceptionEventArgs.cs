using System;
using InlineEdit.Core.Commands;

namespace InlineEdit.Core.Events
{
    public class ExceptionEventArgs<T>
    {
        public ExceptionEventArgs(UpdateEntityCommand<T> command, Exception exception, int version)
        {
            Command = command;
            Exception = exception;
            Version = version;
        }

        public UpdateEntityCommand<T> Command { get; set; }
        public Exception Exception { get; set; }
        public int Version { get; set; }
    }
}