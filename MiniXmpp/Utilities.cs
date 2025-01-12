using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MiniXmpp;

public static class Utilities
{
    public static void ThrowIfNull
    (
        [NotNull]
        this object obj,

        [CallerArgumentExpression(nameof(obj))]
        string? expression = default
    )
    {
        if (obj is null)
            throw new ArgumentNullException(expression);
    }

    public static void ThrowIfNullOrEmpty(this string? s, [CallerArgumentExpression(nameof(s))] string? expression = default)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException(default, expression);
    }

    public static void ThrowIfNullOrWhiteSpace(this string? s, [CallerArgumentExpression(nameof(s))] string? expression = default)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException(default, expression);
    }

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> callback)
    {
        foreach (var item in enumerable)
            callback(item);

        return enumerable;
    }

    public static IEnumerable<U> Map<T, U>(this IEnumerable<T> enumerable, Func<T, U> mapping)
    {
        foreach (var item in enumerable)
            yield return mapping(item);
    }

    public static IEnumerable<U> MapWhen<T, U>(this IEnumerable<T> source, Func<T, bool> condition, Func<T, U> mapping)
    {
        foreach (var item in source)
        {
            if (condition(item))
                yield return mapping(item);
        }
    }

    public static byte[] GetBytes(this string s, Encoding? encoding = default)
        => (encoding ?? Encoding.UTF8).GetBytes(s);

    public static string GetString(this byte[] s, Encoding? encoding = default)
        => (encoding ?? Encoding.UTF8).GetString(s);

    readonly struct HashAlgorithmEntry
    {
        public HashDataFunction HashData { get; init; }
#if NET7_0_OR_GREATER
        public HashStreamFunction HashStream { get; init; }
#endif
    }

    static readonly Dictionary<HashAlgorithmName, HashAlgorithmEntry> s_HashAlgorithms = new();

    static Utilities()
    {
#if NET8_0_OR_GREATER
        InstallHashAlgorithm(HashAlgorithmName.MD5, MD5.HashData, MD5.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA1, SHA1.HashData, SHA1.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA256, SHA256.HashData, SHA256.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA384, SHA384.HashData, SHA384.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA3_256, SHA3_256.HashData, SHA3_256.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA3_384, SHA3_384.HashData, SHA3_384.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA3_512, SHA3_512.HashData, SHA3_512.HashData);
#else
        InstallHashAlgorithm(HashAlgorithmName.MD5, MD5.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA1, SHA1.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA256, SHA256.HashData);
        InstallHashAlgorithm(HashAlgorithmName.SHA384, SHA384.HashData);
#endif
    }

#if NET8_0_OR_GREATER
    public static void InstallHashAlgorithm(HashAlgorithmName name, HashDataFunction hashData, HashStreamFunction hashStream, bool replaceExisting = true)
    {
        hashData.ThrowIfNull();
        hashStream.ThrowIfNull();

        lock (s_HashAlgorithms)
        {
            if (s_HashAlgorithms.ContainsKey(name) && !replaceExisting)
                return;

            s_HashAlgorithms[name] = new HashAlgorithmEntry
            {
                HashData = hashData,
                HashStream = hashStream
            };
        }
    }
#else
    public static void InstallHashAlgorithm(HashAlgorithmName name, HashDataFunction hashData, bool replaceExisting = true)
    {
        hashData.ThrowIfNull();

        lock (s_HashAlgorithms)
        {
            if (s_HashAlgorithms.ContainsKey(name) && !replaceExisting)
                return;

            s_HashAlgorithms[name] = new HashAlgorithmEntry
            {
                HashData = hashData,
            };
        }
    }
#endif

    public static byte[] GetHash(byte[] buffer, HashAlgorithmName algorithm)
    {
        lock (s_HashAlgorithms)
            return s_HashAlgorithms[algorithm].HashData(buffer);
    }

    public static string GetHash(this string text, HashAlgorithmName algorithm)
        => Convert.ToHexString(GetHash(text.GetBytes(), algorithm));

#if NET8_0_OR_GREATER

    public static byte[] GetHash(Stream stream, HashAlgorithmName algorithm)
    {
        lock (s_HashAlgorithms)
            return s_HashAlgorithms[algorithm].HashStream(stream);
    }

#endif
}

public delegate byte[] HashDataFunction(byte[] buffer);

#if NET8_0_OR_GREATER
public delegate byte[] HashStreamFunction(Stream source);
#endif
