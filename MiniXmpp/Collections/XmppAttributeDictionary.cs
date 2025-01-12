using System.Collections;

namespace MiniXmpp.Collections;

public class XmppAttributeDictionary : IEnumerable<KeyValuePair<XmppName, string>>
{
    readonly Dictionary<XmppName, string> _dictionary = new();

    public int Count
    {
        get
        {
            lock (_dictionary)
                return _dictionary.Count;
        }
    }

    public IEnumerable<XmppName> Keys
    {
        get
        {
            lock (_dictionary)
                return _dictionary.Keys.ToArray();
        }
    }

    public IEnumerable<string> Values
    {
        get
        {
            lock (_dictionary)
                return _dictionary.Values.ToArray();
        }
    }

    public string? this[XmppName key]
    {
        get
        {
            lock (_dictionary)
                return _dictionary.GetValueOrDefault(key);
        }
        set
        {
            lock (_dictionary)
            {
                if (value == null)
                    _dictionary.Remove(key);
                else
                    _dictionary[key] = value;
            }
        }
    }

    public void Clear()
    {
        lock (_dictionary)
            _dictionary.Clear();
    }

    public void Add(XmppName key, string value)
    {
        lock (_dictionary)
            _dictionary[key] = value;
    }

    public bool Contains(XmppName key)
    {
        lock (_dictionary)
            return _dictionary.ContainsKey(key);
    }

    public bool TryRemove(XmppName key, out string? value)
    {
        lock (_dictionary)
            return _dictionary.Remove(key, out value);
    }

    public bool Remove(XmppName key)
    {
        lock (_dictionary)
            return _dictionary.Remove(key);
    }

#if NET9_0_OR_GREATER
    public void RemoveAll(params IEnumerable<XmppName> keys)
    {
        lock (_dictionary)
        {
            foreach (var key in keys)
                _dictionary.Remove(key);
        }
    }
#else
    public void RemoveAll(IEnumerable<XmppName> keys)
    {
        lock (_dictionary)
        {
            foreach (var key in keys)
                _dictionary.Remove(key);
        }
    }

    public void RemoveAll(params XmppName[] keys)
    {
        lock (_dictionary)
        {
            foreach (var key in keys)
                _dictionary.Remove(key);
        }
    }
#endif

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<KeyValuePair<XmppName, string>> GetEnumerator()
    {
        lock (_dictionary)
        {
            foreach (var entry in _dictionary)
                yield return entry;
        }
    }
}
