namespace BulkyMerge
{
    public class DynamicTypeConvert : ITypeConverter
    {
        private readonly Func<object, object> _callback;
        public DynamicTypeConvert(Func<object, object> callback)
        {
            _callback = callback;
        }

        public object Convert(object value)
        {
            return _callback(value);
        }
    }
}
