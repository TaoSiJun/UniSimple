using System;
using System.Text;

namespace UniSimple.Data;

public class StringOnlySerializer : IBinarySerializer
{
    public byte[] Serialize<T>(T obj)
    {
        if (obj is string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        throw new ArgumentException($"Only support string, not support {typeof(T).Name}");
    }

    public T Deserialize<T>(byte[] bytes)
    {
        if (typeof(T) != typeof(string))
        {
            throw new ArgumentException($"Only support string, not support {typeof(T).Name}");
        }

        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        return (T)(object)Encoding.UTF8.GetString(bytes);
    }
}