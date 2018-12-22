
// 262 scanlines = 20 vblank
// 2C02 - http://nesdev.com/2C02%20technical%20reference.TXT
public class PPU
{
    #region Data / Constructor

    public const int SCREEN_WIDTH = 256;
    public const int SCREEN_HEIGHT = 240;

    private static uint[] PALETTE = new uint[] // RGBA
    {
        0x666666, 0x002A88, 0x1412A7, 0x3B00A4, 0x5C007E, 0x6E0040, 0x6C0600, 0x561D00,
        0x333500, 0x0B4800, 0x005200, 0x004F08, 0x00404D, 0x000000, 0x000000, 0x000000,
        0xADADAD, 0x155FD9, 0x4240FF, 0x7527FE, 0xA01ACC, 0xB71E7B, 0xB53120, 0x994E00,
        0x6B6D00, 0x388700, 0x0C9300, 0x008F32, 0x007C8D, 0x000000, 0x000000, 0x000000,
        0xFFFEFF, 0x64B0FF, 0x9290FF, 0xC676FF, 0xF36AFF, 0xFE6ECC, 0xFE8170, 0xEA9E22,
        0xBCBE00, 0x88D800, 0x5CE430, 0x45E082, 0x48CDDE, 0x4F4F4F, 0x000000, 0x000000,
        0xFFFEFF, 0xC0DFFF, 0xD3D2FF, 0xE8C8FF, 0xFBC2FF, 0xFEC4EA, 0xFECCC5, 0xF7D8A5,
        0xE4E594, 0xCFEF96, 0xBDF4AB, 0xB3F3CC, 0xB5EBF2, 0xB8B8B8, 0x000000, 0x000000,
    };

    public int Cycle;      // 0-340
    public int Scanline;   // 0-261, 0-239=visible, 240=post, 241-260=vblank, 261=pre
    public ulong Frame;    // Frame counter

    private byte[] NameTableData = new byte[2048]; // http://wiki.nesdev.com/w/index.php/PPU_nametables
    private byte[] PaletteData = new byte[32];
    private byte[] oamData = new byte[256]; // value decays?

    public uint[] screen { get { return screenFront; } }
    public uint[] screenFront = new uint[256 * 240];
    public uint[] screenBack = new uint[256 * 240];

    // registers

    ushort V;   // current vram address (15 bit)
    ushort T;   // temporary vram address (15 bit)
    byte X;     // fine X scroll (3 bit)
    byte W;     // write toggle (1 bit)
    byte F;     // even/odd f rame flag (1 bit)

    byte register;

    // NMI flags
    bool nmiOccurred, nmiOutput, nmiPrevious;
    byte nmiDelay;

    // background temporary variables
    byte nameTableByte;
    byte attributeTableByte;
    byte lowTileByte;
    byte highTileByte;
    ulong tileData;

    // sprite temporary variables
    int SpriteCount;
    uint[] spritePatterns = new uint[8];
    byte[] spritePositions = new byte[8];
    byte[] spritePriorities = new byte[8];
    byte[] spriteIndexes = new byte[8];

    // $001 PPU MASK
    public byte 
        flagGreyscale,
        flagShowLeftBackground, 
        flagShowLeftSprites,
        flagShowBackground, 
        flagShowSprites,
        flagRedTint, 
        flagGreenTint, 
        flagBlueTint;

    // $2000 PPU CTRL
    byte 
        flagNameTable, 
        flagIncrement, 
        flagSpriteTable,
        flagBackgroundTable,
        flagSpriteSize,
        flagMasterSlave;

    // $2002 PPU STATUS
    byte 
        flagSpriteZeroHit, 
        flagSpriteOverflow;

    // $2003 PPU OAM ADDR
    byte oamAddress;

    // $2007 PPUDATA
    byte bufferedData;

    private NES NES;

    public PPU(NES nes)
    {
        NES = nes;

        for (int i = 0; i < PALETTE.Length; ++i)
        {
            uint color = PALETTE[i];

            uint r = (byte)((color >> 16) & 0xFF);
            uint g = (byte)((color >>  8) & 0xFF);
            uint b = (byte)((color >>  0) & 0xFF);

            // RGBA -> ABGR
            uint newCol = 0xFF000000;
            newCol |= (r >>  0); // R
            newCol |= (g <<  8); // G
            newCol |= (b << 16); // B

            PALETTE[i] = newCol;
        }
    }

