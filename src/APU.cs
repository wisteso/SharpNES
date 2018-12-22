
public class APU
{
    private const double frameCounterRate = 1F;
    //private const frameCounterRate = CPUFrequency / 240F;

    private byte[] lengthTable = new byte[]
    {
        10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
        12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30,
    };

    private byte[][] dutyTable = new byte[][]
    {
        new byte[]{0, 1, 0, 0, 0, 0, 0, 0},
        new byte[]{0, 1, 1, 0, 0, 0, 0, 0},
        new byte[]{0, 1, 1, 1, 1, 0, 0, 0},
        new byte[]{1, 0, 0, 1, 1, 1, 1, 1},
    };

    private byte[] triangleTable = new byte[]
    {
        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
    };

    private ushort[] noiseTable = new ushort[]
    {
        4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068,
    };

    private byte[] dmcTable = new byte[]
    {
        214, 190, 170, 160, 143, 127, 113, 107, 95, 80, 71, 64, 53, 42, 36, 27,
    };

    private double[] pulseTable = new double[31];
    private double[] tndTable = new double[203];

    // APU

    private float channel;
    private double sampleRate;

    private Pulse pulse1;
    private Pulse pulse2;
    private Triangle triangle;
    private Noise noise;
    private DMC dmc;

    private ulong cycle;
    private byte framePeriod;
    private byte frameValue;
    private byte frameIRQ;

    private object filterChain;

    public APU(NES nes)
    {
        for (int i = 0; i < pulseTable.Length; ++i) pulseTable[i] = 95.52D / (8128D / i + 100D);

        for (int i = 0; i < tndTable.Length; ++i) tndTable[i] = 163.67D / (24329D / i + 100D);
    }

    public void Power() { }

    public void Step()
    {
        var cycle1 = cycle;
        cycle++;
        var cycle2 = cycle;
        stepTimer();

        int f1 = (int)(cycle1 / frameCounterRate);
        int f2 = (int)(cycle2 / frameCounterRate);

        if (f1 != f2) stepFrameCounter();

        int s1 = (int)(cycle1 / sampleRate);
        int s2 = (int)(cycle2 / sampleRate);

        if (s1 != s2) sendSample();
    }

    private void stepTimer() { }

    private void stepFrameCounter() { }

    private void sendSample() { }

    public void WriteRegister(ushort addr, byte val)
    {
        switch (addr)
        {
            case 0x4000:
                //pulse1.writeControl(val);
                break;
            case 0x4001:
                //pulse1.writeSweep(val);
                break;
            case 0x4002:
                //pulse1.writeTimerLow(val);
                break;
            case 0x4003:
                //pulse1.writeTimerHigh(val);
                break;
            case 0x4004:
                //pulse2.writeControl(val);
                break;
            case 0x4005:
                //pulse2.writeSweep(val);
                break;
            case 0x4006:
                //pulse2.writeTimerLow(val);
                break;
            case 0x4007:
                //pulse2.writeTimerHigh(val);
                break;
            case 0x4008:
                //triangle.writeControl(val);
                break;
            case 0x4009:
                // does nothing
                break;
            case 0x400A:
                //triangle.writeTimerLow(val);
                break;
            case 0x400B:
                //triangle.writeTimerHigh(val);
                break;
            case 0x400C:
                //noise.writeControl(val);
                break;
            case 0x400D:
                // does nothing
                break;
            case 0x400E:
                //noise.writePeriod(val);
                break;
            case 0x400F:
                //noise.writeLength(val);
                break;
            case 0x4010:
                //dmc.writeControl(val);
                break;
            case 0x4011:
                //dmc.writeValue(val);
                break;
            case 0x4012:
                //dmc.writeAddress(val);
                break;
            case 0x4013:
                //dmc.writeLength(val);
                break;
            case 0x4015: // APU Status
                //writeControl(val);
                break;
            case 0x4017: // APU Frame Counter
                //writeFrameCounter(val);
                break;
        }
    }

    public byte ReadRegister(ushort addr)
    {
        if (addr != 0x4015) return 0;

        byte res = 0;

        // or bits

        return res;
    }
}

public class Pulse
{

}

public class Triangle
{

}

public class Noise
{

}

public class DMC
{

}