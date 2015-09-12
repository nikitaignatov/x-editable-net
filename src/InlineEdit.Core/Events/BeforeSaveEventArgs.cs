using System;
using System.Data.Entity;
using InlineEdit.Core.Commands;

namespace InlineEdit.Core.Events
{
    public class BeforeSaveEventArgs<T, TContext> : EventArgs where TContext : DbContext
    {
        public UpdateEntityCommand<T> Command { get; set; }
        public TContext Context { get; set; }
        public object Original { get; set; }
        public int Version { get; set; }
    }
}