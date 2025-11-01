using System.Text;

namespace halloween.Networking;

internal class Packet : IDisposable
{
    private List<byte> _bufferList = null;
    private byte[] _bufferArray = null;
    private int _readPos = 0;
    private bool _disposed = false;

    public Packet()
    {
        _bufferList = new List<byte>();
        _readPos = 0;
    }

    public Packet(byte[] data)
    {
        _bufferList = new List<byte>();
        _readPos = 0;
        WriteBytes(data);
        _bufferArray = _bufferList.ToArray();
    }

    public byte[] GetByteArray()
    {
        _bufferArray = _bufferList.ToArray();
        return _bufferArray;
    }

    public void WriteBytes(byte[] bytes)
    {
        _bufferList.AddRange(bytes);
    }

    public void WriteInt(int value)
    {
        _bufferList.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteFloat(float value)
    {
        _bufferList.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteBool(bool value)
    {
        _bufferList.AddRange(BitConverter.GetBytes(value));
    }

    public void WriteString(string value)
    {
        WriteInt(value.Length);
        _bufferList.AddRange(Encoding.ASCII.GetBytes(value));
    }

    public byte[] ReadBytes(int length, bool move_ReadPos = true)
    {
        if (_bufferList.Count > _readPos)
        {
            byte[] value = _bufferList.GetRange(_readPos, length).ToArray();

            if (move_ReadPos)
            {
                _readPos += length;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public int ReadInt(bool move_ReadPos = true)
    {
        if (_bufferList.Count > _readPos)
        {
            int value = BitConverter.ToInt32(_bufferArray, _readPos);

            if (move_ReadPos)
            {
                _readPos += 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public float ReadFloat(bool move_ReadPos = true)
    {
        if (_bufferList.Count > _readPos)
        {
            float value = BitConverter.ToSingle(_bufferArray, _readPos);

            if (move_ReadPos)
            {
                _readPos += 4;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public bool ReadBoolean(bool move_ReadPos = true)
    {
        if (_bufferList.Count > _readPos)
        {
            bool value = BitConverter.ToBoolean(_bufferArray, _readPos);

            if (move_ReadPos)
            {
                _readPos += 1;
            }

            return value;
        }
        else
        {
            throw new Exception("Could not read the value");
        }
    }

    public string ReadString(bool move_ReadPos = true)
    {
        if (_bufferList.Count > _readPos)
        {
            int length = BitConverter.ToInt32(_bufferArray, _readPos);
            string value = BitConverter.ToString(_bufferArray, _readPos + 4, length);

            if (move_ReadPos)
            {
                _readPos += length + 4;
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
        if(!_disposed)
        {
            if (disposing)
            {
                _bufferList?.Clear();
                _bufferList = null;
                _bufferArray = null;
                _readPos = 0;
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}