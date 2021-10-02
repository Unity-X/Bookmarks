namespace UnityX.Bookmarks
{
    public abstract class Enableable
    {
        private bool _enabled = false;

        public void Enable()
        {
            if (_enabled)
                return;

            _enabled = true;
            OnEnable();
        }

        public void Disable()
        {
            if (!_enabled)
                return;

            _enabled = false;
            OnDisable();
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
    }
}