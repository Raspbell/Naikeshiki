namespace Malen.ListPool
{
    public static class ListPool<T>
    {
        private static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> _pool
            = new System.Collections.Generic.Stack<System.Collections.Generic.List<T>>();

        public static System.Collections.Generic.List<T> Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            else
            {
                return new System.Collections.Generic.List<T>(8);
            }
        }

        public static void Release(System.Collections.Generic.List<T> list)
        {
            if (list == null)
            {
                return;
            }
            list.Clear();
            _pool.Push(list);
        }
    }
}