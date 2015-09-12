using System;

namespace InlineEdit.Core.Commands
{
    public class RevertEntityCommand<TIdentity> : UpdateEntityCommand<TIdentity>
    {
        public RevertEntityCommand(TIdentity id, string value, string name, Type entityType, int version)
            : base(id, value, name, entityType)
        {
            Version = version;
        }

        public int Version { get; private set; }
    }
}