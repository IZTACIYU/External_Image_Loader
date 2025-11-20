using System.Diagnostics.CodeAnalysis;

namespace ImageLoader
{
    public partial class ActionManager
    {
        private event Action<Color>? RefreshTheme;
        public void SetStyle(Action<Color>? action) => RefreshTheme += action;
        public void GetStyle(Color color) => RefreshTheme?.Invoke(color);

        public void Destroy()
        {
            #region Action함수 memFree
            RefreshTheme = null;
            #endregion

            #region 싱글톤 객체 memFree
            _instance = null;
            #endregion
        }
    }


    public partial class ActionManager
    {
        private ActionManager() { }

        // 멀티스레드 환경 고려 싱글톤
        static private readonly object _lock = new object();
        static private ActionManager? _instance;
        static public ActionManager Instance()
        {
            if (_instance == null)
                lock(_lock)
                    if (_instance == null)
                        _instance = new ActionManager();
            return _instance;
        }
    }

}
