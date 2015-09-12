namespace InlineEdit.Core.Model
{
    public class UpdatedValue<T>
    {
        private int p;

        public UpdatedValue(int p)
        {
            this.p = p;
        }

        public UpdatedValue(T original, T to)
        {
            From = original;
            To = to;
        }

        public T From { get; set; }
        public T To { get; set; }
    }
}