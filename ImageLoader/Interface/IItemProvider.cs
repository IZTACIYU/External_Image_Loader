namespace ImageLoader
{
    internal interface IItemProvider<T>
    {
        public T GetItem(string Name);
    }
}
