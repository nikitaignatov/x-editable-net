using System.Collections.Generic;
using System.Data.Entity.Validation;
using InlineEdit.Core.Commands;

namespace InlineEdit.Core.Events
{
    public class DbEntityValidationExceptionEventArgs<T>
    {
        public DbEntityValidationExceptionEventArgs(UpdateEntityCommand<T> command, DbEntityValidationException exception, int version)
        {
            Command = command;
            Exception = exception;
            Version = version;
        }

        public UpdateEntityCommand<T> Command { get; set; }
        public DbEntityValidationException Exception { get; set; }
        public int Version { get; set; }
    }
}