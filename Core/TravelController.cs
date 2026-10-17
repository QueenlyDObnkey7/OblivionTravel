namespace OblivionTravel;

// A transient observation, never an account/character identity or persistence key.
public readonly record struct TravelFrame(bool Restored, bool Alive, nint Pawn, string Area)
{
    public bool Ready => Restored && Alive && Pawn != 0 && !string.IsNullOrWhiteSpace(Area);
}

public sealed class TravelController
{
    readonly TravelSettings settings;
    TravelFrame previous;
    double stableAt, dueAt;
    bool havePrevious;

    public TravelController(TravelSettings settings)
    {
        settings.Validate();
        this.settings = settings;
    }

    public void Reset() { havePrevious = false; previous = default; stableAt = dueAt = 0; }

    // The sink can run only a fixed allowlisted map command. It cannot teleport, save, reveal or hide markers.
    public bool Tick(double now, TravelFrame frame, Func<string, bool> run)
    {
        if (!double.IsFinite(now)) throw new ArgumentOutOfRangeException(nameof(now));
        if (!settings.Enabled || settings.MapFastTravel == MapTravel.Unmanaged) return false;
        if (!frame.Ready) { Reset(); return false; }
        if (!havePrevious || previous.Pawn != frame.Pawn || previous.Area != frame.Area)
        {
            previous = frame;
            havePrevious = true;
            stableAt = now + settings.ReadyDelaySeconds;
            dueAt = stableAt;
        }
        if (now < stableAt || now < dueAt) return false;
        var command = settings.MapFastTravel == MapTravel.Block ? "EnableFastTravel 0" : "EnableFastTravel 1";
        var success = run(command);
        dueAt = now + (success ? settings.ReapplySeconds : 1);
        return success;
    }
}
