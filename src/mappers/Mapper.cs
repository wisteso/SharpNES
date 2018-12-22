
public class Mapper
{
    public byte[] PRG;  // 0x10000
    public byte[] CHR;  // 0x4000
    public byte[] SRAM; // 0x2000
    public byte MapperType;
    public byte MirrorMode;
    public byte BatteryFlag;

    public virtual void Step() { }
    public virtual byte Read(ushort addr) { return 0; }
    public virtual void Write(ushort addr, byte val) { }
    public virtual void Load(byte[] prg, byte[] chr)
    {
        SRAM = new byte[0x2000];

        PRG = prg;
        CHR = chr;
    }
}