    #endregion

    #region Power / Reset

    public void Power()
    {
        for (int i = 0; i < screen.Length; ++i) screen[i] = 0xFF000000;

        Reset();

        //for (int i = 0; i < SCREEN_HEIGHT * SCREEN_HEIGHT; ++i) screenBack[i] = screenFront[i] = PALETTE[i % PALETTE.Length];
    }

    public void Reset()
    {
        Cycle = 36; // 340;
        Scanline = 0; // 240;
        Frame = 0;

        WriteRegister(0x2000, 0); // PPU CTRL
        WriteRegister(0x2001, 0); // PPU MASK
        WriteRegister(0x2003, 0); // OAM ADDR
    }

    #endregion

    #region Register Manipulation

    private void IncrementX() // increment hori(v)
    {
        if ((V & 0x001F) == 31) // if coarse X == 31
        {
            V &= 0xFFE0; // coarse X = 0
            V ^= 0x0400; // switch horizontal nametable
        }
        else
        {
            V += 1; // increment coarse X
        }
    }

    private void IncrementY() // increment vert(v)
    {
        if ((V & 0x7000) != 0x7000) // if fine Y < 7
        {
            V += 0x1000; // increment fine Y
        }
        else
        {
            V &= 0x8FFF; // fine Y = 0

            var Y = (V & 0x03E0) >> 5; // let y = coarse Y

            if (Y == 29)
            {
                Y = 0; // coarse Y = 0

                V ^= 0x0800; // switch vertical nametable
            }
            else if (Y == 31)
            {
                Y = 0; // coarse Y = 0, nametable not switched
            }
            else
            {
                Y += 1; // increment coarse Y
            }

            // put coarse Y back into V
            V = (ushort)((V & 0xFC1F) | (Y << 5));
        }
    }
    
    public byte ReadRegister(ushort addr)
    {
        switch (addr)
        {
            case 0x2002: // PPU STATUS
                {
                    byte val = (byte)(register & 0x1F);     // 0001 1111
                    val |= (byte)(flagSpriteOverflow << 5); // 0010 0000
                    val |= (byte)(flagSpriteZeroHit << 6);  // 0100 0000
                    if (nmiOccurred) val |= 0x80;           // 1000 0000
                    nmiOccurred = false;
                    nmiChange();
                    W = 0;
                    return val;
                }
            case 0x2004: // OAM DATA
                {
                    return oamData[oamAddress];
                }
            case 0x2007: // PPU DATA
                {
                    byte val = NES.MMC.ReadByte(Bank.PPU, V);
                    // emulate buffered reads
                    if (V % 0x4000 < 0x3F00)
                    {
                        var buffered = bufferedData;
                        bufferedData = val;
                        val = buffered;
                    }
                    else
                    {
                        bufferedData = NES.MMC.ReadByte(Bank.PPU, (ushort)(V - 0x1000));
                    }
                    V += (ushort)((flagIncrement == 0) ? 1 : 32);
                    return val;
                }
            default:
                {
                    return 0;
                }
        }
    }

