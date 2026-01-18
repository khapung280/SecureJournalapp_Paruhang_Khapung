using System.Security.Cryptography;
using System.Text;

namespace SecureJournalapp_Paruhang_Khapung.Services
{
    public class SecurityService
    {
        // Hash PIN using SHA256
        public string HashPin(string pin)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = sha.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        // Verify PIN against stored hash
        public bool VerifyPin(string enteredPin, string storedHash)
        {
            var enteredHash = HashPin(enteredPin);
            return enteredHash == storedHash;
        }
    }
}
