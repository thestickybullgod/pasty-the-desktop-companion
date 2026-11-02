namespace Pasty.Core.Animation;

/// <summary>
/// Procedural animation for Pasty, independent of any UI framework. The front end calls
/// <see cref="Update"/> on a timer and copies the resulting <see cref="PastyPose"/> onto its drawing.
/// Because Pasty is on screen all day, the animator also tells the front end how often it needs
/// frames (<see cref="RecommendedFps"/>): fewer when idle, very few when asleep.
/// </summary>
public sealed class PastyAnimator
{
    private enum Face { Neutral, Happy, Smug, Annoyed, Surprised, Listening, Thinking, Sleepy }

    private readonly record struct FaceShape(double BrowLA, double BrowLL, double BrowRA, double BrowRL, double Lid, double Blush, MouthShape Mouth);

    private readonly Random _random;
    private PastyPose _pose = PastyPose.Rest;
    private double _time;

    private PastyReaction _reaction;
    private double _reactionAge;
    private double _reactionLength;

    private double _nextBlink;
    private double _blinkAge = double.MaxValue;
    private double _lookX;
    private double _lookY;

    // Eyebrows do most of the acting: a quick double raise ("flash") when Pasty starts listening or
    // waves, and now and then while idle a single raised brow, the knowing look.
    private const double BrowFlashSeconds = 0.55;
    private const double BrowQuirkSeconds = 1.6;
    private double _browFlashAge = double.MaxValue;
    private double _nextQuirk = 20;
    private double _quirkAge = double.MaxValue;
    private int _quirkSide = 1;

    private const double BatFlightSeconds = 5;
    private double _nextBat = 12;
    private double _batAge = double.MaxValue;

    public PastyAnimator(Random? random = null)
    {
        _random = random ?? new Random();
        ScheduleBlink();
    }

    public PastyState State { get; private set; } = PastyState.Idle;

    public PastyPose Pose => _pose;

    /// <summary>True while a one-shot reaction is still playing.</summary>
    public bool IsReacting => _reaction != PastyReaction.None;

    /// <summary>Halloween Edition: lets the bat come out now and then.</summary>
    public bool BatEnabled { get; set; }

    private bool BatFlying => _batAge < BatFlightSeconds;

    /// <summary>
    /// Calmer animation for people who find motion distracting or uncomfortable: expressions, blinking
    /// and talking stay, but the body doesn't bob, hop, sway, squash or wave, and the bat stays home.
    /// </summary>
    public bool ReducedMotion { get; set; }

    public int RecommendedFps => ReducedMotion
        ? State switch { PastyState.Sleeping => 4, PastyState.Talking => 15, _ => IsReacting ? 15 : 10 }
        : IsReacting ? 40 : BatFlying ? 30 : State switch
        {
            PastyState.Sleeping => 8,
            PastyState.Idle => 20,
            _ => 30,
        };

    public void SetState(PastyState state)
    {
        if (state == State) return;
        State = state;
        if (state == PastyState.Sleeping) EndReaction();
        if (state == PastyState.Listening) _browFlashAge = 0;   // "I'm all ears"
    }

    public void React(PastyReaction reaction)
    {
        if (reaction == PastyReaction.None || State == PastyState.Sleeping) return;
        _reaction = reaction;
        _reactionAge = 0;
        _reactionLength = reaction switch
        {
            PastyReaction.Poked => 0.9,
            PastyReaction.Jump => 1.2,
            PastyReaction.Wave => 2.4,
            PastyReaction.Smug => 2.8,
            PastyReaction.Annoyed => 2.6,
            PastyReaction.Surprised => 1.5,
            PastyReaction.Ghost => 3.6,
            _ => 2.2,
        };
        if (reaction is PastyReaction.Wave or PastyReaction.Happy) _browFlashAge = 0;
        _quirkAge = double.MaxValue;   // a reaction takes over the face
    }

    /// <summary>Where the eyes should look when the state doesn't dictate it. Components roughly -1..1.</summary>
    public void LookToward(double x, double y)
    {
        var length = Math.Sqrt(x * x + y * y);
        if (length > 1) { x /= length; y /= length; }
        _lookX = x;
        _lookY = y;
    }

