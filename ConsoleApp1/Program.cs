using System.Buffers;
using MemoryPack;
using NATS.Client.Core;

var opts = NatsOpts.Default with { SerializerRegistry = new NatsMemoryPackContextSerializerRegistry()};
await using var nats = new NatsConnection(opts);

var subscription = Task.Run(async () =>
{
    await foreach (var msg in nats.SubscribeAsync<Request>("tst"))
    {
        await msg.ReplyAsync(new Response() { Status = "123" });
        break;
    }
});

var response = await nats.RequestAsync<Request, Response>("tst", new Request("123"));
Console.WriteLine("Reply received: {0}", response.Data?.Status);
await subscription;


[MemoryPackable(SerializeLayout.Explicit)]
public partial class Request(string id)
{
    [MemoryPackOrder(0)] public string Id { get; set; } = id;
}

[MemoryPackable(SerializeLayout.Explicit)]
public partial class Response
{
    [MemoryPackOrder(0)] public string Status { get; set; }
}
public sealed class NatsMemoryPackContextSerializerRegistry : INatsSerializerRegistry
{
    public INatsSerialize<T> GetSerializer<T>() => NatsMemoryPackContextSerializer<T>.Default;
    public INatsDeserialize<T> GetDeserializer<T>() => NatsMemoryPackContextSerializer<T>.Default;
}
public sealed class NatsMemoryPackContextSerializer<T> : INatsSerializer<T>
{
    public static readonly INatsSerializer<T> Default = new NatsMemoryPackContextSerializer<T>();
    
    public void Serialize(IBufferWriter<byte> bufferWriter, T value) => MemoryPackSerializer.Serialize(bufferWriter, value);

    public T? Deserialize(in ReadOnlySequence<byte> buffer) => MemoryPackSerializer.Deserialize<T>(buffer);
}