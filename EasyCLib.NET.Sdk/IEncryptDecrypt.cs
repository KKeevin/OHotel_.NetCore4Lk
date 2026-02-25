namespace EasyCLib.NET.Sdk
{
    public interface IEncryptDecrypt
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
