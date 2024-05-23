namespace CacheComms;

public class DiskCache : AbstractDiskCache
{

    public DiskCache(string nameSpace = AbstractDiskCache.DefaultNamespace)
        : base(nameSpace)
    {
    }

    public string? GetAsString(string identifier)
    {
        var filepath = GetFilePath(identifier);
        try
        {
            if (IsCacheEntryValid(filepath))
            {
                lock (locker)
                {
                    return File.ReadAllText(filepath);
                }
            }
        }
        catch (Exception)
        {
        }
        return null;
    }

    public byte[]? GetAsBytes(string identifier)
    {
        var filepath = GetFilePath(identifier);
        try
        {
            if (IsCacheEntryValid(filepath))
            {
                lock (locker)
                {
                    return File.ReadAllBytes(filepath);
                }
            }
        }
        catch (Exception)
        {
        }
        return null;
    }

    public bool Set(string identifier, string contents)
    {
        var filepath = GetFilePath(identifier);
        try
        {
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

    public bool Set(string identifier, byte[] bytes)
    {
        var filepath = GetFilePath(identifier);
        try
        {
            lock (locker)
            {
                File.WriteAllBytes(filepath, bytes);
            }
            return true;
        }
        catch (Exception)
        {
        }
        return false;
    }
}