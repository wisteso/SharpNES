using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.InteropServices;

public class Screen : Game
{
    //[DllImport("nes_ntsc", CharSet = CharSet.Auto, SetLastError = true)]
    //internal static extern int GetWindowText(IntPtr hWnd, [Out, MarshalAs(UnmanagedType.LPTStr)] string lpString, int nMaxCount);

    private const int SCALE = 3;

    NES NES;
    Texture2D buffer; // AABBGGRR

    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;

    public Screen(NES nes)
    {
        NES = nes;
        NES.CONTROLLER_1 = new MonoGameController();

        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = PPU.SCREEN_WIDTH * SCALE;
        graphics.PreferredBackBufferHeight = PPU.SCREEN_HEIGHT * SCALE;
        graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
        graphics.ApplyChanges();

        Content.RootDirectory = "Content";
    }

    protected override void OnExiting(object sender, EventArgs args)
    {
        NES.Power();

        base.OnExiting(sender, args);
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
        buffer = new Texture2D(GraphicsDevice, 256, 240, false, SurfaceFormat.Color);

        base.Initialize();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        // Create a new SpriteBatch, which can be used to draw textures.
        spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// game-specific content.
    /// </summary>
    protected override void UnloadContent()
    {
        // TODO: Unload any non ContentManager content here
    }

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        var controller = (MonoGameController)NES.CONTROLLER_1;
        controller.Update();
        

        base.Update(gameTime);
    }

    Random rnd = new Random();
    Rectangle scale = new Rectangle(0, 0, PPU.SCREEN_WIDTH * SCALE, PPU.SCREEN_HEIGHT * SCALE);

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        //int shift = 0;//(rnd.Next(0, 3) * 8);
        //uint mask = 0x000000FFU << shift;
        //NES.PPU.screen[rnd.Next(PPU.SCREEN_WIDTH * PPU.SCREEN_HEIGHT)] ^= mask;

        buffer.SetData(NES.PPU.screen);
        
        spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
        spriteBatch.Draw(buffer, scale, Color.White);
        spriteBatch.End();

        base.Draw(gameTime);
    }
}

public class MonoGameController : Controller
{
    public void Update()
    {
        var state = Keyboard.GetState();
        ButtonLevels[0] = Convert.ToByte(state.IsKeyDown(Keys.A)); // A
        ButtonLevels[1] = Convert.ToByte(state.IsKeyDown(Keys.S)); // B
        ButtonLevels[2] = Convert.ToByte(state.IsKeyDown(Keys.Q)); // Select
        ButtonLevels[3] = Convert.ToByte(state.IsKeyDown(Keys.W)); // Start
        ButtonLevels[4] = Convert.ToByte(state.IsKeyDown(Keys.Up)); // Up
        ButtonLevels[5] = Convert.ToByte(state.IsKeyDown(Keys.Down)); // Down
        ButtonLevels[6] = Convert.ToByte(state.IsKeyDown(Keys.Left)); // Left
        ButtonLevels[7] = Convert.ToByte(state.IsKeyDown(Keys.Right)); // Right

        var pad = GamePad.GetState(0);
        ButtonLevels[0] |= Convert.ToByte(pad.IsButtonDown(Buttons.A));
        ButtonLevels[1] |= Convert.ToByte(pad.IsButtonDown(Buttons.B));
        ButtonLevels[2] |= Convert.ToByte(pad.IsButtonDown(Buttons.Back));
        ButtonLevels[3] |= Convert.ToByte(pad.IsButtonDown(Buttons.Start));
        ButtonLevels[4] |= Convert.ToByte(pad.ThumbSticks.Left.Y >  0.35F);
        ButtonLevels[5] |= Convert.ToByte(pad.ThumbSticks.Left.Y < -0.35F);
        ButtonLevels[6] |= Convert.ToByte(pad.ThumbSticks.Left.X < -0.35F);
        ButtonLevels[7] |= Convert.ToByte(pad.ThumbSticks.Left.X >  0.35F);
    }

    // A, B, Select, Star, Up, Down, Left, Right
    private byte[] ButtonLevels = new byte[8];

    byte index = 0;
    byte strobe = 0;

    public byte Read()
    {
        byte val = (index < ButtonLevels.Length) ? ButtonLevels[index] : (byte)0;

        index += 1;

        if ((strobe & 1) == 1) index = 0;

        return val;
    }

    public void Write(byte val)
    {
        strobe = val;

        if ((strobe & 1) == 1) index = 0;
    }
}