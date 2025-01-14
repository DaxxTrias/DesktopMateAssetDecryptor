using System.Security.Cryptography;

public class Decryptor
{
    private const string password = "sCHcfTxIpgO2tusgnlNqKVKsdopir0JH";
    public static void Main(string[] args)
    {
        if (args.Length < 2) {
            Console.WriteLine("<path/to/AssetBundle/directory> <AssetBundleType(iltan/miku)> [E(Encrypt)]");
            return;
        }
        bool isEncrypt = args.Length >= 3 && args[2] == "E";
        string key = args[1];
        string originalFile = args[0] + "/" + key;
        string encryptedFile = args[0] + "/" + SimpleAes.AesEncrypt(key);
        try
        {
            if (isEncrypt)
            {
                SeekableAes aesStream = new SeekableAes(File.Create(encryptedFile), password, System.Text.Encoding.UTF8.GetBytes(key + key));
                File.OpenRead(originalFile).CopyTo(aesStream);
            }
            else
            {
                SeekableAes aesStream = new SeekableAes(File.OpenRead(encryptedFile), password, System.Text.Encoding.UTF8.GetBytes(key + key));
                aesStream.CopyTo(File.Create(originalFile));
            }
        }
        catch (Exception e) {
            Console.WriteLine("Operation failed:\n" + e.ToString());
        }
    }
}

public class SimpleAes
{
    private const string fileKey = "YVe2SngRFQNCbPW67xrANOKMaDP8Qopn";
    private const string fileIv = "3RSFHrtWxi7d1eAP";

    public static string AesEncrypt(string plain_text)
    {
        Aes aes = Aes.Create();
        ICryptoTransform encryptor = aes.CreateEncryptor(System.Text.Encoding.UTF8.GetBytes(fileKey), System.Text.Encoding.UTF8.GetBytes(fileIv));
        string encrypted;
        using (MemoryStream msEncrypt = new MemoryStream())
        {
            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plain_text);
                }
            }
            encrypted = System.Convert.ToBase64String(msEncrypt.ToArray()).Replace('/', '-');
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
