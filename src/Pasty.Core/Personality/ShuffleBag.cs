namespace Pasty.Core.Personality;

/// <summary>
/// Hands out items in a random order without repeats until the bag is empty.
/// Pasty runs all day; hearing the same joke twice in an hour is how companions get uninstalled.
/// </summary>
public sealed class ShuffleBag<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly Random _random;
    private readonly List<T> _remaining = [];
    private T? _last;

    public ShuffleBag(IReadOnlyList<T> items, Random random)
    {
        if (items.Count == 0) throw new ArgumentException("A shuffle bag needs at least one item.", nameof(items));
        _items = items;
        _random = random;
    }

    public T Next()
    {
        if (_remaining.Count == 0)
        {
            _remaining.AddRange(_items);
            // Don't let a refill start with the item we just said.
            if (_remaining.Count > 1 && _last is not null)
                _remaining.Remove(_last);
        }

        var index = _random.Next(_remaining.Count);
        var item = _remaining[index];
        _remaining.RemoveAt(index);
        _last = item;
        return item;
    }
}
