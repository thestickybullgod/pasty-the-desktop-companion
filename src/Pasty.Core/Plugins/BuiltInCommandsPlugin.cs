using System.Text;
using Pasty.Core.Commands;
using Pasty.Core.Personality;

namespace Pasty.Core.Plugins;

/// <summary>Pasty's own slash commands, written as a plugin to keep the plugin API honest.</summary>
public sealed class BuiltInCommandsPlugin(PersonalityEngine personality, Random? random = null) : IPastyPlugin
{
    private readonly Random _random = random ?? Random.Shared;

    public string Id => "pasty.builtin";

    public string DisplayName => "Built-in commands";

    public void Initialize(IPastyHost host)
    {
        var commands = host.Commands;

        commands.Register(new("help", "List everything I can do", _ =>
        {
            var sb = new StringBuilder("Here's what I know:\n");
            foreach (var c in commands.All)
                sb.Append($"/{c.Name}: {c.Help}\n");
            return new Utterance(sb.ToString().TrimEnd(), PastyReaction.Happy);
        }));

        commands.Register(new("joke", "Tell a (computer) joke", _ => personality.Joke()));

        commands.Register(new("time", "What time is it?", _ =>
            new Utterance($"It's {DateTime.Now:t} on {DateTime.Now:dddd}.")));

        commands.Register(new("flip", "Flip a coin", _ =>
            new Utterance(_random.Next(2) == 0 ? "Heads!" : "Tails!", PastyReaction.Jump)));

        commands.Register(new("roll", "Roll dice, e.g. /roll 2d6", args => Roll(args)));

        commands.Register(new("tip", "A random Windows tip", _ => personality.IdleRemark()));

        commands.Register(new("boo", "Get (gently) spooked 👻", _ => personality.Boo()));

        commands.Register(new("graveyard", "Pay respects to retired desktop assistants 🪦", _ => personality.Graveyard()));

        commands.Register(new("trickortreat", "🎃 Trick or treat?", _ => personality.TrickOrTreat()));
    }

    private Utterance Roll(string args)
    {
        var count = 1;
        var sides = 6;
        var spec = args.ToLowerInvariant().Split('d', 2);
        if (spec.Length == 2)
        {
            if (spec[0].Length > 0 && !int.TryParse(spec[0], out count)) count = 1;
            if (!int.TryParse(spec[1], out sides)) sides = 6;
        }
        else if (int.TryParse(args, out var s))
        {
            sides = s;
        }

        count = Math.Clamp(count, 1, 20);
        sides = Math.Clamp(sides, 2, 1000);
        var rolls = Enumerable.Range(0, count).Select(_ => _random.Next(1, sides + 1)).ToArray();
        var text = count == 1
            ? $"🎲 You rolled a {rolls[0]}."
            : $"🎲 {string.Join(" + ", rolls)} = {rolls.Sum()}";
        return new Utterance(text, PastyReaction.Jump);
    }
}
