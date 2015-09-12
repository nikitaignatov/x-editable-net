using System;
using System.Data;

namespace InlineEdit.Core.Commands
{
    public class UpdateEntityCommand<TIdentity>
    {
        public UpdateEntityCommand(TIdentity id, string value, string name, Type entityType, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            Id = id;
            Value = value;
            Name = name;
            EntityType = entityType;
            IsolationLevel = isolationLevel;
        }

        public TIdentity Id { get; }
        public string Value { get; }
        public string Name { get; }
        public Type EntityType { get; }
        public IsolationLevel IsolationLevel { get; }
    }
}