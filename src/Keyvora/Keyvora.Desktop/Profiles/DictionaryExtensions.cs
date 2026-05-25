namespace Keyvora.Desktop.Profiles;

using System.Collections.Generic;

public static class DictionaryExtensions
{
    public static void Update<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        where TKey : notnull
    {
        dict[key] = value;
    }
}
