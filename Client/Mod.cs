using Microsoft.Extensions.Logging;
using OblivionMp.Sdk;
using OblivionMp.Sdk.Archetypes.Mixins;
using ReadyM.Api.DI;
using ReadyM.Modloader.Mods;
using ReadyM.Sdk.Common;

namespace OblivionTravel.Client;

public sealed class Mod : ModBase
{
    internal static TravelController? Controller;
    public override string Name => "Oblivion Travel";

    protected override void RegisterServices(IDependencyContainer services)
        => services.RegisterSingleton<ModSystemBase, TravelSystem>();

    public override void Start()
    {
        Controller = null;
        try
        {
            var path = Path.Combine(ModDirectory, "config.json");
            var settings = File.Exists(path) ? TravelSettings.Parse(File.ReadAllText(path)) : new TravelSettings();
            Controller = new TravelController(settings);
            Logger.LogInformation("[OblivionTravel] Map travel: {Policy}; native visibility preserved. Discovery persistence across default-save reloads is NOT verified. Config: {Path}", settings.MapFastTravel, path);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[OblivionTravel] Invalid configuration; mod disabled without changing the map.");
        }
    }

    public override void DeInit() { Controller = null; base.DeInit(); }
}

public sealed class TravelSystem(IPlayers players, ILogger logger) : ModSystemBase
{
    NativeConsole? console;
    readonly HostReadiness host = new();
    double nextWarning;

    protected override void OnUpdate(UpdateTick tick)
    {
        if (Mod.Controller is not { } controller) return;
        var now = Environment.TickCount64 / 1000d;
        try
        {
            var frame = default(TravelFrame);
            if (players.LocalMainCharacter is { IsValid: true } player)
            {
                var ready = host.Read();
                frame = new TravelFrame(ready.Restored, player.Hp > 0, player.Pawn, ready.Area);
            }
            controller.Tick(now, frame, command =>
            {
                try
                {
                    (console ??= new NativeConsole()).Run(frame.Pawn, command);
                    logger.LogDebug("[OblivionTravel] Submitted {Command}; native command effects require in-game verification.", command);
                    return true;
                }
                catch (Exception ex) { Warn(now, ex); return false; }
            });
        }
        catch (Exception ex) { controller.Reset(); Warn(now, ex); }
    }

    void Warn(double now, Exception ex)
    {
        if (now < nextWarning) return;
        nextWarning = now + 30;
        logger.LogWarning(ex, "[OblivionTravel] Host/native bridge unavailable; deferring map command.");
    }
}
