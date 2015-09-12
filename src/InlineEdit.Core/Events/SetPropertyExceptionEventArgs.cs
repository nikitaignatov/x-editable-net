using System;
using InlineEdit.Core.Commands;

namespace InlineEdit.Core.Events
{
    public class SetPropertyExceptionEventArgs<T>
    {
        public UpdateEntityCommand<T> Command { get; set; }
        public Exception Exception { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }
    }
}