using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;


namespace GetInstalledPrograms.App_Class
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

    #region 加解密參考
    public static class AsymmetricRSAEncryption

    {

        //The key size to use
        private const int _EncryptionKeySize = 1024;

        //The buffer size to encrypt per set
        private const int _EncryptionBufferSize = 117;

        // The buffer size to decrypt per set
        private const int _DecryptionBufferSize = 128;

        /// <summary>
        /// Generate a new Asymmetric key and return the public key
        /// </summary>
        /// <param name="keyFilePath">The full path to the file where to store the private key</param>
        /// <returns>The public key in string XLM format</returns>
        public static string GenerateKey(string keyFilePath)
        {
            if (keyFilePath == null)
                throw new ArgumentNullException("Invalid key file path!", "keyFilePath");

            //No need to specify the method because RSA is the only true asymmetric algorithm
            RSACryptoServiceProvider asm = new RSACryptoServiceProvider(_EncryptionBufferSize);

            //Generate the private key, true indicates the private key
            string privateKey = asm.ToXmlString(true);

            byte[] privateKeyEncoded = Encoding.UTF8.GetBytes(privateKey);

            //Protect the key with DPAPI
            byte[] privateKeyEncrypted = ProtectedData.Protect(privateKeyEncoded, null, DataProtectionScope.LocalMachine);

            //Write the encrypted key to the specified file
            using (FileStream fs = new FileStream(keyFilePath, FileMode.Create))
            {
                fs.Write(privateKeyEncrypted, 0, privateKeyEncrypted.Length);
                fs.Flush();
            }

            //Return the public key
            return asm.ToXmlString(false);

        }

        /// <summary>
        /// Retrieve the private key
        /// </summary>
        /// <param name="keyFilePath">The private keys full file path</param>
        /// <returns>The private key in XML string format</returns>
        private static RSACryptoServiceProvider RetrieveKey(string keyFilePath)
        {
            //Read the private key from the specified file path
            byte[] privateKeyEncrypted;

            using (FileStream fs = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                privateKeyEncrypted = new byte[fs.Length];
                fs.Read(privateKeyEncrypted, 0, (int)fs.Length);
            }

            //Decrypt the private key with DPAPI
            byte[] privateKeyEncoded = ProtectedData.Unprotect(privateKeyEncrypted, null, DataProtectionScope.LocalMachine);

            //Unencode the private key
            string privateKey = Encoding.UTF8.GetString(privateKeyEncoded);

            //Return the private key

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(_EncryptionBufferSize);
            rsa.FromXmlString(privateKey);
            return rsa;
        }

        /// <summary>
        /// Encrypt the data using RSA
        /// </summary>
        /// <param name="data">The string data to encrypt</param>
        /// <param name="publicKey">The public key in XML format to encrypt with</param>
        /// <returns>An array of bytes containing the encrypted data</returns>
        public static byte[] EncryptData(string data, string publicKey)
        {
            //Create a new asymmetric algorithm object using the supplied public key
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            rsa.FromXmlString(publicKey);

            //Encode the data
            byte[] dataEncoded = Encoding.UTF8.GetBytes(data);

            //Store every chunk of encrypted data during the encryption process in a memory stream
            using (MemoryStream ms = new MemoryStream())
            {
                //Create a buffer with the maximum allowed size
                byte[] buffer = new byte[_EncryptionBufferSize];
                int pos = 0;
                int copyLength = buffer.Length;

                while (true)
                {
                    //Check if the bytes left to read is smaller than the buffer size, then limit the buffer size to the number of bytes left

                    if (pos + copyLength > dataEncoded.Length)
                        copyLength = dataEncoded.Length - pos;

                    //Create a new buffer that has the correct size
                    buffer = new byte[copyLength];

                    //Copy as many bytes as the algorithm can handle at a time, iterate until the whole input array is encoded
                    Array.Copy(dataEncoded, pos, buffer, 0, copyLength);

                    //Start from here in next iteration

                    pos += copyLength;

                    //Encrypt the data using the public key and add it to the memory buffer
                    //_DecryptionBufferSize is the size of the encrypted data
                    ms.Write(rsa.Encrypt(buffer, false), 0, _DecryptionBufferSize);

                    //Clear the content of the buffer, otherwise we could end up copying the same data during the last iteration
                    Array.Clear(buffer, 0, copyLength);

                    //Check if we have reached the end, then exit
                    if (pos >= dataEncoded.Length)
                        break;
                }

                //Return the encrypted data
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decrypt the data using the private key
        /// </summary>
        /// <param name="data">The data to decrypt</param>
        /// <param name="keyFilePath">The full path to the private key file</param>
        /// <returns>The decrypted data in string format</returns>
        /// <exception cref="System.Security.CryptoGraphicException">Unable to decrypt the data, probably corrupted</exception>
        public static string DecryptData(byte[] data, string keyFilePath)
        {
            try
            {
                //Retrieve the private key from the key file path supplied
                RSACryptoServiceProvider rsa = RetrieveKey(keyFilePath);

                //Initialize a memory stream to hold the encrypted chunks of data, use the same size as the encrypted data (however, will be slightly smaller)
                using (MemoryStream ms = new MemoryStream(data.Length))
                {
                    //The buffer that will hold the encrypted chunks
                    byte[] buffer = new byte[_DecryptionBufferSize];
                    int pos = 0;
                    int copyLength = buffer.Length;

                    while (true)
                    {
                        //Copy a chunk of encrypted data / iteration
                        Array.Copy(data, pos, buffer, 0, copyLength);

                        //Set the next start position
                        pos += copyLength;

                        //Decrypt the data using the private key
                        //We need to store the decrypted data temporarily because we don't know the size of it; 
                        //unlike with encryption where we know the size is 128 bytes. The only thing we know is that it's between 1-117 bytes
                        byte[] resp = rsa.Decrypt(buffer, false);
                        ms.Write(resp, 0, resp.Length);

                        //Cleat the buffers
                        Array.Clear(resp, 0, resp.Length);
                        Array.Clear(buffer, 0, copyLength);

                        //Are we ready to exit?
                        if (pos >= data.Length) break;
                    }

                    //Return the decoded data
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch (CryptographicException ce) //The data is probably corrupted 
            {
                Console.WriteLine(ce.Message);
                return Encoding.UTF8.GetString(data);
            }
        }
    }
    #endregion
}







