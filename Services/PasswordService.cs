using System.Security.Cryptography;
using System.Text;

namespace c1Soft_b4bProje.Services;

public class PasswordService
{
    // Şifreyi SHA256 ile hashler
    public string Hash(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] hashBytes = SHA256.HashData(passwordBytes);

        return Convert.ToHexString(hashBytes);
    }

    // Girilen şifreyi kayıtlı hash ile karşılaştırır
    public bool Verify(string password, string passwordHash)
    {
        string newHash = Hash(password);
        return newHash == passwordHash;
    }
}
