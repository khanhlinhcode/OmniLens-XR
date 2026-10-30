using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OmniLensXR.S07
{
    public enum WorkspaceRepositoryError
    {
        None = 0,
        Missing,
        InvalidRoot,
        InvalidKey,
        InvalidEnvelope,
        AuthenticationFailed,
        InvalidLayout,
        IoFailure,
    }

    public interface IWorkspaceRepository
    {
        bool TrySave(WorkspaceLayoutData data, out WorkspaceRepositoryError error);
        bool TryLoad(out WorkspaceLayoutData data, out WorkspaceRepositoryError error);
        bool TryReset(out WorkspaceRepositoryError error);
    }

    public sealed class SandboxWorkspaceRepository : IWorkspaceRepository
    {
        public const string LayoutFileName = "workspace-v2.bin";
        public const string KeyFileName = "workspace-v2.key";
        private const int KeyLength = 64;
        private readonly string _root;

        public SandboxWorkspaceRepository(string sandboxRoot)
        {
            if (
                string.IsNullOrWhiteSpace(sandboxRoot)
                || !Path.IsPathRooted(sandboxRoot)
            )
            {
                throw new ArgumentException(
                    "An absolute sandbox root is required.",
                    nameof(sandboxRoot)
                );
            }

            _root = Path.GetFullPath(sandboxRoot);
            if (
                string.Equals(
                    _root,
                    Path.GetPathRoot(_root),
                    StringComparison.Ordinal
                )
            )
            {
                throw new ArgumentException(
                    "The filesystem root cannot be used as a workspace sandbox.",
                    nameof(sandboxRoot)
                );
            }
        }

        public bool TrySave(
            WorkspaceLayoutData data,
            out WorkspaceRepositoryError error
        )
        {
            error = WorkspaceRepositoryError.None;
            if (
                !WorkspaceLayoutCodec.TrySerialize(
                    data,
                    out string json,
                    out WorkspaceLayoutError layoutError
                )
            )
            {
                error = layoutError == WorkspaceLayoutError.None
                    ? WorkspaceRepositoryError.IoFailure
                    : WorkspaceRepositoryError.InvalidLayout;
                return false;
            }

            try
            {
                Directory.CreateDirectory(_root);
                byte[] key = LoadOrCreateKey();
                byte[] envelope = WorkspaceEnvelope.Encrypt(
                    Encoding.UTF8.GetBytes(json),
                    key
                );
                WriteAtomically(LayoutPath, envelope);
                return true;
            }
            catch (CryptographicException)
            {
                error = WorkspaceRepositoryError.InvalidKey;
                return false;
            }
            catch (IOException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
        }

        public bool TryLoad(
            out WorkspaceLayoutData data,
            out WorkspaceRepositoryError error
        )
        {
            data = null;
            error = WorkspaceRepositoryError.None;
            try
            {
                if (!File.Exists(LayoutPath))
                {
                    error = WorkspaceRepositoryError.Missing;
                    return false;
                }

                if (!File.Exists(KeyPath))
                {
                    error = WorkspaceRepositoryError.InvalidKey;
                    return false;
                }

                byte[] key = File.ReadAllBytes(KeyPath);
                if (key.Length != KeyLength)
                {
                    error = WorkspaceRepositoryError.InvalidKey;
                    return false;
                }

                byte[] envelope = File.ReadAllBytes(LayoutPath);
                if (
                    !WorkspaceEnvelope.TryDecrypt(
                        envelope,
                        key,
                        out byte[] plaintext,
                        out error
                    )
                )
                {
                    return false;
                }

                string json = Encoding.UTF8.GetString(plaintext);
                if (
                    !WorkspaceLayoutCodec.TryDeserialize(
                        json,
                        out data,
                        out WorkspaceLayoutError layoutError
                    )
                )
                {
                    error = layoutError == WorkspaceLayoutError.None
                        ? WorkspaceRepositoryError.InvalidEnvelope
                        : WorkspaceRepositoryError.InvalidLayout;
                    return false;
                }

                return true;
            }
            catch (IOException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
        }

        public bool TryReset(out WorkspaceRepositoryError error)
        {
            error = WorkspaceRepositoryError.None;
            try
            {
                if (File.Exists(LayoutPath))
                {
                    File.Delete(LayoutPath);
                }

                string staged = LayoutPath + ".new";
                if (File.Exists(staged))
                {
                    File.Delete(staged);
                }

                return true;
            }
            catch (IOException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                error = WorkspaceRepositoryError.IoFailure;
                return false;
            }
        }

        private string LayoutPath => Path.Combine(_root, LayoutFileName);

        private string KeyPath => Path.Combine(_root, KeyFileName);

        private byte[] LoadOrCreateKey()
        {
            if (File.Exists(KeyPath))
            {
                byte[] existing = File.ReadAllBytes(KeyPath);
                if (existing.Length != KeyLength)
                {
                    throw new CryptographicException("Invalid workspace key length.");
                }

                return existing;
            }

            byte[] key = new byte[KeyLength];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
            }

            WriteAtomically(KeyPath, key);
            return key;
        }

        private static void WriteAtomically(string destination, byte[] bytes)
        {
            string staged = destination + ".new";
            File.WriteAllBytes(staged, bytes);
            if (File.Exists(destination))
            {
                string backup = destination + ".bak";
                File.Replace(staged, destination, backup);
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }
            }
            else
            {
                File.Move(staged, destination);
            }
        }
    }

    public static class WorkspaceEnvelope
    {
        private static readonly byte[] Magic = { 0x4f, 0x4c, 0x53, 0x37 };
        private const byte FormatVersion = 1;
        private const int IvLength = 16;
        private const int MacLength = 32;
        private const int MaximumCiphertextLength = 128 * 1024;

        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            ValidateInputs(plaintext, key);
            byte[] iv = new byte[IvLength];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(iv);
            }

            byte[] ciphertext;
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Slice(key, 0, 32);
                aes.IV = iv;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    ciphertext = encryptor.TransformFinalBlock(
                        plaintext,
                        0,
                        plaintext.Length
                    );
                }
            }

            byte[] authenticated = BuildAuthenticatedBytes(iv, ciphertext);
            byte[] mac;
            using (HMACSHA256 hmac = new HMACSHA256(Slice(key, 32, 32)))
            {
                mac = hmac.ComputeHash(authenticated);
            }

            byte[] envelope = new byte[authenticated.Length + mac.Length];
            Buffer.BlockCopy(authenticated, 0, envelope, 0, authenticated.Length);
            Buffer.BlockCopy(mac, 0, envelope, authenticated.Length, mac.Length);
            return envelope;
        }

        public static bool TryDecrypt(
            byte[] envelope,
            byte[] key,
            out byte[] plaintext,
            out WorkspaceRepositoryError error
        )
        {
            plaintext = null;
            error = WorkspaceRepositoryError.None;
            if (key == null || key.Length != 64)
            {
                error = WorkspaceRepositoryError.InvalidKey;
                return false;
            }

            int headerLength = Magic.Length + 1 + sizeof(int) + IvLength;
            if (envelope == null || envelope.Length < headerLength + 16 + MacLength)
            {
                error = WorkspaceRepositoryError.InvalidEnvelope;
                return false;
            }

            for (int index = 0; index < Magic.Length; index += 1)
            {
                if (envelope[index] != Magic[index])
                {
                    error = WorkspaceRepositoryError.InvalidEnvelope;
                    return false;
                }
            }

            if (envelope[Magic.Length] != FormatVersion)
            {
                error = WorkspaceRepositoryError.InvalidEnvelope;
                return false;
            }

            int lengthOffset = Magic.Length + 1;
            int ciphertextLength = BitConverter.ToInt32(envelope, lengthOffset);
            if (
                ciphertextLength <= 0
                || ciphertextLength > MaximumCiphertextLength
                || ciphertextLength % 16 != 0
                || envelope.Length != headerLength + ciphertextLength + MacLength
            )
            {
                error = WorkspaceRepositoryError.InvalidEnvelope;
                return false;
            }

            int authenticatedLength = envelope.Length - MacLength;
            byte[] expectedMac;
            using (HMACSHA256 hmac = new HMACSHA256(Slice(key, 32, 32)))
            {
                expectedMac = hmac.ComputeHash(envelope, 0, authenticatedLength);
            }

            if (!FixedTimeEquals(expectedMac, envelope, authenticatedLength))
            {
                error = WorkspaceRepositoryError.AuthenticationFailed;
                return false;
            }

            byte[] iv = Slice(envelope, Magic.Length + 1 + sizeof(int), IvLength);
            byte[] ciphertext = Slice(envelope, headerLength, ciphertextLength);
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = Slice(key, 0, 32);
                    aes.IV = iv;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        plaintext = decryptor.TransformFinalBlock(
                            ciphertext,
                            0,
                            ciphertext.Length
                        );
                    }
                }

                return true;
            }
            catch (CryptographicException)
            {
                error = WorkspaceRepositoryError.InvalidEnvelope;
                plaintext = null;
                return false;
            }
        }

        private static byte[] BuildAuthenticatedBytes(byte[] iv, byte[] ciphertext)
        {
            byte[] bytes = new byte[Magic.Length + 1 + sizeof(int) + iv.Length + ciphertext.Length];
            Buffer.BlockCopy(Magic, 0, bytes, 0, Magic.Length);
            bytes[Magic.Length] = FormatVersion;
            Buffer.BlockCopy(
                BitConverter.GetBytes(ciphertext.Length),
                0,
                bytes,
                Magic.Length + 1,
                sizeof(int)
            );
            Buffer.BlockCopy(iv, 0, bytes, Magic.Length + 1 + sizeof(int), iv.Length);
            Buffer.BlockCopy(
                ciphertext,
                0,
                bytes,
                Magic.Length + 1 + sizeof(int) + iv.Length,
                ciphertext.Length
            );
            return bytes;
        }

        private static void ValidateInputs(byte[] plaintext, byte[] key)
        {
            if (plaintext == null || plaintext.Length == 0)
            {
                throw new ArgumentException("Plaintext is required.", nameof(plaintext));
            }

            if (key == null || key.Length != 64)
            {
                throw new ArgumentException("A 64-byte key is required.", nameof(key));
            }
        }

        private static byte[] Slice(byte[] input, int offset, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(input, offset, result, 0, length);
            return result;
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] actual, int offset)
        {
            if (expected == null || actual == null || actual.Length - offset != expected.Length)
            {
                return false;
            }

            int difference = 0;
            for (int index = 0; index < expected.Length; index += 1)
            {
                difference |= expected[index] ^ actual[offset + index];
            }

            return difference == 0;
        }
    }
}
