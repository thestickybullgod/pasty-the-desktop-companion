using Pasty.Core.Brains;
using Pasty.Core.Commands;
using Pasty.Core.Context;
using Pasty.Core.Personality;
using Pasty.Core.Plugins;
using Pasty.Core.Settings;

namespace Pasty.Core.Tests;

public class ClassifierTests
{
    [Theory]
    [InlineData("https://example.com/a?b=c", ClipboardClassifier.Url)]
    [InlineData("someone@example.com", ClipboardClassifier.Email)]
    [InlineData("1,234.56", ClipboardClassifier.Number)]
    [InlineData("public int X() { return 1; }", ClipboardClassifier.Code)]
    [InlineData("just a sentence, nothing special", ClipboardClassifier.Text)]
    public void Clipboard_text_is_reduced_to_a_category(string text, string expected) =>
        Assert.Equal(expected, ClipboardClassifier.Classify(text));

    [Theory]
    [InlineData("devenv", AppClassifier.Code)]
    [InlineData("WINWORD", AppClassifier.Writing)]
    [InlineData("WindowsTerminal", AppClassifier.Terminal)]
    [InlineData("some-random-app", AppClassifier.Other)]
    public void Processes_are_reduced_to_a_category(string process, string expected) =>
        Assert.Equal(expected, AppClassifier.Classify(process));
}

public class ContextHubTests
{
    private sealed class FakeSource : IContextSource
    {
        public bool Running;
        public string Id => "fake";
        public string Description => "fake";
        public event Action<ContextSignal>? Signal;
        public bool IsConsented(PastySettings settings) => settings.WatchClipboard;
        public void Start() => Running = true;
        public void Stop() => Running = false;
        public void Raise() => Signal?.Invoke(new ContextSignal(ContextKind.Clipboard, "text"));
        public void Dispose() => Stop();
    }

    [Fact]
    public void Sources_only_run_with_consent()
    {
        var hub = new ContextHub();
        var source = new FakeSource();
        hub.Register(source);

        hub.Apply(new PastySettings());
        Assert.False(source.Running);

        hub.Apply(new PastySettings { WatchClipboard = true });
        Assert.True(source.Running);

        hub.Apply(new PastySettings { WatchClipboard = false });
        Assert.False(source.Running);
    }

    [Fact]
    public void Signals_are_forwarded()
    {
        var hub = new ContextHub();
        var source = new FakeSource();
        hub.Register(source);
        ContextSignal? received = null;
        hub.Signal += s => received = s;
        source.Raise();
        Assert.Equal("text", received?.Category);
    }
}

public class ConversationTests
{
    private sealed class FailingBrain(Exception error) : IPastyBrain
    {
        public string Name => "Failing";
        public bool IsRemote => true;
        public Task<Utterance> ReplyAsync(string userText, BrainContext context, CancellationToken cancellationToken) => throw error;
        public void ResetConversation() { }
    }

    private sealed class Host : IPastyHost
    {
        public CommandRegistry Commands { get; } = new();
        public void Say(Utterance utterance) { }
    }

    private static (PastyConversation, CommandRegistry) Create()
    {
        var personality = new PersonalityEngine(new PastySettings(), new Random(3));
        var host = new Host();
        new BuiltInCommandsPlugin(personality, new Random(3)).Initialize(host);
        return (new PastyConversation(host.Commands, new OfflineBrain(personality)), host.Commands);
    }

    private static readonly BrainContext Now = new(new DateTime(2026, 10, 31, 12, 0, 0));

    [Fact]
    public async Task Slash_commands_bypass_the_brain()
    {
        var (conversation, _) = Create();
        conversation.Brain = new FailingBrain(new InvalidOperationException());
        var reply = await conversation.AskAsync("/flip", Now, CancellationToken.None);
        Assert.Contains(reply.Text, new[] { "Heads!", "Tails!" });
    }

    [Fact]
    public async Task Help_lists_registered_commands()
    {
        var (conversation, _) = Create();
        var reply = await conversation.AskAsync("/help", Now, CancellationToken.None);
        Assert.Contains("/joke", reply.Text);
        Assert.Contains("/roll", reply.Text);
    }

    [Fact]
    public async Task Unexpected_brain_failure_falls_back_to_offline()
    {
        var (conversation, _) = Create();
        conversation.Brain = new FailingBrain(new HttpRequestException("network down"));
        var reply = await conversation.AskAsync("hello", Now, CancellationToken.None);
        Assert.StartsWith("(My AI brain hiccuped", reply.Text);
    }

    [Fact]
    public async Task Brain_exceptions_are_spoken_in_character()
    {
        var (conversation, _) = Create();
        conversation.Brain = new FailingBrain(new BrainException("Bad key."));
        var reply = await conversation.AskAsync("hello", Now, CancellationToken.None);
        Assert.Equal("Bad key.", reply.Text);
    }

    [Fact]
    public async Task Offline_brain_knows_who_it_is()
    {
        var (conversation, _) = Create();
        var reply = await conversation.AskAsync("who are you?", Now, CancellationToken.None);
        Assert.Contains("Pasty", reply.Text);
    }

    [Fact]
    public void Roll_clamps_silly_input()
    {
        var (_, commands) = Create();
        Assert.True(commands.TryRun("/roll 999d99999", out var reply));
        Assert.Contains("=", reply.Text);
    }
}

public class SettingsStoreTests
{
    [Fact]
    public void Round_trips_and_survives_corruption()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pasty-test-{Guid.NewGuid():N}.json");
        try
        {
            var store = new SettingsStore(path);
            store.Save(new PastySettings { Sarcasm = 87, Chattiness = Chattiness.Relentless, WatchClipboard = true });
            var loaded = store.Load();
            Assert.Equal(87, loaded.Sarcasm);
            Assert.Equal(Chattiness.Relentless, loaded.Chattiness);
            Assert.True(loaded.WatchClipboard);

            File.WriteAllText(path, "{ not json");
            Assert.Equal(50, store.Load().Sarcasm);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Privacy_sensitive_settings_default_to_off()
    {
        var s = new PastySettings();
        Assert.False(s.WatchActiveApp);
        Assert.False(s.WatchClipboard);
        Assert.False(s.ShareContextWithAi);
        Assert.False(s.EnableExternalPlugins);
        Assert.False(s.StartWithWindows);
        Assert.Equal(BrainKind.Offline, s.Brain);
    }
}
