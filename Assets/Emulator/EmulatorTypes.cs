using int8_  = System.SByte;
using int16_ = System.Int16;
using int32_ = System.Int32;
using int64_ = System.Int64;

using uint8_  = System.Byte;
using uint16_ = System.UInt16;
using uint32_ = System.UInt32;
using uint64_ = System.UInt64;

// TODO maybe delete signed versions?

public struct int8 {
    public int8_ data;
    public int8(int value){ data = (int8_)(value); }

    // To and From int
    public static implicit operator int8(int from) => new int8(from);
    public static implicit operator int(int8 from) => (int)(from.data);

    // To 16, 32, and 64
    public static implicit operator int16(int8 from) => new int16((int) from.data);
    public static implicit operator int32(int8 from) => new int32((int) from.data);
    public static implicit operator int64(int8 from) => new int64((int) from.data);

    public static implicit operator uint16(int8 from) => new uint16((int) from.data);
    public static implicit operator uint32(int8 from) => new uint32((int) from.data);
    public static implicit operator uint64(int8 from) => new uint64((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X2");
    }
}

public struct int16 {
    public int16_ data;
    public int16(int value){ data = (int16_)(value); }

    // To and From int
    public static implicit operator int16(int from) => new int16(from);
    public static implicit operator int(int16 from) => (int)(from.data);

    // To 8, 32, and 64
    public static implicit operator int8(int16 from) => new int8((int) from.data);
    public static implicit operator int32(int16 from) => new int32((int) from.data);
    public static implicit operator int64(int16 from) => new int64((int) from.data);

    public static implicit operator uint8(int16 from) => new uint8((int) from.data);
    public static implicit operator uint32(int16 from) => new uint32((int) from.data);
    public static implicit operator uint64(int16 from) => new uint64((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X4");
    }
}

public struct int32 {
    public int32_ data;
    public int32(int value){ data = (int32_)(value); }

    // To and From int
    public static implicit operator int32(int from) => new int32(from);
    public static implicit operator int(int32 from) => (int)(from.data);

    // To 8, 16, and 64
    public static implicit operator int8(int32 from) => new int8((int) from.data);
    public static implicit operator int16(int32 from) => new int16((int) from.data);
    public static implicit operator int64(int32 from) => new int64((int) from.data);

    public static implicit operator uint8(int32 from) => new uint8((int) from.data);
    public static implicit operator uint16(int32 from) => new uint16((int) from.data);
    public static implicit operator uint64(int32 from) => new uint64((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X8");
    }
}

public struct int64 {
    public int64_ data;
    public int64(int value){ data = (int64_)(value); }

    // To and From int
    public static implicit operator int64(int from) => new int64(from);
    public static implicit operator int(int64 from) => (int)(from.data);

    // To 8, 16, and 32
    public static implicit operator int8(int64 from) => new int8((int) from.data);
    public static implicit operator int16(int64 from) => new int16((int) from.data);
    public static implicit operator int32(int64 from) => new int32((int) from.data);

    public static implicit operator uint8(int64 from) => new uint8((int) from.data);
    public static implicit operator uint16(int64 from) => new uint16((int) from.data);
    public static implicit operator uint32(int64 from) => new uint32((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X16");
    }
}

public struct uint8 {
    public uint8_ data;
    public uint8(int value){ data = (uint8_)(value); }

    // To and From int
    public static implicit operator uint8(int from) => new uint8(from);
    public static implicit operator int(uint8 from) => (int)(from.data);

    // To 16, 32, and 64
    public static implicit operator int16(uint8 from) => new int16((int) from.data);
    public static implicit operator int32(uint8 from) => new int32((int) from.data);
    public static implicit operator int64(uint8 from) => new int64((int) from.data);

    public static implicit operator uint16(uint8 from) => new uint16((int) from.data);
    public static implicit operator uint32(uint8 from) => new uint32((int) from.data);
    public static implicit operator uint64(uint8 from) => new uint64((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X2");
    }
}

public struct uint16 {
    public uint16_ data;
    public uint16(int value){ data = (uint16_)(value); }

    // To and From int
    public static implicit operator uint16(int from) => new uint16(from);
    public static implicit operator int(uint16 from) => (int)(from.data);

    // To 8, 32, and 64
    public static implicit operator int8(uint16 from) => new int8((int) from.data);
    public static implicit operator int32(uint16 from) => new int32((int) from.data);
    public static implicit operator int64(uint16 from) => new int64((int) from.data);

    public static implicit operator uint8(uint16 from) => new uint8((int) from.data);
    public static implicit operator uint32(uint16 from) => new uint32((int) from.data);
    public static implicit operator uint64(uint16 from) => new uint64((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X4");
    }
}

public struct uint32 {
    public uint32_ data;
    public uint32(int value){ data = (uint32_)(value); }

    // To and From int
    public static implicit operator uint32(int from) => new uint32(from);
    public static implicit operator int(uint32 from) => (int)(from.data);

    // To 8, 16, and 64
    public static implicit operator int8(uint32 from) => new int8((int) from.data);
    public static implicit operator int16(uint32 from) => new int16((int) from.data);
    public static implicit operator int64(uint32 from) => new int64((int) from.data);

    public static implicit operator uint8(uint32 from) => new uint8((int) from.data);
    public static implicit operator uint16(uint32 from) => new uint16((int) from.data);
    public static implicit operator uint64(uint32 from) => new uint64((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X8");
    }
}

public struct uint64 {
    public uint64_ data;
    public uint64(int value){ data = (uint64_)(value); }

    // To and From int
    public static implicit operator uint64(int from) => new uint64(from);
    public static implicit operator int(uint64 from) => (int)(from.data);

    // To 8, 16, and 32
    public static implicit operator int8(uint64 from) => new int8((int) from.data);
    public static implicit operator int16(uint64 from) => new int16((int) from.data);
    public static implicit operator int32(uint64 from) => new int32((int) from.data);

    public static implicit operator uint8(uint64 from) => new uint8((int) from.data);
    public static implicit operator uint16(uint64 from) => new uint16((int) from.data);
    public static implicit operator uint32(uint64 from) => new uint32((int) from.data);

    public override string ToString(){
        return "0x" + data.ToString("X16");
    }
}
