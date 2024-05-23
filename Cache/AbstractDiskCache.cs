using System.Security.Cryptography;
using System.Text;

namespace CacheComms;

public abstract class AbstractDiskCache
{
    public const string DefaultNamespace = "default";

    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(2);

    /// <summary>
    /// How log should a cache entry be valid for?
    /// </summary>
    public TimeSpan EntryLifespan { get; set; } = DefaultLifetime;
    protected object locker;

    private string Namespace;

    public AbstractDiskCache(string nameSpace)
    {
        Namespace= nameSpace;
        locker = new object();
    }

    public void Clear(string identifier)
    {
        //get the key
        var cacheKey = GetCacheKey(identifier);
        var filepath = GetFilePath(cacheKey);
        try
        {
            File.Delete(filepath);
        }
        catch (Exception)
        {
        }
    }

    protected string GetFilePath(string identifier)
    {
        var cacheKey = GetCacheKey(identifier);
        return Path.Combine(Path.GetTempPath(), $"{cacheKey}.diskcache-{Namespace}");
    }

    protected bool IsCacheEntryValid(string filepath)
    {
        bool isValid = false;

        try
        {
            isValid = (File.GetLastWriteTime(filepath) >= DateTime.Now.Subtract(EntryLifespan));
            if(!isValid)
            {
                //even if the delete fails, we still return if its valid
                File.Delete(filepath);
            }
        }
        catch (Exception)
        {
        }
        return isValid;
    }

    private string GetCacheKey(string identifier)
        => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(identifier)));
}
