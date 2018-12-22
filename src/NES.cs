using System.IO;
using System.Threading;

public class NES
{
    public bool Debug = false;

    private bool Running;
    private int Sleep;

    public Memory MMC;
    public CPU CPU;
    public PPU PPU;
    public APU APU;
    public Mapper MAPPER;

    public Controller CONTROLLER_1 = new NullController();
    public Controller CONTROLLER_2 = new NullController();

    public const string PROJECT_HOME = @"C:\dev\projects\sharpnes";
    public const string ROM_BASE = PROJECT_HOME + @"\data";
    public static StreamWriter LOG = new StreamWriter(@"C:\dev\projects\sharpnes\NES_log.txt");

    public const int SLEEP_FREQ = 2000000;

    public void Load(string romName, int mapper)
    {
        string prg = ROM_BASE + "\\" + romName + "\\program.rom", chr = ROM_BASE + "\\" + romName + "\\character.rom";

        var PRG = File.Exists(prg) ? File.ReadAllBytes(prg) : new byte[0x2000];
        var CHR = File.Exists(chr) ? File.ReadAllBytes(chr) : new byte[0x2000];

        switch (mapper)
        {
            case 1: MAPPER = new MMC1(this); break;
            case 4: MAPPER = new MMC3(this); break;
        }

        MAPPER.Load(PRG, CHR);
    }

    public void Power()
    {
        if (MAPPER == null) throw new System.Exception("No rom loaded");

        Running = !Running;

        if (Running)
        {
            Sleep = Debug ? 0 : 0;

            MMC = new Memory(this);
            CPU = new CPU(this);
            PPU = new PPU(this);
            APU = new APU(this);

            MMC.Power();
            CPU.Power();
            PPU.Power();
            APU.Power();
        }
    }

    public void StepForever()
    {
        long cycles = 0;
        long t1 = System.DateTime.Now.Ticks;

        int loops = SLEEP_FREQ;
        while (Running)
        {
            var cpuCycles = CPU.Step();
            cycles += cpuCycles;
            var ppuCycles = cpuCycles * 3;
            while (--ppuCycles > -1)
            {
                PPU.Step();
                MAPPER.Step();
            }

            while (cpuCycles-- > 0)
            {
                APU.Step();
            }

            //Thread.Sleep(Sleep);
            //if (--loops < 0)
            //{
            //    Thread.Sleep(Sleep);
            //    loops = SLEEP_FREQ;
            //}
        }

        long t2 = System.DateTime.Now.Ticks;
        var time = (t2 - t1) / (float)System.TimeSpan.TicksPerSecond;
        System.Console.WriteLine("MHz: " + cycles / time / 1000000);
    }
}