using System.Numerics;
using System.Security.Cryptography;
using System.Text;

public class Decryptor
{
    private const string keyFileName = "xriGM9Vwo1bXOkMVQPsnTw";
    public static void Main(string[] args)
    {
        if (args.Length < 2) {
            Console.WriteLine("<path/to/AssetBundle/directory> <AssetBundleType(iltan/miku)> [E(Encrypt)]");
            return;
        }
        string basePath = args[0];
        string assetName = args[1];
        bool isEncrypt = args.Length >= 3 && args[2] == "E";

        try
        {
            Stream keyFileStream = File.OpenRead(basePath + "/" + keyFileName);
            string originalFile = args[0] + "/" + assetName;
            string encryptedFile = args[0] + "/" + SimpleAes.AesEncrypt(assetName, keyFileStream);

            string password = Encoding.UTF8.GetString(getKeyFileBytes(keyFileStream, getKeyFileOffset(keyFileStream, assetName, 3), 32));
            byte[] salt = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(assetName));
            if (isEncrypt)
            {
                SeekableAes aesStream = new SeekableAes(File.Create(encryptedFile), password, salt);
                File.OpenRead(originalFile).CopyTo(aesStream);
            }
            else
            {
                SeekableAes aesStream = new SeekableAes(File.OpenRead(encryptedFile), password, salt);
                aesStream.CopyTo(File.Create(originalFile));
            }
        }
        catch (Exception e) {
            Console.WriteLine("Operation failed:\n" + e.ToString());
        }
    }

    public static int getKeyFileOffset(Stream fileStream, string plainText, byte charOffset)
    {
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        if (charOffset > 1)
        {
            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextBytes[i] += charOffset;
                if (plainText != "iltan" && (i & 1) == 0)
                    plainTextBytes[i] += 2;
            }
        }
        BigInteger offset = new BigInteger(MD5.Create().ComputeHash(plainTextBytes));
        return Math.Abs((int)(offset % fileStream.Length));
    }

    public static byte[] getKeyFileBytes(Stream fileStream, int offset, int size) {
        fileStream.Seek(offset, SeekOrigin.Begin);
        byte[] res = new byte[size];
        fileStream.Read(res, 0, size);
        return res;
    }
}

public class SimpleAes
{
    public static string AesEncrypt(string plainText, Stream keyFileStream)
    {
        Aes aes = Aes.Create();
        byte[] key = Decryptor.getKeyFileBytes(keyFileStream, Decryptor.getKeyFileOffset(keyFileStream, plainText, 1), 32);
        byte[] iv = Decryptor.getKeyFileBytes(keyFileStream, Decryptor.getKeyFileOffset(keyFileStream, plainText, 7), 16);
        ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
        string encrypted;
        using (MemoryStream msEncrypt = new MemoryStream())
        {
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
            }
            encrypted = Convert.ToBase64String(msEncrypt.ToArray()).Replace("/", "-").Replace("=", "");
        }
        return encrypted;
    }
}

public class SeekableAes : Stream
{
    private Stream baseStream;
    private AesManaged aesData;
    private ICryptoTransform encryptor;
    public bool autoDisposeBaseStream;

    public override bool CanRead
    {
        get
        {
            return baseStream.CanRead;
        }
    }

    public override bool CanSeek
    {
        get
        {
            return baseStream.CanSeek;
        }
    }

    public override bool CanWrite
    {
        get
        {
            return baseStream.CanWrite;
        }
    }

    public override long Length
    {
        get
        {
            return baseStream.Length;
        }
    }

    public override long Position
    {
        get
        {
            return baseStream.Position;
        }
        set
        {
            baseStream.Position = value;
        }
    }

    public SeekableAes(Stream rawStream, string password, byte[] salt)
    {
        baseStream = rawStream;
        autoDisposeBaseStream = true;
        Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(password, salt);
        aesData = new AesManaged();
        aesData.KeySize = 256;
        aesData.Mode = CipherMode.ECB;
        aesData.Padding = PaddingMode.None;
        aesData.Key = rfc.GetBytes(32);
        encryptor = aesData.CreateEncryptor();
    }

    private void cipher(byte[] buffer, int offset, int count, long streamPos)
    {
        int size = aesData.BlockSize / 8;
        byte[] srcblock = new byte[size];
        byte[] dstblock = new byte[size];
        var blockoff = streamPos % size;
        var blockidx = streamPos / size + 1;
        bool flag = false;
        while (offset < count) {
            if(!flag || blockoff % size == 0)
            {
                System.BitConverter.GetBytes(blockidx).CopyTo(srcblock, 0);
                encryptor.TransformBlock(srcblock, 0, size, dstblock, 0);
                blockidx++;
                if(flag) blockoff = 0;
                flag = true;
            }
            buffer[offset++] ^= dstblock[blockoff++];
        }
    }

    public override void Flush()
    {
        baseStream.Flush();
    }

    public override void SetLength(long value)
    {
        baseStream.SetLength(value);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return baseStream.Seek(offset, origin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var pos = Position;
        int res = baseStream.Read(buffer, offset, count);
        cipher(buffer, offset, count, pos);
        return res;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        cipher(buffer, offset, count, Position);
        baseStream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing)
        {
            encryptor.Dispose();
            aesData.Dispose();
            if(autoDisposeBaseStream) baseStream.Dispose();
        }
    }
}
