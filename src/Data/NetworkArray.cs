namespace sodoffmmo.Data;
public class NetworkArray {
    List<DataWrapper> arrData = new();

    public DataWrapper this[int i] {
        get { return arrData[i]; }
    }

    public int Length {
        get { return arrData.Count; }
    }

    public void Add(bool value) => AddObject(NetworkDataType.Bool, value);

    public void Add(byte value) => AddObject(NetworkDataType.Byte, value);

    public void Add(short value) => AddObject(NetworkDataType.Short, value);

    public void Add(int value) => AddObject(NetworkDataType.Int, value);

    public void Add(long value) => AddObject(NetworkDataType.Long, value);

    public void Add(float value) => AddObject(NetworkDataType.Float, value);

    public void Add(double value) => AddObject(NetworkDataType.Double, value);

    public void Add(string value) => AddObject(NetworkDataType.String, value);

    public void Add(bool[] value) => AddObject(NetworkDataType.BoolArray, value);

    public void Add(NetworkData value) => AddObject(NetworkDataType.ByteArray, value.Data);

    public void Add(short[] value) => AddObject(NetworkDataType.ShortArray, value);

    public void Add(int[] value) => AddObject(NetworkDataType.IntArray, value);

    public void Add(long[] value) => AddObject(NetworkDataType.LongArray, value);

    public void Add(float[] value) => AddObject(NetworkDataType.FloatArray, value);

    public void Add(double[] value) => AddObject(NetworkDataType.DoubleArray, value);

    public void Add(string[] value) => AddObject(NetworkDataType.StringArray, value);

    public void Add(NetworkArray value) => AddObject(NetworkDataType.NetworkArray, value);

    public void Add(NetworkObject value) => AddObject(NetworkDataType.NetworkObject, value);

    public void Add(DataWrapper dataWrapper) => arrData.Add(dataWrapper);

    private void AddObject(NetworkDataType dataType, object obj) => Add(new DataWrapper(dataType, obj));

    public T GetValue<T>(int index) {
        if (index >= arrData.Count)
            throw new IndexOutOfRangeException();
        return (T)arrData[index].Data;
    }

    public bool GetBool(int index) => GetValue<bool>(index);

    public byte GetByte(int index) => GetValue<byte>(index);

    public short GetShort(int index) => GetValue<short>(index);

    public int GetInt(int index) => GetValue<int>(index);

    public long GetLong(int index) => GetValue<long>(index);

    public float GetFloat(int index) => GetValue<float>(index);

    public double GetDouble(int index) => GetValue<double>(index);

    public string GetUtfString(int index) => GetValue<string>(index);

    public bool[] GetBoolArray(int index) => GetValue<bool[]>(index);

    public NetworkData GetNetworkData(int index) => GetValue<NetworkData>(index);

    public short[] GetShortArray(int index) => GetValue<short[]>(index);

    public int[] GetIntArray(int index) => GetValue<int[]>(index);

    public long[] GetLongArray(int index) => GetValue<long[]>(index);

    public float[] GetFloatArray(int index) => GetValue<float[]>(index);

    public double[] GetDoubleArray(int index) => GetValue<double[]>(index);

    public string[] GetStringArray(int index) => GetValue<string[]>(index);

    public NetworkArray GetNetworkArray(int index) => GetValue<NetworkArray>(index);

    public NetworkObject GetNetworkObject(int index) => GetValue<NetworkObject>(index);

    public bool Contains(object obj) {
        if (obj is NetworkObject || obj is NetworkArray)
            throw new Exception("Unsupported object type");
        for (int i = 0; i < arrData.Count; i++) {
            if (object.Equals(arrData[i], obj))
                return true;
        }
        return false;
    }

    public class NullClass {};
    public static NullClass NULL = new ();
    
    public void AddWithType<T>(T value) {
        if (typeof(T) == typeof(NullClass))
            AddWithTypeObject(NetworkDataType.Null, 0, null);
        else if (typeof(T) == typeof(bool))
            AddWithTypeObject(NetworkDataType.Bool, 1, value);
        else if (typeof(T) == typeof(int))
            AddWithTypeObject(NetworkDataType.Int, 2, value);
        else if (typeof(T) == typeof(double))
            AddWithTypeObject(NetworkDataType.Double, 3, value);
        else if (typeof(T) == typeof(float))
            AddWithTypeObject(NetworkDataType.Float, 3, value);
        else if (typeof(T) == typeof(string))
            AddWithTypeObject(NetworkDataType.String, 4, value);
        else
            throw new Exception("Unsupported type");
    }
    
    private void AddWithTypeObject(NetworkDataType dataType, byte typeId, object? obj) {
        Add(new DataWrapper(NetworkDataType.Byte, typeId));
        Add(new DataWrapper(dataType, obj));
    }

    public static NetworkArray Param<T>(string name, T value) {
        NetworkArray arr = new();
        arr.Add(name);
        arr.AddWithType(value);
        return arr;
    }
    public static NetworkArray VlElement<T>(string name, T value, bool isPrivate = false, bool isPersistent = false) {
        NetworkArray arr = new();
        arr.Add(name);
        arr.AddWithType(value);
        arr.Add(isPrivate);
        arr.Add(isPersistent);
        return arr;
    }
}
