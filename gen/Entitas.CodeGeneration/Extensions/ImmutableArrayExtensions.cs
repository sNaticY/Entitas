using System.Collections.Immutable;

namespace Entitas.CodeGeneration.Extensions;

public static class ImmutableArrayExtensions
{
    public static int GetSequenceHashCode<T>(this ImmutableArray<T> array)
    {
        if (array.IsDefault)
            return 0;

        unchecked
        {
            int hash = 17;
            foreach (var item in array)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    // Structural equality for dictionaries whose values are ImmutableArrays —
    // ImmutableDictionary itself uses reference equality, which silently busts
    // the incremental generator pipeline cache.
    public static bool DictionaryValueEquals<TKey, TValue>(
        this ImmutableDictionary<TKey, ImmutableArray<TValue>> left,
        ImmutableDictionary<TKey, ImmutableArray<TValue>> right)
        where TKey : notnull
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null || left.Count != right.Count)
            return false;

        foreach (var kvp in left)
        {
            if (!right.TryGetValue(kvp.Key, out var otherValue))
                return false;
            if (!kvp.Value.SequenceEqual(otherValue))
                return false;
        }
        return true;
    }

    public static int GetDictionaryValueHashCode<TKey, TValue>(
        this ImmutableDictionary<TKey, ImmutableArray<TValue>> dictionary)
        where TKey : notnull
    {
        if (dictionary is null)
            return 0;

        unchecked
        {
            int hash = 17;
            foreach (var kvp in dictionary)
                hash ^= kvp.Key.GetHashCode() * 31 + kvp.Value.GetSequenceHashCode();
            return hash;
        }
    }
}