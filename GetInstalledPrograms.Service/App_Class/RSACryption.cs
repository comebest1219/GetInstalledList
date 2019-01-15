using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;


namespace App_Class
{

    public class RSACryption
    {

        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }

        public RSACryption(string publicKey = "", string privatecKey = "")
        {
            GenerateRSAKeys(publicKey, privatecKey);
        }
        //產生公鑰及私鑰
        private void GenerateRSAKeys(string publicKey, string privatecKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            PublicKey = (string.IsNullOrEmpty(publicKey)) ? rsa.ToXmlString(false) : publicKey;
            PrivateKey = (string.IsNullOrEmpty(privatecKey)) ? rsa.ToXmlString(true) : privatecKey;
        }

        //加密字串
        public string EncryptRsa(string content)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(PublicKey);
            try
            {
                //Encode the data
                byte[] dataEncoded = Encoding.UTF8.GetBytes(content);
                int bufferSize = (rsa.KeySize / 8) - 11;
                byte[] buffer = new byte[bufferSize];

                using (MemoryStream input = new MemoryStream(dataEncoded))
                using (MemoryStream output = new MemoryStream())
                {
                    while (true)
                    {
                        int readLine = input.Read(buffer, 0, bufferSize);
                        if (readLine <= 0) break;
                        byte[] temp = new byte[readLine];
                        Array.Copy(buffer, 0, temp, 0, readLine);
                        byte[] encrypt = rsa.Encrypt(temp, false);
                        output.Write(encrypt, 0, encrypt.Length);
                    }
                    return Convert.ToBase64String(output.ToArray());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("EncryptRsa" + e.Message); return string.Empty;
            }

        }

        //解密字串
        public string DecryptRsa(string encryptedContent)
        {
            if (string.IsNullOrEmpty(encryptedContent))
            {
                Console.WriteLine("欲解密資料錯誤，請確認!"); return string.Empty;
            }
            try
            {
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(PrivateKey);
                byte[] EncryptDada = Convert.FromBase64String(encryptedContent);

                int keySize = rsa.KeySize / 8;
                byte[] buffer = new byte[keySize];

                using (MemoryStream input = new MemoryStream(EncryptDada))
                using (MemoryStream output = new MemoryStream())
                {
                    while (true)
                    {
                        int readLine = input.Read(buffer, 0, keySize);
                        if (readLine <= 0) break;
                        byte[] temp = new byte[readLine];
                        Array.Copy(buffer, 0, temp, 0, readLine);
                        byte[] decrypt = rsa.Decrypt(temp, false);
                        output.Write(decrypt, 0, decrypt.Length);
                    }
                    return Encoding.UTF8.GetString(output.ToArray());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("DecryptRsa:" + e.Message);
                return string.Empty;
            }
        }
    }
}







