using ComponentAce.Compression.Libs.zlib;

namespace sodoffmmo.Data;
public class NetworkPacket {

    byte header;
    byte[] data;
    bool compressed = false;

    public int Length {
        get {
            return data.Length;
        }
    }

    public byte[] SendData {
        get {
            NetworkData sendData = new();
            sendData.WriteValue(header);
            sendData.WriteValue((ushort)data.Length);
            sendData.WriteChunk(data);
            return sendData.Data;
        }
    }

    public NetworkPacket() {
        data = new byte[0];
    }

    public NetworkPacket(NetworkData data, bool compressed = false) {
        header = 0x80;
        if (compressed) {
            header = 0xa0;
            this.compressed = true;
        }
        this.data = new byte[data.Data.Length];
        Buffer.BlockCopy(data.Data, 0, this.data, 0, data.Data.Length);
    }

    public NetworkPacket(byte header, byte[] data) {
        this.header = header;
        this.data = new byte[data.Length];
        Buffer.BlockCopy(data, 0, this.data, 0, data.Length);
        if (header == 0xa0)
            compressed = true;
    }

    public NetworkObject GetObject() {
        if (compressed)
            Decompress();

        NetworkObject obj = new NetworkObject(data);
        return obj;
    }

    public void Compress() {
        if (compressed)
            return;
        MemoryStream outStream = new();
        using (ZOutputStream zstream = new(outStream, 9)) {
            zstream.Write(data);
            zstream.Flush();
        }
        data = outStream.ToArray();
        header = 0xa0;
    }

    public void Decompress() {
        MemoryStream outStream = new();
        using (ZOutputStream zstream = new(outStream)) {
            zstream.Write(data);
            zstream.Flush();
        }
        data = outStream.ToArray();
    }
}