    public void WriteRegister(ushort addr, byte val)
    {
        register = val;

        switch (addr)
        {
            case 0x2000: // PPU CTRL
                {
                    flagNameTable = (byte)((val >> 0) & 3);
                    flagIncrement = (byte)((val >> 2) & 1);
                    flagSpriteTable = (byte)((val >> 3) & 1);
                    flagBackgroundTable = (byte)((val >> 4) & 1);
                    flagSpriteSize = (byte)((val >> 5) & 1);
                    flagMasterSlave = (byte)((val >> 6) & 1);
                    nmiOutput = ((val >> 7) & 1) == 1;
                    nmiChange();
                    T = (ushort)((T & 0xF3FF) | ((val & 0x03) << 10));
                }
                break;
            case 0x2001: // PPU MASK
                {
                    flagGreyscale = (byte)((val >> 0) & 1);
                    flagShowLeftBackground = (byte)((val >> 1) & 1);
                    flagShowLeftSprites = (byte)((val >> 2) & 1);
                    flagShowBackground = (byte)((val >> 3) & 1);
                    flagShowSprites = (byte)((val >> 4) & 1);
                    flagRedTint = (byte)((val >> 5) & 1);
                    flagGreenTint = (byte)((val >> 6) & 1);
                    flagBlueTint = (byte)((val >> 7) & 1);
                }
                break;
            case 0x2003: // OAM ADDR
                {
                    oamAddress = val;
                }
                break;
            case 0x2004: // OAM DATA
                {
                    oamData[oamAddress++] = val;
                }
                break;
            case 0x2005: // PPU SCROLL
                {
                    if (W == 0)
                    {
                        // t: ........ ...HGFED = d: HGFED...
                        // x:               CBA = d: .....CBA
                        // w:                   = 1
                        T = (ushort)((T & 0xFFE0) | (val >> 3));
                        X = (byte)(val & 0x07);
                        W = 1;
                    }
                    else
                    {
                        // t: .CBA..HG FED..... = d: HGFEDCBA
                        // w:                   = 0
                        T = (ushort)((T & 0x8FFF) | ((val & 0x07) << 12));
                        T = (ushort)((T & 0xFC1F) | ((val & 0xF8) << 2));
                        W = 0;
                    }
                }
                break;
            case 0x2006: // PPU ADDR
                {
                    if (W == 0)
                    {
                        // t: ..FEDCBA ........ = d: ..FEDCBA
                        // t: .X...... ........ = 0
                        // w:                   = 1
                        T = (ushort)((T & 0x80FF) | ((val & 0x3F) << 8));
                        W = 1;
                    }
                    else
                    {
                        // t: ........ HGFEDCBA = d: HGFEDCBA
                        // v                    = t
                        // w:                   = 0
                        T = (ushort)((T & 0xFF00) | val);
                        V = T;
                        W = 0;
                    }
                }
                break;
            case 0x2007: // PPU DATA
                {
                    NES.MMC.WriteByte(Bank.PPU, V, val);
                    V += (ushort)(flagIncrement == 0 ? 1 : 32);
                }
                break;
            case 0x4014: // OAM DMA
                {
                    ushort address = (ushort)(val << 8);
                    for (int i = 0; i < 256; ++i)
                    {
                        oamData[oamAddress++] = NES.MMC.ReadByte(Bank.CPU, address++);
                    }
                    NES.CPU.Stall += NES.CPU.CycleCounter % 2 == 1 ? 514 : 513;
                }
                break;
        }
    }

    public byte ReadRegisterPPU(ushort addr)
    {
        if (addr < 0x3F00)
        {
            addr = NES.MMC.MirrorAddress(addr, NES.MAPPER.MirrorMode);
            return NameTableData[addr % 2048];
        }
        else
        {
            return ReadPalette((ushort)(addr % 32));
        }
    }

    public void WriteRegisterPPU(ushort addr, byte val)
    {
        if (addr < 0x3F00)
        {
            addr = NES.MMC.MirrorAddress(addr, NES.MAPPER.MirrorMode);
            NameTableData[addr % 2048] = val;
        }
        else
        {
            WritePalette((ushort)(addr % 32), val);
        }
    }

    private byte ReadPalette(ushort addr)
    {
        if (addr >= 16 && addr % 4 == 0) addr -= 16;

        return PaletteData[addr];
    }

    private void WritePalette(ushort addr, byte val)
    {
        if (addr >= 16 && addr % 4 == 0) addr -= 16;

        PaletteData[addr] = val;
    }

    private void nmiChange()
    {
        var nmi = nmiOutput && nmiOccurred;

        // TODO: this fixes some games but the delay shouldn't have to be so
        // long, so the timings are off somewhere
        if (nmi && !nmiPrevious) nmiDelay = 15;

        nmiPrevious = nmi;
    }

    #endregion

    #region HighLevelRendering

    private void EvaluateSprites()
    {
        int h = flagSpriteSize == 0 ? 8 : 16;
        int count = 0;

        for (int i = 0; i < 64; ++i)
        {
            byte y = oamData[i * 4 + 0];
            byte a = oamData[i * 4 + 2];
            byte x = oamData[i * 4 + 3];
            int row = Scanline - y;

            if (row < 0 || row >= h) continue;

            if (count < 8)
            {
                spritePatterns[count] = FetchSpritePattern(i, row);
                spritePositions[count] = x;
                spritePriorities[count] = (byte)((a >> 5) & 1);
                spriteIndexes[count] = (byte)i;
            }
            count += 1;
        }

        if (count > 8)
        {
            count = 8;
            flagSpriteOverflow = 1;
        }
        SpriteCount = count;
    }