    public PastyPose Update(TimeSpan elapsed)
    {
        var dt = Math.Clamp(elapsed.TotalSeconds, 0, 0.25);
        _time += dt;

        if (IsReacting)
        {
            _reactionAge += dt;
            if (_reactionAge >= _reactionLength) EndReaction();
        }

        var face = Shape(CurrentFace());
        var p = _pose;

        // Facial features ease toward the target expression; the brows are a little quicker than the rest.
        var k = Approach(dt, State == PastyState.Sleeping ? 3 : 14);
        var kb = Approach(dt, State == PastyState.Sleeping ? 3 : 18);
        var (la, ll, ra, rl) = (face.BrowLA, face.BrowLL, face.BrowRA, face.BrowRL);
        AddBrowAccents(dt, ref la, ref ll, ref ra, ref rl);
        p.BrowLeftAngle = Lerp(p.BrowLeftAngle, la, kb);
        p.BrowLeftLift = Lerp(p.BrowLeftLift, ll, kb);
        p.BrowRightAngle = Lerp(p.BrowRightAngle, ra, kb);
        p.BrowRightLift = Lerp(p.BrowRightLift, rl, kb);
        p.Blush = Lerp(p.Blush, face.Blush, Approach(dt, 6));
        var lid = Lerp(p.EyeLid, face.Lid, k);
        p.Mouth = State == PastyState.Talking ? MouthShape.Open : face.Mouth;
        p.MouthOpen = State == PastyState.Talking ? 0.3 + 0.7 * Math.Abs(Math.Sin(_time * 12)) : 1;

        // Gaze.
        var (gx, gy) = State switch
        {
            PastyState.Thinking => (3.5, -4.0),
            PastyState.Listening or PastyState.Talking => (-1.0, -3.0),
            PastyState.Sleeping => (0.0, 2.0),
            _ => (_lookX * 4, _lookY * 3.5),
        };
        p.GazeX = Lerp(p.GazeX, gx, Approach(dt, 10));
        p.GazeY = Lerp(p.GazeY, gy, Approach(dt, 10));

        // Body: a gentle loop per state...
        double bob = 0, lean = 0, breathe = 0, zzz = 0;
        switch (State)
        {
            case PastyState.Idle: bob = -1.25 - 1.25 * Math.Sin(_time * Math.PI / 2.2); break;
            case PastyState.Listening: bob = -0.75 - 0.75 * Math.Sin(_time * Math.PI / 1.8); lean = -4; break;
            case PastyState.Thinking: lean = 3 * Math.Sin(_time * Math.PI / 1.5); break;
            case PastyState.Talking: bob = -1 - Math.Sin(_time * Math.PI / 0.7); break;
            case PastyState.Sleeping:
                lean = 5;
                breathe = 0.015 + 0.015 * Math.Sin(_time * Math.PI / 3.2);
                zzz = 0.5 + 0.4 * Math.Sin(_time * Math.PI / 1.6);
                break;
        }

        // ...plus whatever the current reaction adds on top.
        double squash = 0, arm = 0, capTilt = 0, capLift = 0, opacity = 1;
        if (IsReacting)
        {
            var t = _reactionAge;
            switch (_reaction)
            {
                case PastyReaction.Poked:
                    squash = 0.14 * Math.Exp(-5 * t) * Math.Cos(t * 16);
                    break;
                case PastyReaction.Jump: bob += Hop(t, 24); break;
                case PastyReaction.Happy: bob += Hop(t, 8); break;
                case PastyReaction.Surprised: bob += Hop(t, 10); break;
                case PastyReaction.Smug: lean += 4 * Envelope(t, _reactionLength); break;
                case PastyReaction.Annoyed: lean += 3 * Math.Sin(t * 45) * Math.Exp(-6 * t); break;
                case PastyReaction.Ghost:
                    // Fade to see-through, drift up, sway like a sheet in the wind. Slow and gentle: no flashing.
                    var haunt = Envelope(t, _reactionLength, rise: 0.6, fall: 0.8);
                    opacity = 1 - 0.6 * haunt;
                    bob += haunt * (-12 + 3 * Math.Sin(t * 4));
                    lean += haunt * 5 * Math.Sin(t * 2.5);
                    break;
                case PastyReaction.Wave:
                    var env = Envelope(t, 1.6, rise: 0.22, fall: 0.3);
                    arm = env * (-107 + 17 * Math.Sin(t * 18));
                    var tip = Envelope(t, 1.2, rise: 0.2, fall: 0.3);
                    capTilt = -14 * tip;
                    capLift = -7 * tip;
                    break;
            }
        }

        if (ReducedMotion)
        {
            // Keep the face; still the body. A wave becomes a raised hand, sleep's Zzz stops pulsing.
            bob = lean = squash = breathe = capTilt = capLift = 0;
            if (arm != 0) arm = -105;
            if (State == PastyState.Sleeping) zzz = 0.6;
            p.MouthOpen = State == PastyState.Talking ? 0.5 + 0.5 * Math.Abs(Math.Sin(_time * 5)) : 1;
        }

        p.BodyY = bob;
        p.Lean = Lerp(p.Lean, lean, !ReducedMotion && (State == PastyState.Thinking || _reaction == PastyReaction.Annoyed) ? 1 : Approach(dt, 8));
        p.Opacity = opacity;
        p.SquashX = 1 + squash;
        p.SquashY = 1 - squash + breathe;
        p.ShadowScale = 1 + Math.Min(0, bob) / 60;
        p.ArmAngle = arm;
        p.CapTilt = capTilt;
        p.CapLift = capLift;
        p.Zzz = Lerp(p.Zzz, zzz, Approach(dt, 4));

        UpdateBat(ref p, dt);

        // The stored pose keeps the expression's lid; blinking is layered on the returned copy only.
        p.EyeLid = lid;
        _pose = p;
        return p with { EyeLid = lid * BlinkFactor(dt) };
    }

