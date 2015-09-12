using System.ComponentModel;

namespace InlineEdit.Core.Model
{
    public interface IHaveVersion
    {
        [ReadOnly(true)]
        int Version { get; set; }
    }
}