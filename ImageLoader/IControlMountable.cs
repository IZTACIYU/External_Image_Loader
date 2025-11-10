namespace ImageLoader
{
    internal interface IControlMountable<T>
    {
        public void MountTo(T control);
    }
}
