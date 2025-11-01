using System.Text;

namespace halloween.Networking;

internal class Packet : IDisposable
{
    private List<byte> bufferList = null;
    private byte[] bufferArray = null;
    private int readPos = 0;
    private bool disposed = false;

    public Packet()
    {
        bufferList = new List<byte>();
        readPos = 0;
    }

    public Packet(byte[] data)
    {
        bufferList = new List<byte>();
        readPos = 0;
        WriteBytes(data);
        bufferArray = bufferList.ToArray();
    }

    public byte[] GetByteArray()
    {
        bufferArray = bufferList.ToArray();
        return bufferArray;
    }

    public void WriteBytes(byte[] bytes)
    {
        bufferList.AddRange(bytes);
    }

    public void WriteInt(int value)
    {
        bufferList.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteFloat(float value)
    {
        bufferList.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteBool(bool value)
    {
        bufferList.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteString(string value)
    {
        WriteInt(value.Length);
        bufferList.AddRange(Encoding.ASCII.GetBytes(value));
    }

    public byte[] ReadBytes(int length, bool moveReadPos = true)
    {
        if (bufferList.Count > readPos)
        {
            byte[] value = bufferList.GetRange(readPos, length).ToArray();

            if (moveReadPos)
            {
                readPos += length;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public int ReadInt(bool moveReadPos = true)
    {
        if (bufferList.Count > readPos)
        {
            int value = BitConverter.ToInt32(bufferArray, readPos);

            if (moveReadPos)
            {
                readPos += 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public float ReadFloat(bool moveReadPos = true)
    {
        if (bufferList.Count > readPos)
        {
            float value = BitConverter.ToSingle(bufferArray, readPos);

            if (moveReadPos)
            {
                readPos += 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public bool ReadBoolean(bool moveReadPos = true)
    {
        if (bufferList.Count > readPos)
        {
            bool value = BitConverter.ToBoolean(bufferArray, readPos);

            if (moveReadPos)
            {
                readPos += 1;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public string Readint(bool moveReadPos = true)
    {
        if (bufferList.Count > readPos)
        {
            int length = BitConverter.ToInt32(bufferArray, readPos);
            string value = BitConverter.ToString(bufferArray, readPos + 4, length);

            if (moveReadPos)
            {
                readPos += length + 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!disposed)
        {
            if (disposing)
            {
                bufferList?.Clear();
                bufferList = null;
                bufferArray = null;
                readPos = 0;
            }
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}