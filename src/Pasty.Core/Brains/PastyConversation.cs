using Pasty.Core.Commands;

namespace Pasty.Core.Brains;

/// <summary>
/// Routes what the user types: slash commands first, then the active brain, then the
/// offline brain if the active one fails. Pasty never goes silent because the network did.
/// </summary>
public sealed class PastyConversation(CommandRegistry commands, OfflineBrain offline)
{
    private static readonly TimeSpan ForgetAfter = TimeSpan.FromMinutes(30);
    private DateTime _lastExchange = DateTime.MinValue;

    /// <summary>The brain used for free-form text. Null means offline.</summary>
    public IPastyBrain? Brain { get; set; }

    public IPastyBrain ActiveBrain => Brain ?? offline;

    /// <summary>Checked before any brain: easter eggs and the like. Return null to pass.</summary>
    public List<Func<string, Utterance?>> Interceptors { get; } = [];

    public async Task<Utterance> AskAsync(string input, BrainContext context, CancellationToken cancellationToken)
    {
        if (CommandRegistry.IsCommand(input))
        {
            commands.TryRun(input, out var result);
            return result;
        }

        foreach (var intercept in Interceptors)
        {
            if (intercept(input) is { } special)
                return special;
        }

        // A companion that runs all day shouldn't drag a morning chat into the evening.
        if (context.LocalTime - _lastExchange > ForgetAfter)
            Brain?.ResetConversation();
        _lastExchange = context.LocalTime;

        if (Brain is null)
            return await offline.ReplyAsync(input, context, cancellationToken);

        try
        {
            return await Brain.ReplyAsync(input, context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (BrainException ex)
        {
            return new Utterance(ex.Message, PastyReaction.Annoyed);
        }
        catch (Exception)
        {
            var fallback = await offline.ReplyAsync(input, context, cancellationToken);
            return fallback with { Text = "(My AI brain hiccuped, so you get the offline me.) " + fallback.Text };
        }
    }

    public void Reset() => ActiveBrain.ResetConversation();
}
