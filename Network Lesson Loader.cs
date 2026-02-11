using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NetworkLessonLoader : MonoBehaviour
{
    private const string rootUrl = "https://raw.githubusercontent.com/Baray-Ter/Aegean-Trivia-Question-Data/main/OlymposGodsData/";
    private const string manifestName = "Manifest.json";

    [Header("UI References")]
    [SerializeField] private Button downloadButton;

    [Serializable] public class Manifest { public List<ManifestEntry> files; }
    [Serializable] public class ManifestEntry { public string fileName; public int version; }
    [Serializable] public class QuestionList { public List<Question> questions; }
    [Serializable] public class Question { public string[] image; }

    public delegate void DownloadProgressCallback(float progress);
    public static event DownloadProgressCallback OnDownloadProgressCompleted;

    void Start()
    {
        if (downloadButton == null) downloadButton = GetComponent<Button>();
        downloadButton.onClick.AddListener(() => _ = StartDownloadRoutine());
    }

    private async Task StartDownloadRoutine()
    {
        downloadButton.interactable = false;

        try
        {
            string webManifestJson = await DownloadTextAsync(rootUrl + manifestName);
            Manifest webManifest = JsonUtility.FromJson<Manifest>(webManifestJson);

            string localManifestPath = Path.Combine(Application.persistentDataPath, manifestName);
            Manifest localManifest = new Manifest() { files = new List<ManifestEntry>() };
            
            if (File.Exists(localManifestPath))
            {
                try
                {
                    localManifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(localManifestPath)) ?? new Manifest();
                    if(localManifest.files == null) localManifest.files = new List<ManifestEntry>();
                }
                catch { }
            }

            foreach (var webFile in webManifest.files)
            {
                var localFile = localManifest.files.Find(x => x.fileName == webFile.fileName);

                if (localFile == null || webFile.version > localFile.version)
                {
                    await DownloadAndProcessFile(webFile.fileName);

                    if (localFile == null)
                    {
                        localManifest.files.Add(webFile);
                    }
                    else
                    {
                        localFile.version = webFile.version;
                    }
                    
                    await File.WriteAllTextAsync(localManifestPath, JsonUtility.ToJson(localManifest));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Download Process Failed: {e.Message}");
        }
        finally
        {
            downloadButton.interactable = true;
        }
    }

    private async Task DownloadAndProcessFile(string fileName)
    {
        string url = rootUrl + fileName;
        string jsonResult = await DownloadTextAsync(url);

        string localPath = Path.Combine(Application.persistentDataPath, fileName);
        await File.WriteAllTextAsync(localPath, jsonResult);

        await ProcessAndDownloadImages(jsonResult);
    }

    private async Task ProcessAndDownloadImages(string jsonData)
    {
        QuestionList data = JsonUtility.FromJson<QuestionList>(jsonData);
        if (data == null || data.questions == null) return;

        var uniqueImages = data.questions
            .Where(q => q.image != null)
            .SelectMany(q => q.image)
            .Distinct()
            .ToList();

        List<Task> downloadTasks = new List<Task>();
        
        foreach (string relativePath in uniqueImages)
        {
            string fullWebUrl = rootUrl + relativePath;
            downloadTasks.Add(DownloadImageAsync(fullWebUrl, relativePath));
        }

        if (downloadTasks.Count > 0) await Task.WhenAll(downloadTasks);
    }

    private async Task<string> DownloadTextAsync(string url)
    {
        Debug.Log($"<color=yellow>Attempting to download: {url}</color>");
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);

            return req.downloadHandler.text;
        }
    }

    private async Task DownloadImageAsync(string url, string relativePath)
    {
        string localPath = Path.Combine(Application.persistentDataPath, relativePath);

        if (File.Exists(localPath)) return;

        string directoryName = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result == UnityWebRequest.Result.Success)
            {
                await File.WriteAllBytesAsync(localPath, req.downloadHandler.data);
            }
        }
    }
}