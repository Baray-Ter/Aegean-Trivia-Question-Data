using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Manifest   
{
    public List<ManifestEntry> files;
}

[System.Serializable]
public class ManifestEntry
{
    public string fileName;
    public int version;
}
