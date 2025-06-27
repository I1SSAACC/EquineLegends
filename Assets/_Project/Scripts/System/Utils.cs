using System.Security.Cryptography;
using System.Text;

public static class Utils
{
    public static string ComputeSHA512Hash(string input)
    {
        using SHA512 sha512 = SHA512.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = sha512.ComputeHash(bytes);
        StringBuilder sb = new();

        foreach (byte b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}