    /// <summary>Every minute or so, a small bat loops around Pasty's head. Gentle and slow: no swooping at the user.</summary>
    private void UpdateBat(ref PastyPose p, double dt)
    {
        if (BatFlying)
        {
            _batAge += dt;
        }
        else if (BatEnabled && !ReducedMotion && State is PastyState.Idle or PastyState.Talking)
        {
            _nextBat -= dt;
            if (_nextBat <= 0)
            {
                _batAge = 0;
                _nextBat = 45 + _random.NextDouble() * 60;
            }
        }

        if (!BatFlying)
        {
            p.Bat = 0;
            return;
        }

        var t = _batAge / BatFlightSeconds;
        // A lazy figure-eight around the hat.
        p.BatX = 70 + 52 * Math.Sin(t * 2 * Math.PI);
        p.BatY = -4 + 12 * Math.Sin(t * 4 * Math.PI);
        p.BatFlap = Math.Abs(Math.Sin(_batAge * 14));
        p.Bat = Envelope(_batAge, BatFlightSeconds, rise: 0.5, fall: 0.5);
    }

    private Face CurrentFace()
    {
        if (IsReacting)
        {
            return _reaction switch
            {
                PastyReaction.Poked or PastyReaction.Surprised or PastyReaction.Ghost => Face.Surprised,
                PastyReaction.Smug => Face.Smug,
                PastyReaction.Annoyed => Face.Annoyed,
                _ => Face.Happy,
            };
        }

        return State switch
        {
            PastyState.Listening => Face.Listening,
            PastyState.Thinking => Face.Thinking,
            PastyState.Sleeping => Face.Sleepy,
            _ => Face.Neutral,
        };
    }

    // Brow angles: positive tips the left brow's inner end down (a scowl); the right brow mirrors it.
    private static FaceShape Shape(Face face) => face switch
    {
        Face.Happy => new(-3, -5, 3, -5, 0.85, 0.75, MouthShape.Smile),
        Face.Smug => new(-14, -10, 14, 3, 0.6, 0, MouthShape.Smirk),        // one brow way up: "oh really?"
        Face.Annoyed => new(22, 3, -22, 3, 0.72, 0, MouthShape.Flat),
        Face.Surprised => new(-6, -12, 6, -12, 1.12, 0, MouthShape.Open),   // high, worried arch
        Face.Listening => new(-4, -5, 4, -5, 1.05, 0, MouthShape.Smile),
        Face.Thinking => new(-10, -8, 10, 1, 0.9, 0, MouthShape.Flat),
        Face.Sleepy => new(0, 2, 0, 2, 0.08, 0, MouthShape.Flat),
        _ => new(0, 0, 0, 0, 1, 0, MouthShape.Smile),
    };

