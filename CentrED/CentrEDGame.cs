using System.Reflection;
using CentrED.Map;
using CentrED.UI;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;
using static SDL3.SDL;
using CentrED.Client.Map;
using CentrED.Client;

namespace CentrED;

public class CentrEDGame : Game
{
    public readonly GraphicsDeviceManager _gdm;

    public MapManager MapManager;
    public UIManager UIManager;
    public bool Closing { get; set; }

    public CentrEDGame()
    {
        _gdm = new GraphicsDeviceManager(this)
        {
            IsFullScreen = false,
            PreferredDepthStencilFormat = DepthFormat.Depth24
        };

        _gdm.PreparingDeviceSettings += (sender, e) =>
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                RenderTargetUsage.DiscardContents;
        };
        var appName = Assembly.GetExecutingAssembly().GetName();
        Window.Title = $"{appName.Name} {appName.Version}";

        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnWindowResized;
    }

    protected override void Initialize()
    {
        if (_gdm.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _gdm.GraphicsProfile = GraphicsProfile.HiDef;
        }

        _gdm.ApplyChanges();

        Log.Start(LogTypes.All);
        MapManager = new MapManager(_gdm.GraphicsDevice, Window);
        UIManager = new UIManager(_gdm.GraphicsDevice, Window);
        CentrED.Map.RadarMap.Initialize(_gdm.GraphicsDevice);

        base.Initialize();
    }

    protected override void BeginRun()
    {
        base.BeginRun();
        SDL_MaximizeWindow(Window.Handle);
    }

    protected override void UnloadContent()
    {
        CEDClient.Disconnect();
    }

    protected override void Update(GameTime gameTime)
    {
        try
        {
            var packetsProcessed = 0;
            while (packetsProcessed < 10000 && ClientPacketQueue.TryDequeue(out var packet))
            {
                try
                {
                    // LargeScaleOperationPacket (opcode 0x0E) nunca deve ser comprimido!
                    if (packet is LargeScaleOperationPacket)
                    {
                        Console.WriteLine($"[DEBUG] Enviando pacote: LargeScaleOperationPacket (opcode 0x0E)");
                        CEDClient.Send(packet); // NUNCA use SendCompressed aqui!
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Enviando pacote: {packet.GetType().Name}");
                        CEDClient.Send(packet);
                    }
                    Console.WriteLine($"[DEBUG] Pacote enviado: {packet.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Falha ao enviar pacote: {ex}");
                }
                packetsProcessed++;
            }
            Metrics.Start("UpdateClient");
            CEDClient.Update();
            Metrics.Stop("UpdateClient");
            MapManager.Update(gameTime, IsActive, !UIManager.CapturingMouse, !UIManager.CapturingKeyboard);
            Config.AutoSave();
        }
        catch (Exception e)
        {
            UIManager.ReportCrash(e);
            Console.WriteLine($"[ERROR] Exceção no Update: {e}");
        }
        base.Update(gameTime);
    }

    protected override bool BeginDraw()
    {
        Metrics.Start("BeginDraw");
        //We can rely on UIManager, since it draws UI over the main window as well as handles to all the extra windows
        var maxWindowSize = UIManager.MaxWindowSize();
        var width = (int)maxWindowSize.X;
        var height = (int)maxWindowSize.Y;
        if (width > 0 && height > 0)
        {
            var pp = GraphicsDevice.PresentationParameters;
            if (width != pp.BackBufferWidth || height != pp.BackBufferHeight)
            {
                pp.BackBufferWidth = width;
                pp.BackBufferHeight = height;
                pp.DeviceWindowHandle = Window.Handle;
                GraphicsDevice.Reset(pp);
            }
        }
        Metrics.Stop("BeginDraw");
        return base.BeginDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (gameTime.ElapsedGameTime.Ticks > 0)
        {
            try
            {
                Metrics.Start("Draw");
                MapManager.Draw();
                UIManager.Draw(gameTime, IsActive);
                Present();
                UIManager.DrawExtraWindows();
                Metrics.Stop("Draw");
            }
            catch (Exception e)
            {
                UIManager.ReportCrash(e);
            }
        }
        base.Draw(gameTime);
    }

    private void Present()
    {
        Rectangle bounds = Window.ClientBounds;
        GraphicsDevice.Present(
            new Rectangle(0, 0, bounds.Width, bounds.Height),
            null,
            Window.Handle
        );
    }

    protected override void EndDraw()
    {
        //Restore main window viewport and scissor rectangle for next tick Update()
        var gameWindowRect = Window.ClientBounds;
        GraphicsDevice.Viewport = new Viewport(0, 0, gameWindowRect.Width, gameWindowRect.Height);
        GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, gameWindowRect.Width, gameWindowRect.Height);
    }

    private void OnWindowResized(object? sender, EventArgs e)
    {
        GameWindow window = sender as GameWindow;
        if (window != null)
            MapManager.OnWindowsResized(window);
    }
}