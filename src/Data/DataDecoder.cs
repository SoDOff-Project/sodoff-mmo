namespace sodoffmmo.Data;

internal static class DataDecoder {
    internal static DataWrapper DecodeNull(NetworkData data) => new DataWrapper(NetworkDataType.Null, null!);

    internal static DataWrapper DecodeBool(NetworkData data) => new DataWrapper(NetworkDataType.Bool, data.ReadBool());

    internal static DataWrapper DecodeByte(NetworkData data) => new DataWrapper(NetworkDataType.Byte, data.ReadByte());

    internal static DataWrapper DecodeShort(NetworkData data) => new DataWrapper(NetworkDataType.Short, data.ReadShort());

    internal static DataWrapper DecodeInt(NetworkData data) => new DataWrapper(NetworkDataType.Int, data.ReadInt());

    internal static DataWrapper DecodeLong(NetworkData data) => new DataWrapper(NetworkDataType.Long, data.ReadLong());

    internal static DataWrapper DecodeFloat(NetworkData data) => new DataWrapper(NetworkDataType.Float, data.ReadFloat());

    internal static DataWrapper DecodeDouble(NetworkData data) => new DataWrapper(NetworkDataType.Double, data.ReadDouble());

    internal static DataWrapper DecodeString(NetworkData data) => new DataWrapper(NetworkDataType.String, data.ReadString());

    internal static DataWrapper DecodeByteArray(NetworkData data) {
        int count = data.ReadInt();
        return new DataWrapper(NetworkDataType.ByteArray, data.ReadChunk(count));
    }

    internal static DataWrapper DecodeBoolArray(NetworkData data)
        => new DataWrapper(NetworkDataType.BoolArray, DecodeTypedArray(data, d => d.ReadBool()));

    internal static DataWrapper DecodeShortArray(NetworkData data)
        => new DataWrapper(NetworkDataType.ShortArray, DecodeTypedArray(data, d => d.ReadShort()));

    internal static DataWrapper DecodeIntArray(NetworkData data)
        => new DataWrapper(NetworkDataType.IntArray, DecodeTypedArray(data, d => d.ReadInt()));

    internal static DataWrapper DecodeLongArray(NetworkData data)
        => new DataWrapper(NetworkDataType.LongArray, DecodeTypedArray(data, d => d.ReadLong()));

    internal static DataWrapper DecodeFloatArray(NetworkData data)
        => new DataWrapper(NetworkDataType.FloatArray, DecodeTypedArray(data, d => d.ReadFloat()));

    internal static DataWrapper DecodeDoubleArray(NetworkData data)
        => new DataWrapper(NetworkDataType.DoubleArray, DecodeTypedArray(data, d => d.ReadDouble()));

    internal static DataWrapper DecodeStringArray(NetworkData data)
        => new DataWrapper(NetworkDataType.StringArray, DecodeTypedArray(data, d => d.ReadString()));

    private static T[] DecodeTypedArray<T>(NetworkData data, Func<NetworkData, T> readFunction) {
        short length = data.ReadShort();
        T[] arr = new T[length];
        for (short i = 0; i < length; i++)
            arr[i] = readFunction(data);
        return arr;
    }
}
