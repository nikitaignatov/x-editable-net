using System;
using InlineEdit.Core.Commands;

namespace InlineEdit.Core.Events
{
    public class EntityUpdatedEventArgs<T> : EventArgs
    {
        public UpdateEntityCommand<T> Command { get; set; }
        public object Original { get; set; }
        public int Version { get; set; }
    }
}