    private uint FetchSpritePattern(int i, int row)
    {
        var tile = oamData[i * 4 + 1];
        var attributes = oamData[i * 4 + 2];
        ushort addr;

        if (flagSpriteSize == 0)
        {
            if ((attributes & 0x80) == 0x80) row = 7 - row;

            addr = (ushort)(0x1000 * flagSpriteTable + tile * 16 + row);
        }
        else
        {
            if ((attributes & 0x80) == 0x80) row = 15 - row;

            var table = tile & 1;
            tile &= 0xFE;

            if (row > 7)
            {
                tile += 1;
                row -= 8;
            }

            addr = (ushort)(0x1000 * table + tile * 16 + (ushort)row);
        }

        var a = (attributes & 0x3) << 2;
        var lowTileByte = NES.MMC.ReadByte(Bank.PPU, addr);
        var highTileByte = NES.MMC.ReadByte(Bank.PPU, (ushort)(addr + 8));

        uint data = 0;

        for (int x = 0; x < 8; ++x)
        {
            byte p1 = 0, p2 = 0;
            if ((attributes & 0x40) == 0x40)
            {
                p1 = (byte)((lowTileByte & 0x1) << 0);
                p2 = (byte)((highTileByte & 0x1) << 1);
                lowTileByte >>= 1;
                highTileByte >>= 1;
            }
            else
            {
                p1 = (byte)((lowTileByte & 0x80) >> 7);
                p2 = (byte)((highTileByte & 0x80) >> 6);
                lowTileByte <<= 1;
                highTileByte <<= 1;
            }
            data <<= 4;
            data |= (uint)(a | p1 | p2);
        }

        return data;
    }

    private void RenderPixel()
    {
        // [ BackgroundPixel in-line ]
        byte background = 0;
        if (flagShowBackground != 0)
        {
            uint fetchTileData = (uint)(tileData >> 32);
            fetchTileData >>= ((7 - X) * 4);
            background = (byte)(fetchTileData & 0x0F);
        }

        // [ SpritePixel in-line ]
        byte i = 0, sprite = 0;
        if (flagShowSprites != 0)
        {
            for (int j = 0; j < SpriteCount; ++j)
            {
                var offset = (Cycle - 1) - spritePositions[j];

                if (offset < 0 || offset > 7) continue;

                byte pixelColor = (byte)((spritePatterns[j] >> ((7 - offset) * 4)) & 0x0F);

                if (pixelColor % 4 != 0)
                {
                    i = (byte)j;
                    sprite = pixelColor;

                    break;
                }
            }
        }

        // RenderPixel
        int x = Cycle - 1;
        int y = Scanline;

        if (x < 8)
        {
            if (flagShowLeftBackground == 0) background = 0;

            if (flagShowLeftSprites == 0) sprite = 0;
        }

        bool b = (background % 4) != 0;
        bool s = (sprite % 4) != 0;
        byte color = 0;

        if (!b && !s)
            color = 0;
        else if (!b && s)
            color = (byte)(sprite | 0x10);
        else if (b && !s)
            color = background;
        else
        {
            if (spriteIndexes[i] == 0 && x < 255) flagSpriteZeroHit = 1;

            if (spritePriorities[i] == 0)
                color = (byte)(sprite | 0x10);
            else
                color = background;
        }

        screenBack[x + y * SCREEN_WIDTH] = PALETTE[ReadPalette((ushort)(color % 64))];
    }