    /// <summary>Brow moments layered on the expression: the flash, emphasis while talking, the idle quirk and a ponder.</summary>
    private void AddBrowAccents(double dt, ref double la, ref double ll, ref double ra, ref double rl)
    {
        // Flash: two quick raises.
        if (_browFlashAge < BrowFlashSeconds)
        {
            _browFlashAge += dt;
            var flash = 7 * Math.Abs(Math.Sin(_browFlashAge / BrowFlashSeconds * 2 * Math.PI));
            ll -= flash;
            rl -= flash;
        }

        switch (State)
        {
            case PastyState.Talking when !IsReacting:
                // Emphasis now and then, like someone making a point.
                var beat = Math.Max(0, Math.Sin(_time * 2.6)) * Math.Max(0, Math.Sin(_time * 1.1 + 0.7));
                ll -= 5 * beat;
                rl -= 5 * beat;
                break;
            case PastyState.Thinking:
                // A slow ponder: the raised brow drifts.
                ll -= 2 * Math.Sin(_time * 1.7);
                la -= 3 * Math.Sin(_time * 1.7);
                break;
        }

        // Idle quirk: one brow up for a beat, then back. Not while reacting or doing anything else.
        if (State == PastyState.Idle && !IsReacting)
        {
            if (_quirkAge >= BrowQuirkSeconds)
            {
                _nextQuirk -= dt;
                if (_nextQuirk <= 0)
                {
                    _quirkAge = 0;
                    _quirkSide = _random.Next(2) == 0 ? -1 : 1;
                    _nextQuirk = 20 + _random.NextDouble() * 25;
                }
            }
            if (_quirkAge < BrowQuirkSeconds)
            {
                _quirkAge += dt;
                var q = Envelope(_quirkAge, BrowQuirkSeconds, rise: 0.18, fall: 0.35);
                if (_quirkSide < 0) { ll -= 9 * q; la -= 12 * q; ra += 4 * q; rl += 2 * q; }
                else { rl -= 9 * q; ra += 12 * q; la -= 4 * q; ll += 2 * q; }
            }
        }
        else
        {
            _quirkAge = double.MaxValue;
        }
    }

    private double BlinkFactor(double dt)
    {
        if (State == PastyState.Sleeping || State == PastyState.Thinking) return 1;

        _nextBlink -= dt;
        if (_nextBlink <= 0)
        {
            _blinkAge = 0;
            ScheduleBlink();
        }

        if (_blinkAge > 0.15) return 1;
        _blinkAge += dt;
        // Close over 70 ms, open over 80 ms.
        return _blinkAge < 0.07 ? Lerp(1, 0.08, _blinkAge / 0.07) : Lerp(0.08, 1, Math.Min(1, (_blinkAge - 0.07) / 0.08));
    }

    private void ScheduleBlink() => _nextBlink = 2.5 + _random.NextDouble() * 4;

    private void EndReaction()
    {
        _reaction = PastyReaction.None;
        _reactionAge = 0;
    }

    /// <summary>Upward hop with a small landing bounce. Negative is up.</summary>
    private static double Hop(double t, double height)
    {
        if (t < 0.36) return -height * 4 * (t / 0.36) * (1 - t / 0.36);
        if (t < 0.56) return -height * 0.12 * Math.Sin((t - 0.36) / 0.2 * Math.PI);
        return 0;
    }

    /// <summary>0→1→0 trapezoid over <paramref name="length"/> seconds.</summary>
    private static double Envelope(double t, double length, double rise = 0.3, double fall = 0.3)
    {
        if (t <= 0 || t >= length) return 0;
        if (t < rise) return t / rise;
        if (t > length - fall) return (length - t) / fall;
        return 1;
    }

    private static double Approach(double dt, double rate) => 1 - Math.Exp(-rate * dt);

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
