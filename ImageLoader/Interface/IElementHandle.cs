namespace ImageLoader
{
    public interface IElementHandle<T>
    {
        public T GetElement(string name);
        public void SetElement(string name, T button);
        public void RemoveElement(string name);
    }
}