    public void Step()
    {
        if (nmiDelay > 0 && --nmiDelay == 0 && nmiOutput && nmiOccurred)
        {
            NES.CPU.Interrupt = CPU.INTERRUPT_NMI;
        }

        if ((flagShowBackground != 0 || flagShowSprites != 0) &&
            (F == 1 && Scanline == 261 && Cycle == 339))
        {
            Cycle = 0;
            Scanline = 0;
            Frame += 1;
            F ^= 1;
        }
        else if (++Cycle > 340)
        {
            Cycle = 0;
            if (++Scanline > 261)
            {
                Scanline = 0;
                Frame += 1;
                F ^= 1;
            }
        }

        bool renderingEnabled = flagShowBackground != 0 || flagShowSprites != 0;
        bool preLine = Scanline == 261;
        bool visibleLine = Scanline < 240;
        bool renderLine = preLine || visibleLine;
        bool preFetchCycle = Cycle >= 321 && Cycle <= 336;
        bool visibleCycle = Cycle >= 1 && Cycle <= 256;
        bool fetchCycle = preFetchCycle || visibleCycle;

        if (renderingEnabled)
        {
            if (visibleLine && visibleCycle) RenderPixel();

            if (renderLine && fetchCycle)
            {
                tileData <<= 4;

                switch (Cycle % 8)
                {
                    case 1: // FetchNameTableByte
                        {
                            ushort v = V;
                            ushort addr = (ushort)(0x2000 | (v & 0x0FFF));
                            nameTableByte = NES.MMC.ReadByte(Bank.PPU, addr);
                            break;
                        }
                    case 3: // FetchAttributeTableByte
                        {
                            ushort v = V;
                            ushort addr = (ushort)(0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07));
                            var shift = ((v >> 4) & 4) | (v & 2);
                            attributeTableByte = (byte)(((NES.MMC.ReadByte(Bank.PPU, addr) >> shift) & 3) << 2);
                            break;
                        }
                    case 5: // FetchLowTileByte
                        {
                            var fineY = (V >> 12) & 7;
                            var table = flagBackgroundTable;
                            var tile = nameTableByte;
                            ushort addr = (ushort)(0x1000 * table + tile * 16 + fineY);
                            lowTileByte = NES.MMC.ReadByte(Bank.PPU, addr);
                            break;
                        }
                    case 7: // FetchHighTileByte
                        {
                            var fineY = (V >> 12) & 7;
                            var table = flagBackgroundTable;
                            var tile = nameTableByte;
                            ushort addr = (ushort)(0x1000 * table + tile * 16 + fineY + 8);
                            highTileByte = NES.MMC.ReadByte(Bank.PPU, addr);
                            break;
                        }
                    case 0: // StoreTileData
                        {
                            uint data = 0;
                            for (int i = 0; i < 8; ++i)
                            {
                                uint a = attributeTableByte;
                                uint p1 = (uint)((lowTileByte & 0x80) >> 7);
                                uint p2 = (uint)((highTileByte & 0x80) >> 6);
                                lowTileByte <<= 1;
                                highTileByte <<= 1;
                                data <<= 4;
                                data |= (a | p1 | p2);
                            }
                            tileData |= data;
                            break;
                        }
                }
            }

            if (preLine && Cycle >= 280 && Cycle <= 304)
            {
                // [ CopyY in-line ]
                // vert(v) = vert(t)
                // v: .IHGF.ED CBA..... = t: .IHGF.ED CBA.....
                V = (ushort)((V & 0x841F) | (T & 0x7BE0));
            }

            if (renderLine)
            {
                if (fetchCycle && (Cycle % 8 == 0)) IncrementX();

                if (Cycle == 256) IncrementY();

                // [ CopyX in-line ]
                // hori(v) = hori(t)
                // v: .....F.. ...EDCBA = t: .....F.. ...EDCBA
                else if (Cycle == 257)
                    V = (ushort)((V & 0xFBE0) | (T & 0x041F));
            }

            if (renderLine && Cycle == 257)
            {
                if (visibleLine)
                    EvaluateSprites();
                else
                    SpriteCount = 0;
            }
        }

        if (Scanline == 241 && Cycle == 1)
        {
            // [ SetVerticalBlank in-line ]
            var tmp = screenBack;
            screenBack = screenFront;
            screenFront = tmp;
            nmiOccurred = true;
            nmiChange();
        }

        if (preLine && Cycle == 1)
        {
            // [ ClearVerticalBlank in-line ]
            nmiOccurred = false;
            nmiChange();

            flagSpriteZeroHit = 0;
            flagSpriteOverflow = 0;
        }
    }

    #endregion
}