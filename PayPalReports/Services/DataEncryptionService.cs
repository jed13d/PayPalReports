using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PayPalReports.Services
{
    internal class DataEncryptionService
    {
        private byte[] _entropy;
        private int _bytesWritten;

        private readonly string ENTROPY_FILE_PATH = "PayPalReports.data.dll";
        private readonly int ENTROPY_SIZE = 12;
        private readonly int TEMP_ENTROPY_SIZE = 16;
        private readonly byte[] HARD_ENTROPY_PIECES = { 0x61, 0xB3, 0xF0, 0x97 };

        public DataEncryptionService()
        {
            _entropy = new byte[ENTROPY_SIZE];
            LoadEntropy();
        }

        /// <summary>
        /// Retrieve the encrypted data from parameterized filePath
        /// </summary>
        /// <param name="filePath">Path to the file in which the data resides.</param>
        /// <returns>Decrypted data as a single string.</returns>
        public string RetrieveData(string filePath)
        {
            string returnData = "";

            try
            {
                // Open the file.
                FileStream fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                // Read from the stream and decrypt the data.
                byte[] decryptedData = DecryptDataFromStream(GetEntropy(), DataProtectionScope.CurrentUser, fStream, (int)fStream.Length);

                // Close the filestream
                fStream.Close();

                // Decrypt and locally store data ready for return
                returnData = UnicodeEncoding.ASCII.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while decrypting data for \"{filePath}\": {ex.Message}");
            }

            return returnData;
        }

        /// <summary>
        /// Encrypt and store the parameterized data into a file determined by the parameterized filePath.
        /// </summary>
        /// <param name="data">Data to encrypt and store</param>
        /// <param name="filePath">Path to the file in which the data will be stored.</param>
        /// <returns></returns>
        public bool StoreData(byte[] data, string filePath)
        {
            _bytesWritten = 0;

            try
            {
                // Open or create data file
                FileStream fStream = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                // encrypt data into file stream
                _bytesWritten = EncryptDataToStream(data, GetEntropy(), DataProtectionScope.CurrentUser, fStream);

                fStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"A error occurred while storing data into the datafile: {filePath}, error: {ex}");
            }

            return _bytesWritten > 0;
        }

        /// <summary>
        /// This is a test meant for the UI to display the status of the parameterized file.
        /// </summary>
        /// <param name="filePath">The path to the file in question</param>
        /// <returns></returns>
        public bool DataFileExists(string filePath)
        {
            bool returnValue = false;

            try
            {
                // Attempt to open the file.
                FileStream fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                // If the file opened, go ahead and close it.
                fStream.Close();
                returnValue = true;
            }
            catch (FileNotFoundException)
            {
                // The file couldn't be found, therefore it doesn't exist.
                returnValue = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"A error occurred while testing the existence for a datafile: {filePath}, error: {ex}");
            }

            return returnValue;
        }

        private static byte[] DecryptDataFromStream(byte[] entropy, DataProtectionScope scope, Stream s, int length)
        {
            // Some argument validation
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (length <= 0)
                throw new ArgumentException("The given length was 0.", nameof(length));
            if (entropy == null)
                throw new ArgumentNullException(nameof(entropy));
            if (entropy.Length <= 0)
                throw new ArgumentException("The entropy length was 0.", nameof(entropy));

            // Some variable initialization
            byte[] inBuffer = new byte[length];
            byte[] outBuffer;

            // Read the encrypted data from a stream.
            if (s.CanRead)
            {
                s.Read(inBuffer, 0, length);

                outBuffer = ProtectedData.Unprotect(inBuffer, entropy, scope);
            }
            else
            {
                throw new IOException("Could not read the stream.");
            }

            // Return the decrypted data
            return outBuffer;
        }

        private static int EncryptDataToStream(byte[] buffer, byte[] entropy, DataProtectionScope scope, Stream s)
        {
            // check buffer
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (buffer.Length <= 0)
            {
                throw new ArgumentException("The buffer length is 0.", nameof(buffer));
            }

            // check entropy
            if (entropy == null)
            {
                throw new ArgumentNullException(nameof(entropy));
            }
            if (entropy.Length <= 0)
            {
                throw new ArgumentException("The entropy length is 0.", nameof(entropy));
            }

            // check Stream
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            int length = 0;

            // encrypt the data and store the result in a new byte array, leaving the original unchanged.
            byte[] encryptedData = ProtectedData.Protect(buffer, entropy, scope);

            // write data to stream
            if (s.CanWrite && encryptedData != null)
            {
                s.Write(encryptedData, 0, encryptedData.Length);

                length = encryptedData.Length;
            }

            // return length written to the stream
            return length;
        }

        private void CreateEntropy()
        {
            // Generate random bytes for entropy
            _entropy = RandomNumberGenerator.GetBytes(ENTROPY_SIZE);
        }

        /**
         * This is only so secure, but keeps the whole entropy from being stored in a file.
         * */
        private byte[] GetEntropy()
        {
            byte[] entropy = new byte[TEMP_ENTROPY_SIZE];

            for (int i = 0, j = 0, h = 0, e = 0; i < TEMP_ENTROPY_SIZE; i++, j++)
            {
                if (j == 3)
                {
                    entropy[i] = HARD_ENTROPY_PIECES[h++];
                    j = -1;
                    continue;
                }

                entropy[i] = _entropy[e++];
            }

            return entropy;
        }

        /**
         * Attempt to load the entropy from a file, if that fails, refresh the entropy with a new one.
         * This will cause the existing encrypted data files to become inaccessible.
         * */
        private void LoadEntropy()
        {
            try
            {
                FileStream fStream = new FileStream(ENTROPY_FILE_PATH, FileMode.Open, FileAccess.Read);

                fStream.Read(_entropy, 0, ENTROPY_SIZE);

                fStream.Close();
            }
            catch (FileNotFoundException)
            {
                RefreshEntropy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading entropy: {ex.Message}");
            }
        }

        /**
         * Creates and stores a new entropy as needed.
         * */
        private void RefreshEntropy()
        {
            FileStream fStream = new(ENTROPY_FILE_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            CreateEntropy();

            fStream.Write(_entropy, 0, ENTROPY_SIZE);

            fStream.Close();
        }
    }
}
