/*
using MessagePack;

public class MsgPackSerializerImpl : IBinarySerializer
{
    public byte[] Serialize<T>(T obj)
    {
        // 可以加入加密逻辑： Encrypt(MessagePackSerializer.Serialize(obj));
        return MessagePackSerializer.Serialize(obj);
    }
    
    public T Deserialize<T>(byte[] bytes)
    {
        // 可以加入解密逻辑： bytes = Decrypt(bytes);
        return MessagePackSerializer.Deserialize<T>(bytes);
    }
}
*/