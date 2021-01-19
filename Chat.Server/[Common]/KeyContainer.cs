namespace Chat.Server.Call
{
    using System;
    using System.Collections.Generic;

    public class KeyContainer 
    {
        #region Fields

        private readonly object _locked;
        private readonly HashSet<int> _taken;
        private readonly Queue<int> _release;

        private int _key;

        #endregion Fields

        #region Constructors

        public KeyContainer(int key = 1) 
        {
            _locked = new object();

            _taken = new HashSet<int>();
            _release = new Queue<int>();

            _key = key;
        }

        #endregion Constructors

        #region Methods

        public int Take() 
        {
            lock (_locked)
            {
                if (!_release.TryDequeue(out int key))
                {
                    key = _key++;
                }

                _taken.Add(key);

                return key;
            }
        }

        public void Release(int key)
        {
            lock (_locked)
            {
                if (!_taken.Remove(key))
                {
                    return;
                }
                
                _release.Enqueue(key);
            }
        }

        public bool HasReleased(int key)
        {
            return _release.Contains(key);
        }

        #endregion Methods
    }
}
