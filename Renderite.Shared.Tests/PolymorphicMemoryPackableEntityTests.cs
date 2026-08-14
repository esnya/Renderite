namespace Renderite.Shared.Tests;

[TestClass]
public class PolymorphicMemoryPackableEntityTests
{
    [TestMethod]
    public void ReadPolymorphicList_GenericToTouch_ReturnsOldTypeAndBorrowsNewType()
    {
        var existing = new GenericControllerState();

        var result = DecodeReplacement(existing, new TouchControllerState());

        Assert.IsInstanceOfType<TouchControllerState>(result.Decoded[0]);
        Assert.AreSame(existing, result.Pool.ReturnedInstances.Single());
        Assert.AreEqual(1, result.Pool.ReturnCount<GenericControllerState>());
        Assert.AreEqual(0, result.Pool.ReturnCount<TouchControllerState>());
        Assert.AreEqual(1, result.Pool.BorrowCount<TouchControllerState>());
        Assert.AreEqual(0, result.UnpackerRemainingData);
    }

    [TestMethod]
    public void ReadPolymorphicList_TouchToGeneric_ReturnsOldTypeAndBorrowsNewType()
    {
        var existing = new TouchControllerState();

        var result = DecodeReplacement(existing, new GenericControllerState());

        Assert.IsInstanceOfType<GenericControllerState>(result.Decoded[0]);
        Assert.AreSame(existing, result.Pool.ReturnedInstances.Single());
        Assert.AreEqual(1, result.Pool.ReturnCount<TouchControllerState>());
        Assert.AreEqual(0, result.Pool.ReturnCount<GenericControllerState>());
        Assert.AreEqual(1, result.Pool.BorrowCount<GenericControllerState>());
        Assert.AreEqual(0, result.UnpackerRemainingData);
    }

    [TestMethod]
    public void ReadPolymorphicList_SameType_ReusesExistingInstance()
    {
        var existing = new TouchControllerState();

        var result = DecodeReplacement(existing, new TouchControllerState());

        Assert.AreSame(existing, result.Decoded[0]);
        Assert.AreEqual(0, result.Pool.ReturnedInstances.Count);
        Assert.AreEqual(0, result.Pool.TotalBorrowCount);
        Assert.AreEqual(0, result.UnpackerRemainingData);
    }

    private static DecodeResult DecodeReplacement(VR_ControllerState existing, VR_ControllerState replacement)
    {
        var buffer = new byte[4096];
        var packer = new MemoryPacker(buffer);
        packer.WritePolymorphicList(new List<VR_ControllerState> { replacement });
        var writtenLength = packer.ComputeLength(buffer);

        var pool = new TrackingPool();
        var decoded = new List<VR_ControllerState> { existing };
        var unpacker = new MemoryUnpacker(buffer.AsSpan(0, writtenLength), pool);
        unpacker.ReadPolymorphicList(ref decoded);

        return new DecodeResult(decoded, pool, unpacker.RemainingData);
    }

    private sealed record DecodeResult(
        List<VR_ControllerState> Decoded,
        TrackingPool Pool,
        int UnpackerRemainingData);

    private sealed class TrackingPool : IMemoryPackerEntityPool
    {
        private readonly Dictionary<Type, int> _borrowCounts = new();
        private readonly Dictionary<Type, int> _returnCounts = new();

        public List<IMemoryPackable> ReturnedInstances { get; } = new();

        public int TotalBorrowCount => _borrowCounts.Values.Sum();

        public T Borrow<T>() where T : class, IMemoryPackable, new()
        {
            Increment(_borrowCounts, typeof(T));
            return new T();
        }

        public void Return<T>(T value) where T : class, IMemoryPackable, new()
        {
            Increment(_returnCounts, typeof(T));
            ReturnedInstances.Add(value);
        }

        public int BorrowCount<T>() where T : class, IMemoryPackable, new() =>
            _borrowCounts.GetValueOrDefault(typeof(T));

        public int ReturnCount<T>() where T : class, IMemoryPackable, new() =>
            _returnCounts.GetValueOrDefault(typeof(T));

        private static void Increment(Dictionary<Type, int> counts, Type type)
        {
            counts[type] = counts.GetValueOrDefault(type) + 1;
        }
    }
}
