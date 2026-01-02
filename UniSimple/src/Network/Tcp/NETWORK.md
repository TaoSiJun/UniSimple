
TCP 发送逻辑：
```csharp
// 1. 序列化
byte[] body = message.ToByteArray();

// 2. 拼装包
byte[] packet = new byte[2 + body.Length];

// 3. 写入ID
byte[] idBytes = BitConverter.GetBytes((ushort)msgId);

// 4. packet 写入 id
Array.Copy(idBytes, 0, packet, 0, 2);

// 5. packet 写入 body
Array.Copy(body, 0, packet, 2, body.Length);
```

一个完整的、工业级的消息包通常是这样的： 

**`[ 长度 (4字节) ] + [ ID (2字节) ] + [ Body内容 ]`**

最终在网络上传输的 `packet` 结构如下：

| 字节索引 | 0 | 1 | 2 | 3 | 4 | ... | n |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **内容** | **ID(高8位)** | **ID(低8位)** | **数据内容...** | **数据内容...** | **数据内容...** | **...** | **数据结束** |
| **用途** | <---- 消息标识 ----> | <----------------------- 真正的数据 -----------------------> | | | | | |