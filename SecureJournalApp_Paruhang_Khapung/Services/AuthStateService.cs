namespace SecureJournalapp_Paruhang_Khapung.Services
{
    /// <summary>
    /// Global authentication state service to track app unlock status
    /// Singleton service that persists unlock state during the session
    /// </summary>
    public class AuthStateService
    {
        private bool _isUnlocked = false;

        /// <summary>
        /// Indicates whether the app is currently unlocked (PIN validated)
        /// </summary>
        public bool IsUnlocked => _isUnlocked;

        /// <summary>
        /// Event fired when unlock state changes
        /// </summary>
        public event Action? OnUnlockStateChanged;

        /// <summary>
        /// Unlocks the app after successful PIN validation
        /// </summary>
        public void Unlock()
        {
            _isUnlocked = true;
            OnUnlockStateChanged?.Invoke();
        }

        /// <summary>
        /// Locks the app (e.g., on logout or app restart)
        /// </summary>
        public void Lock()
        {
            _isUnlocked = false;
            OnUnlockStateChanged?.Invoke();
        }
    }
}

