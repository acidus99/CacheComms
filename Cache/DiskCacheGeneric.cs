using System.Text.Json;

namespace CacheComms;

public class DiskCache<T> : AbstractDiskCache
{
    public DiskCache(string nameSpace)
        : base(nameSpace)
    {
    }

    public T? Get(string identifier)
    {
        //get the key
        var filepath = GetFilePath(identifier);
        try
        {
            if (IsCacheEntryValid(filepath))
            {
                lock (locker)
                {
                    return JsonSerializer.Deserialize<T>(File.ReadAllText(filepath));
                }
            }
        }
        catch (Exception)
        {
        }
        return default;
    }

    public bool Set(string identifier, T t)
    {
        //get the key
        var filepath = GetFilePath(identifier);
        try
        {
            string contents = JsonSerializer.Serialize<T>(t);
            lock (locker)
            {
                File.WriteAllText(filepath, contents);
            }
            return true;
        }
        catch (Exception)
        {
        }
        return false;
    }
}
