using System;

namespace InlineEdit.Core.Events
{
    public class UpdatedEntityEvent<TIdentity>
    {
        public UpdatedEntityEvent(TIdentity id, string value, string name, Type entityType)
        {
            Id = id;
            Value = value;
            Name = name;
            EntityType = entityType;
        }

        public TIdentity Id { get; private set; }
        public string Value { get; private set; }
        public string Name { get; private set; }
        public Type EntityType { get; private set; }
    }
}