using System;
using System.Collections.Generic;
using System.IO;

namespace Ameath.DesktopPet.Managers;

public sealed class AssetManager
{
    private readonly string _assetRoot;

    public AssetManager(string assetRoot)
    {
        _assetRoot = assetRoot;
    }

    public IReadOnlyList<string> GetAssetPaths(params string[] fileNames)
    {
        var results = new List<string>();
        foreach (var fileName in fileNames)
        {
            var path = Path.Combine(_assetRoot, fileName);
            if (File.Exists(path))
            {
                results.Add(path);
            }
        }

        return results;
    }
}
