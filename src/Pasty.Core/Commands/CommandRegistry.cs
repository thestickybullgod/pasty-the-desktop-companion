namespace Pasty.Core.Commands;

/// <summary>A slash command. <c>Run</c> receives everything after the command name.</summary>
public sealed record PastyCommand(string Name, string Help, Func<string, Utterance> Run);

/// <summary>Slash commands typed into the ask box. Built-ins and plugins register here.</summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, PastyCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<PastyCommand> All => _commands.Values.OrderBy(c => c.Name);

    public void Register(PastyCommand command)
    {
        var name = command.Name.TrimStart('/');
        if (string.IsNullOrWhiteSpace(name) || name.Contains(' '))
            throw new ArgumentException($"Invalid command name '{command.Name}'.", nameof(command));
        _commands[name] = command with { Name = name };
    }

    public static bool IsCommand(string input) => input.TrimStart().StartsWith('/');

    public bool TryRun(string input, out Utterance result)
    {
        var trimmed = input.Trim().TrimStart('/');
        var space = trimmed.IndexOf(' ');
        var name = space < 0 ? trimmed : trimmed[..space];
        var args = space < 0 ? "" : trimmed[(space + 1)..].Trim();

        if (_commands.TryGetValue(name, out var command))
        {
            result = command.Run(args);
            return true;
        }

        result = new Utterance($"I don't know /{name}. Try /help.", PastyReaction.Annoyed);
        return false;
    }
}
