using OblivionTravel;

var count = 0;
void Check(bool ok, string label)
{
    if (!ok) throw new Exception("FAIL: " + label);
    Console.WriteLine("PASS: " + label); count++;
}
void Reject(string json, string label)
{
    try { TravelSettings.Parse(json); }
    catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException) { Check(true, label); return; }
    Check(false, label);
}
var settings = TravelSettings.Parse("{}");
Check(settings.MapFastTravel == MapTravel.Block && settings.Discovery == "PreserveNative", "safe configuration defaults");
Reject("null", "null config rejected");
Reject("{\"MapFastTravel\":0}", "numeric enum rejected");
Reject("{\"MapFastTravel\":\"Teleport\"}", "unknown policy rejected");
Reject("{\"Discovery\":\"Reset\"}", "destructive discovery mode rejected");
Reject("{\"Discovery\":null}", "null discovery mode rejected");
Reject("{\"CharacterGuid\":\"abc\"}", "custom identity setting rejected");
Reject("{\"ReapplySeconds\":0}", "unbounded command loop rejected");
Reject("{\"ReadyDelaySeconds\":31}", "invalid readiness delay rejected");

var controller = new TravelController(settings);
var frame = new TravelFrame(true, true, 123, "Tamriel");
var sent = new List<string>();
bool Send(string command) { sent.Add(command); return true; }
Check(!controller.Tick(0, frame with { Restored = false }, Send) && sent.Count == 0, "never submits during default save restoration");
Check(!controller.Tick(1, frame, Send) && !controller.Tick(2.99, frame, Send), "waits for stable ready interval");
Check(controller.Tick(3, frame, Send) && sent.Single() == "EnableFastTravel 0", "blocks map travel after ready");
Check(!controller.Tick(62.99, frame, Send) && controller.Tick(63, frame, Send) && sent.Count == 2, "periodic reapplication uses bounded cadence");
Check(!controller.Tick(64, frame with { Area = "City" }, Send) && controller.Tick(66, frame with { Area = "City" }, Send), "area transition reapplies without moving player");
Check(!controller.Tick(67, frame with { Alive = false }, Send) && !controller.Tick(68, frame, Send) && controller.Tick(70, frame, Send), "death defers; revival reapplies map policy only");
Check(!controller.Tick(71, frame with { Pawn = 456 }, Send) && controller.Tick(73, frame with { Pawn = 456 }, Send), "new local pawn starts fresh readiness interval");
controller.Reset();
Check(!controller.Tick(74, frame, Send) && controller.Tick(76, frame, Send), "disconnect clears transient timing");
Check(sent.All(s => s == "EnableFastTravel 0"), "transitions never issue teleport, discovery, or save commands");

var attempts = 0;
controller = new TravelController(settings with { ReadyDelaySeconds = 0 });
bool Flaky(string _) => ++attempts > 1;
Check(!controller.Tick(0, frame, Flaky) && attempts == 1, "failed native submission is retained for retry");
Check(!controller.Tick(.99, frame, Flaky) && controller.Tick(1, frame, Flaky) && attempts == 2, "retry uses one-second backoff");
foreach (var variant in new[] { settings with { Enabled = false }, settings with { MapFastTravel = MapTravel.Unmanaged } })
{
    controller = new TravelController(variant);
    Check(!controller.Tick(0, frame, _ => throw new Exception("Should not execute")), "disabled/unmanaged policy leaves native state alone");
}
controller = new TravelController(settings with { MapFastTravel = MapTravel.Allow, ReadyDelaySeconds = 0 });
Check(controller.Tick(0, frame, c => c == "EnableFastTravel 1"), "explicit allow only enables native map travel");
foreach (var invalidFrame in new[] { frame with { Pawn = 0 }, frame with { Area = "" }, frame with { Restored = false }, frame with { Alive = false } })
    Check(!controller.Tick(10, invalidFrame, _ => throw new Exception("Should not execute")), "unready frame defers without touching native state");

if (args.Length == 2 && args[0] == "--sdk") SdkContracts.Check(args[1], Check);
Console.WriteLine($"{count} automated checks passed. In-game/native/save persistence behavior is NOT tested by this runner.");
