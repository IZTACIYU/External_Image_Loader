using Directory = System.IO.Directory; // [요청하신 사항] 최상단 추가
using ImageMagick;
using MetadataExtractor;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ImageLoader
{
    public class WebUiHost : IDisposable
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private readonly string _url = "http://localhost:5000/";
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private ConcurrentDictionary<string, byte[]> _imageCache = new();
        private ConcurrentQueue<object> _eventQueue = new();
        private bool _isRunning = false;

        private LoaderConfig _currentConfig = new();
        private PathConfig _pathConfig = new();

        public void Start()
        {
            LoadConfigs();

            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            LogConsole($"Web UI started at {_url}");
            try { Process.Start(new ProcessStartInfo { FileName = _url, UseShellExecute = true }); } catch { }

            Task.Run(() => ListenLoop());
        }

        private void LogConsole(string msg, bool isError = false)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = isError ? ConsoleColor.Red : ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
            Console.ForegroundColor = originalColor;
        }

        private void LoadConfigs()
        {
            try
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var ldPath = Path.Combine(exeDir, "ImgLdConfig.json");
                var ptPath = Path.Combine(exeDir, "PathConfig.json");

                if (File.Exists(ldPath))
                {
                    var loaded = JsonSerializer.Deserialize<LoaderConfig>(File.ReadAllText(ldPath));
                    if (loaded != null) _currentConfig = loaded;
                }
                if (File.Exists(ptPath))
                {
                    var loaded = JsonSerializer.Deserialize<PathConfig>(File.ReadAllText(ptPath));
                    if (loaded != null) _pathConfig = loaded;
                }
                LogConsole("Config loaded from files.");
            }
            catch (Exception ex) { LogConsole($"Failed to load config: {ex.Message}", true); }
        }

        private async Task ListenLoop()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch { break; }
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var resp = ctx.Response;
            var path = req.Url.AbsolutePath.ToLower();

            try
            {
                if (req.HttpMethod == "GET" && path == "/") ServeHtml(resp);
                else if (req.HttpMethod == "GET" && path.StartsWith("/image/")) ServeImage(resp, path.Replace("/image/", ""));
                else if (req.HttpMethod == "GET" && path == "/api/updates") ServeUpdates(resp);
                else if (req.HttpMethod == "GET" && path == "/api/config/load") HandleLoadConfig(resp);
                else if (req.HttpMethod == "POST" && path == "/api/start") await HandleStart(req, resp);
                else if (req.HttpMethod == "POST" && path == "/api/stop") { StopFetching(); LogConsole("Stopped by user."); RespondJson(resp, new { status = "stopped" }); }
                else if (req.HttpMethod == "POST" && path == "/api/save") await HandleSave(req, resp);
                else if (req.HttpMethod == "POST" && path == "/api/config/save") await HandleSaveConfig(req, resp);
                else { resp.StatusCode = 404; resp.Close(); }
            }
            catch (Exception ex)
            {
                LogConsole($"Request Error: {ex.Message}", true);
                resp.StatusCode = 500;
                resp.Close();
            }
        }

        // --- Core Logic ---
        private async Task HandleStart(HttpListenerRequest req, HttpListenerResponse resp)
        {
            if (_isRunning) { RespondJson(resp, new { error = "Already running" }); return; }

            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var data = JsonSerializer.Deserialize<JsonElement>(await reader.ReadToEndAsync());

            string baseUrl = data.GetProperty("baseUrl").GetString();
            string code = data.GetProperty("code").GetString();
            string names = data.GetProperty("names").GetString();
            string situations = data.GetProperty("situations").GetString();
            int startNum = data.GetProperty("startNum").GetInt32();
            int endNum = data.GetProperty("endNum").GetInt32();
            int parallel = data.GetProperty("parallel").GetInt32();

            var jobs = GenerateJobs(baseUrl, code, names, situations, startNum, endNum);

            if (jobs.Count == 0) { RespondJson(resp, new { error = "No jobs generated" }); return; }

            _cts = new CancellationTokenSource();
            _isRunning = true;
            _imageCache.Clear();

            LogConsole($"Starting {jobs.Count} jobs with {parallel} threads...");
            _ = Task.Run(() => FetchAllAsync(jobs, parallel, _cts.Token));

            RespondJson(resp, new { status = "started", jobCount = jobs.Count });
        }

        private async Task HandleSaveConfig(HttpListenerRequest req, HttpListenerResponse resp)
        {
            try
            {
                using var reader = new StreamReader(req.InputStream);
                var data = JsonSerializer.Deserialize<JsonElement>(await reader.ReadToEndAsync());

                var loaderConfig = new LoaderConfig
                {
                    BaseURL = GetJsonString(data, "baseUrl"),
                    Code = GetJsonString(data, "code"),
                    NameToken = GetJsonString(data, "names"),
                    SituationToken = GetJsonString(data, "situations"),
                    St_Num = GetJsonInt(data, "startNum"),
                    En_Num = GetJsonInt(data, "endNum"),
                    Multi_Call_Num = GetJsonInt(data, "parallel")
                };
                var pathConfig = new PathConfig
                {
                    InputPath = GetJsonString(data, "inputPath"),
                    OutputPath = GetJsonString(data, "outputPath")
                };

                _currentConfig = loaderConfig;
                _pathConfig = pathConfig;

                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var opt = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(Path.Combine(exeDir, "ImgLdConfig.json"), JsonSerializer.Serialize(loaderConfig, opt));
                File.WriteAllText(Path.Combine(exeDir, "PathConfig.json"), JsonSerializer.Serialize(pathConfig, opt));

                LogConsole("Settings saved to disk.");
                RespondJson(resp, new { status = "ok", message = "Configuration saved successfully." });
            }
            catch (Exception ex) { LogConsole($"Error saving config: {ex.Message}", true); RespondJson(resp, new { status = "error", message = ex.Message }); }
        }

        private void HandleLoadConfig(HttpListenerResponse resp)
        {
            var responseData = new
            {
                baseUrl = _currentConfig.BaseURL ?? "",
                code = _currentConfig.Code ?? "",
                names = _currentConfig.NameToken ?? "",
                situations = _currentConfig.SituationToken ?? "",
                startNum = _currentConfig.St_Num,
                endNum = _currentConfig.En_Num,
                parallel = _currentConfig.Multi_Call_Num == 0 ? 4 : _currentConfig.Multi_Call_Num,
                inputPath = _pathConfig.InputPath ?? "",
                outputPath = _pathConfig.OutputPath ?? ""
            };
            RespondJson(resp, responseData);
        }

        private async Task FetchAllAsync(List<Job> jobs, int parallelism, CancellationToken ct)
        {
            var throttler = new SemaphoreSlim(parallelism);
            int done = 0;
            try
            {
                var tasks = jobs.Select(async job =>
                {
                    await throttler.WaitAsync(ct);
                    try
                    {
                        if (ct.IsCancellationRequested) return;
                        await FetchOneAsync(job, ct);
                    }
                    finally { throttler.Release(); done++; _eventQueue.Enqueue(new { type = "progress", done, total = jobs.Count }); }
                }).ToArray();
                await Task.WhenAll(tasks);
            }
            catch (Exception ex) { LogConsole($"Batch Error: {ex.Message}", true); }
            finally { _isRunning = false; _eventQueue.Enqueue(new { type = "complete" }); LogConsole("All jobs completed."); if (_cts != null) { _cts.Dispose(); _cts = null; } }
        }

        private async Task FetchOneAsync(Job job, CancellationToken ct)
        {
            try
            {
                var resp = await _http.GetAsync(job.Url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode) { LogConsole($"HTTP {(int)resp.StatusCode}: {job.Url}", true); return; }

                var data = await resp.Content.ReadAsByteArrayAsync(ct);
                string resolution = "Unknown";

                // 1. 이미지 유효성 검사 (ImageMagick)
                using (var ms = new MemoryStream(data))
                {
                    try { using var img = new MagickImage(ms); resolution = $"{img.Width}x{img.Height}"; } catch { return; }
                }

                // 2. EXIF 검사 (MainForm 로직 이식)
                bool hasExif = false;
                using (var ms = new MemoryStream(data))
                {
                    hasExif = CheckExif(ms);
                }

                string id = Guid.NewGuid().ToString();
                _imageCache[id] = data;

                var titleParts = new List<string>();
                if (job.Tokens.ContainsKey("name")) titleParts.Add(job.Tokens["name"]);
                if (job.Tokens.ContainsKey("situation")) titleParts.Add(job.Tokens["situation"]);
                if (job.Tokens.ContainsKey("num")) titleParts.Add(job.Tokens["num"]);
                string title = string.Join(" / ", titleParts);
                if (string.IsNullOrEmpty(title)) title = Path.GetFileName(job.Url);

                _eventQueue.Enqueue(new
                {
                    type = "image",
                    id,
                    title,
                    url = job.Url,
                    resolution,
                    hasExif, // [추가] 프론트엔드로 EXIF 결과 전송
                    jobTokens = job.Tokens
                });
            }
            catch (Exception ex) { if (!(ex is OperationCanceledException)) LogConsole($"Err: {ex.Message} ({job.Url})", true); }
        }

        // [추가] MainForm에서 가져온 EXIF 확인 로직
        private bool CheckExif(MemoryStream ms)
        {
            try
            {
                ms.Position = 0;
                var directories = ImageMetadataReader.ReadMetadata(ms);
                string? software = null;
                string? comment = null;

                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        var name = tag.Name;
                        if (name == "Software")
                        {
                            software = tag.Description;
                            if (CheckBySoftware(software)) return true;
                        }
                        else if (name == "Comment")
                        {
                            comment = tag.Description;
                            if (CheckByComment(comment)) return true;
                        }
                        else if (name == "Textual Data")
                        {
                            var parts = tag.Description.Split(new[] { ':' }, 2);
                            if (parts.Length != 2) continue;
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            if (key.Equals("Software", StringComparison.OrdinalIgnoreCase))
                            {
                                software ??= value;
                                if (CheckBySoftware(software)) return true;
                            }
                            else if (key.Equals("Comment", StringComparison.OrdinalIgnoreCase))
                            {
                                comment ??= value;
                                if (CheckByComment(comment)) return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch { return false; }

            bool CheckBySoftware(string? sw)
            {
                if (string.IsNullOrEmpty(sw)) return false;
                if (sw.Contains("NovelAI", StringComparison.OrdinalIgnoreCase)) return true;
                if (sw.Contains("NAI Diffusion", StringComparison.OrdinalIgnoreCase)) return true;
                var knownModelHashes = new[] { "37C2B166", "7BCCAA2C", "7ABFFA2A", "37442FCA", "C02D4F98", "4BDE2A90" };
                var last = sw.Split(' ').Last();
                return knownModelHashes.Contains(last, StringComparer.OrdinalIgnoreCase);
            }
            bool CheckByComment(string? c)
            {
                if (string.IsNullOrEmpty(c)) return false;
                return c.Contains("\"request_type\"", StringComparison.OrdinalIgnoreCase) && c.Contains("\"signed_hash\"", StringComparison.OrdinalIgnoreCase);
            }
        }

        private List<Job> GenerateJobs(string baseUrl, string codePart, string namesStr, string sitsStr, int numStart, int numEnd)
        {
            var jobs = new List<Job>();
            bool hasName = codePart.Contains("{name}"), hasNum = codePart.Contains("{num}"), hasSit = codePart.Contains("{situation}");
            var nameList = hasName ? namesStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string> { null };
            var sitList = hasSit ? sitsStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string> { null };
            if (!hasNum) { numStart = 0; numEnd = 0; }

            foreach (var name in nameList)
                foreach (var sit in sitList)
                    for (int num = numStart; num <= numEnd; num++)
                    {
                        var job = new Job { Tokens = new Dictionary<string, string>() };
                        var tempUrl = codePart;
                        if (hasName && name != null) { tempUrl = tempUrl.Replace("{name}", Uri.EscapeDataString(name)); job.Tokens["name"] = name; }
                        if (hasSit && sit != null) { tempUrl = tempUrl.Replace("{situation}", Uri.EscapeDataString(sit)); job.Tokens["situation"] = sit; }
                        if (hasNum) { tempUrl = tempUrl.Replace("{num}", num.ToString()); job.Tokens["num"] = num.ToString(); }
                        job.Url = baseUrl + tempUrl;
                        jobs.Add(job);
                    }

            if (jobs.Count == 0 && !codePart.Contains("{")) jobs.Add(new Job { Url = baseUrl + codePart, Tokens = new Dictionary<string, string>() });
            return jobs;
        }

        private void StopFetching() { _cts?.Cancel(); _isRunning = false; }

        private async Task HandleSave(HttpListenerRequest req, HttpListenerResponse resp)
        {
            try
            {
                using var reader = new StreamReader(req.InputStream);
                var payload = JsonSerializer.Deserialize<JsonElement>(await reader.ReadToEndAsync());
                string path = payload.GetProperty("path").GetString();
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                int saved = 0;
                foreach (var kvp in _imageCache) { File.WriteAllBytes(Path.Combine(path, $"{Guid.NewGuid()}.png"), kvp.Value); saved++; }
                LogConsole($"Saved {saved} images to {path}");
                RespondJson(resp, new { msg = $"{saved} saved" });
            }
            catch (Exception ex) { LogConsole($"Save failed: {ex.Message}", true); RespondJson(resp, new { msg = "Save failed" }); }
        }

        private string GetJsonString(JsonElement root, string key) => root.TryGetProperty(key, out var prop) ? prop.GetString() : "";
        private int GetJsonInt(JsonElement root, string key) => root.TryGetProperty(key, out var prop) && prop.TryGetInt32(out int val) ? val : 0;

        private void ServeImage(HttpListenerResponse resp, string id)
        {
            if (_imageCache.TryGetValue(id, out var data)) { resp.ContentType = "image/png"; resp.ContentLength64 = data.Length; resp.OutputStream.Write(data, 0, data.Length); }
            else resp.StatusCode = 404;
            resp.Close();
        }

        private void ServeUpdates(HttpListenerResponse resp)
        {
            var events = new List<object>();
            while (_eventQueue.TryDequeue(out var ev)) events.Add(ev);
            RespondJson(resp, events);
        }

        private void RespondJson(HttpListenerResponse resp, object data)
        {
            var json = JsonSerializer.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            resp.ContentType = "application/json";
            resp.ContentLength64 = buffer.Length;
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            resp.Close();
        }

        private void ServeHtml(HttpListenerResponse resp)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(HtmlContent);
            resp.ContentType = "text/html";
            resp.ContentLength64 = buffer.Length;
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            resp.Close();
        }

        public void Dispose() { StopFetching(); _listener?.Stop(); }

        // --- Frontend HTML/JS/CSS ---
        private const string HtmlContent = @"
<!DOCTYPE html>
<html lang='ko'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>ImageFlow WebUI</title>
    <style>
        :root {
            --bg-color: #121212;
            --panel-color: #1e1e1e;
            --input-bg: #2c2c2c;
            --text-main: #e0e0e0;
            --text-sub: #a0a0a0;
            --accent: #bb86fc;
            --success: #03dac6;
            --error: #cf6679;
            --header-height: 50px;
            --nav-height: 40px;
        }
        body { margin: 0; font-family: 'Segoe UI', sans-serif; background: var(--bg-color); color: var(--text-main); height: 100vh; overflow: hidden; display: flex; flex-direction: column; }

        /* --- Header & Nav --- */
        .header-bar { height: var(--header-height); background: #181818; border-bottom: 1px solid #333; display: flex; align-items: center; justify-content: space-between; padding: 0 20px; flex-shrink: 0; z-index: 200; }
        .app-title { font-weight: bold; color: var(--accent); font-size: 1.2rem; letter-spacing: 0.5px; }
        .menu-btn {
                background: transparent;
                border: none;
                color: var(--text-main);
                cursor: pointer;
                padding: 8px;
                border-radius: 4px;
                display: flex;
                align-items: right;
                justify-content: right;
                transition: background 0.2s;
                width: 40px;
                height: 40px;            
                flex-grow: 0;
                flex-shrink: 0;
            }
        .menu-btn:hover { background: #333; }
        .menu-btn svg { fill: currentColor; width: 24px; height: 24px; }        .top-nav { height: var(--nav-height); background: #252526; border-bottom: 1px solid #333; display: flex; align-items: center; padding: 0 20px; gap: 15px; flex-shrink: 0; }
        .nav-btn { background: transparent; border: 3.3px solid transparent; color: var(--text-sub); cursor: pointer; font-size: 0.9rem; padding: 0 10px; height: 100%; transition: 0.2s; flex-shrink: 0; }
        .nav-btn:hover { color: var(--text-main); }
        .nav-btn.active { color: var(--text-main); border-bottom-color: var(--accent); }

        /* --- Layout --- */
        .view-container { flex: 1; display: none; width: 100%; height: calc(100vh - var(--header-height) - var(--nav-height)); overflow: hidden; position: relative; }
        .view-container.active { display: flex; }

        /* --- Sidebar --- */
        .sidebar { width: 320px; background: var(--panel-color); padding: 20px; display: flex; flex-direction: column; gap: 15px; border-right: 1px solid #333; overflow-y: auto; height: 100%; box-sizing: border-box; }
        .sidebar-footer { margin-top: auto; display: flex; flex-direction: column; gap: 10px; padding-top: 15px; border-top: 1px solid #333; }

        /* --- Flow Panel --- */
        .flow-panel { position: fixed; top: var(--header-height); right: 0; bottom: 0; width: 320px; background: #202020; border-left: 1px solid #333; box-shadow: -5px 0 15px rgba(0,0,0,0.5); transform: translateX(100%); transition: transform 0.3s ease-in-out; z-index: 300; display: flex; flex-direction: column; padding: 20px; }
        .flow-panel.open { transform: translateX(0); }
        .flow-header { font-size: 1.1rem; font-weight: bold; color: var(--text-main); border-bottom: 1px solid #333; padding-bottom: 10px; margin-bottom: 15px; display: flex; justify-content: space-between; align-items: center; }
        .close-flow-btn {
            background: transparent;
            border: none;
            color: var(--text-sub);
            cursor: pointer;
            font-size: 1.2rem;
            display: flex;
            align-items: right;
            justify-content: right;
        }
        /* --- Controls --- */
        .group { display: flex; flex-direction: column; gap: 5px; }
        label { font-size: 0.85rem; color: var(--text-sub); }
        input, textarea, select { background: var(--input-bg); border: 1px solid #444; color: white; padding: 8px; border-radius: 4px; font-family: inherit; width: 100%; box-sizing: border-box; }
        textarea { resize: vertical; min-height: 60px; }
        input:focus, textarea:focus, select:focus { outline: none; border-color: var(--accent); }
        .row { display: flex; gap: 10px; width: 100%; }
        .btn-row { display: flex; gap: 10px; margin-top: 10px; }
        button { flex: 1; padding: 10px; border: none; border-radius: 4px; font-weight: bold; cursor: pointer; transition: 0.2s; }
        .btn-start { background: var(--accent); color: black; }
        .btn-stop { background: var(--error); color: black; display: none; }
        .btn-save { background: var(--input-bg); color: var(--success); border: 1px solid var(--success); }
        button:hover { opacity: 0.9; }

        /* --- Progress Bar --- */
        .status-wrapper { display: flex; flex-direction: column; gap: 5px; }
        .status-header { display: flex; justify-content: flex-end; }
        .status-text { color: var(--text-sub); font-size: 0.85rem; font-weight: bold; }
        .progress-container { width: 100%; height: 6px; background: #333; border-radius: 3px; overflow: hidden; }
        .progress-bar { height: 100%; background: var(--success); width: 0%; transition: width 0.3s; }

        /* --- Gallery --- */
        .main-content { flex: 1; display: flex; flex-direction: column; padding: 20px; overflow: hidden; height: 100%; box-sizing: border-box; }
        .gallery { flex: 1; overflow-y: auto; display: grid; grid-template-columns: repeat(auto-fill, 220px); grid-auto-rows: max-content; gap: 15px; align-content: start; padding-right: 5px; }
        .gallery::-webkit-scrollbar { width: 8px; }
        .gallery::-webkit-scrollbar-track { background: #121212; }
        .gallery::-webkit-scrollbar-thumb { background: #333; border-radius: 4px; }
        .gallery::-webkit-scrollbar-thumb:hover { background: #444; }

        /* [수정됨] 카드 스타일: EXIF 결과에 따른 테두리 색상 */
        .card { 
            background: var(--panel-color); 
            border-radius: 8px; 
            overflow: hidden; 
            border: 1px solid #333; 
            transition: transform 0.2s, border-color 0.2s; 
            position: relative; 
            width: 220px; 
            flex-shrink: 0; 
        }
        .card:hover { transform: translateY(-3px); }
        
        /* EXIF 상태별 테두리 색상 */
        .card.exif-ok { border-color: #4caf50; border-width: 1px; } /* Green */
        .card.exif-fail { border-color: #f44336; border-width: 1px; } /* Red */

        .card img { width: 100%; height: 220px; object-fit: cover; display: block; cursor: pointer; }
        .card-info { padding: 10px; font-size: 0.8rem; }
        .card-title { font-weight: bold; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-bottom: 4px; }
        .card-meta { color: var(--text-sub); display: flex; justify-content: space-between; }

        /* --- Settings --- */
        .settings-layout { display: flex; width: 100%; height: 100%; }
        .settings-sidebar { width: 250px; background: var(--panel-color); border-right: 1px solid #333; padding: 20px 0; display: flex; flex-direction: column; }
        .settings-menu-item { padding: 12px 20px; cursor: pointer; color: var(--text-sub); transition: 0.2s; border-left: 3px solid transparent; }
        .settings-menu-item:hover { background: #2a2a2a; color: var(--text-main); }
        .settings-menu-item.active { background: #2a2a2a; color: var(--accent); border-left-color: var(--accent); }
        .settings-content { flex: 1; padding: 40px; overflow-y: auto; }
        .settings-section { display: none; max-width: 600px; }
        .settings-section.active { display: block; }
        .settings-header { font-size: 1.5rem; margin-bottom: 20px; border-bottom: 1px solid #333; padding-bottom: 10px; color: var(--text-main); }

        .modal { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.9); display: none; justify-content: center; align-items: center; z-index: 1000; }
        .modal img { max-width: 90%; max-height: 90%; box-shadow: 0 0 20px rgba(0,0,0,0.5); }
    </style>
</head>
<body>
    <div class='header-bar'>
        <div class='app-title'>ImageFlow</div>
        <button class='menu-btn' onclick='toggleFlowPanel()' title='Open Flow Panel'><svg viewBox='0 0 24 24'><path d='M3 6h18v2H3V6zm0 5h18v2H3v-2zm0 5h18v2H3v-2z'/></svg></button>
    </div>

    <div class='top-nav'>
        <button class='nav-btn active' onclick='switchPage(""main"")' id='nav-main'>ImageLoader</button>
        <button class='nav-btn' onclick='switchPage(""settings"")' id='nav-settings'>Settings</button>
    </div>

    <div class='flow-panel' id='flowPanel'>
        <div class='flow-header'><span>Flow Panel</span><button class='close-flow-btn' onclick='toggleFlowPanel()'>&times;</button></div>
        <div style='color: #888; font-size: 0.9rem;'>
            <div class='group'><label>Quick Actions</label><button class='btn-save' style='width:100%' onclick='saveAll()'>Save All Now</button></div>
             <div class='group' style='margin-top:10px'><button class='btn-start' style='width:100%' onclick='saveSettings()'>Save Settings</button></div>
        </div>
    </div>

    <div id='view-main' class='view-container active'>
        <div class='sidebar'>
            <div class='group'><label>Base URL</label><input type='text' id='baseUrl' placeholder='고정 링크'></div>
            <div class='group'><label>Code (Template)</label><input type='text' id='code' placeholder='{name}, {situation}, {num}'></div>
            <div class='group'><label>Name List ({name})</label><textarea id='names' placeholder='쉼표(,)로 구분'></textarea></div>
            <div class='group'><label>Situation List ({situation})</label><textarea id='situations' placeholder='쉼표(,)로 구분'></textarea></div>
            <div class='row'>
                <div class='group' style='flex:1'><label>Start #</label><input type='number' id='startNum' value='0'></div>
                <div class='group' style='flex:1'><label>End #</label><input type='number' id='endNum' value='0'></div>
            </div>
            <div class='group'><label>Parallel Requests</label><input type='number' id='parallel' value='4' min='1' max='64'></div>
            <div class='btn-row'><button class='btn-start' id='btnStart' onclick='start()'>Start</button><button class='btn-stop' id='btnStop' onclick='stop()'>Stop</button></div>
            <div class='sidebar-footer'>
                <div class='group'><label>Save Path</label><input type='text' id='savePath' value='C:\ImageFlow_Output'></div>
                <div class='status-wrapper'><div class='status-header'><span id='statusText' class='status-text'>Ready</span></div><div class='progress-container'><div class='progress-bar' id='progressBar'></div></div></div>
            </div>
        </div>
        <div class='main-content'><div class='gallery' id='gallery'></div></div>
    </div>

    <div id='view-settings' class='view-container'>
        <div class='settings-layout'>
            <div class='settings-sidebar'>
                <div class='settings-menu-item active' onclick='switchSettingTab(this, ""set-directory"")'>Directory</div>
                <div class='settings-menu-item' onclick='switchSettingTab(this, ""set-general"")'>General</div>
                <div class='settings-menu-item' onclick='switchSettingTab(this, ""set-about"")'>About</div>
            </div>
            <div class='settings-content'>
                <div id='set-directory' class='settings-section active'>
                    <div class='settings-header'>Directory Settings</div>
                    <div class='group' style='margin-bottom: 20px;'><label>Input Path</label><input type='text' id='cfg_inputPath' value=''></div>
                    <div class='group' style='margin-bottom: 20px;'><label>Output Path</label><input type='text' id='cfg_outputPath' value=''></div>
                </div>
                <div id='set-general' class='settings-section'>
                    <div class='settings-header'>Appearance</div>
                    <div class='group'><label>Color Theme</label><select><option>Dark Mode</option><option>Light Mode</option><option>System Default</option></select></div>
                </div>
                <div id='set-about' class='settings-section'>
                   <div class='settings-header'>About</div>
                   <p style='color: #aaa;'>ImageFlow WebUI v1.4.0</p>
                   <p style='color: #888;'>Created with C# Console & HTML/JS Interface.</p>                    
                </div>
            </div>
        </div>
    </div>

    <div class='modal' id='modal' onclick='this.style.display=""none""'><img id='modalImg' src=''></div>

    <script>
        window.onload = async function() {
            try {
                const res = await fetch('/api/config/load');
                if(res.ok) {
                    const data = await res.json();
                    if(data.baseUrl) document.getElementById('baseUrl').value = data.baseUrl;
                    if(data.code) document.getElementById('code').value = data.code;
                    if(data.names) document.getElementById('names').value = data.names;
                    if(data.situations) document.getElementById('situations').value = data.situations;
                    if(data.startNum) document.getElementById('startNum').value = data.startNum;
                    if(data.endNum) document.getElementById('endNum').value = data.endNum;
                    if(data.parallel) document.getElementById('parallel').value = data.parallel;
                    if(data.inputPath) document.getElementById('cfg_inputPath').value = data.inputPath;
                    if(data.outputPath) document.getElementById('cfg_outputPath').value = data.outputPath;
                    if(data.outputPath) document.getElementById('savePath').value = data.outputPath;
                    console.log('Settings loaded.');
                }
            } catch(e) { console.error('Failed to load settings', e); }
        };

        function toggleFlowPanel() { document.getElementById('flowPanel').classList.toggle('open'); }
        function switchPage(pageName) {
            document.querySelectorAll('.view-container').forEach(el => el.classList.remove('active'));
            document.querySelectorAll('.nav-btn').forEach(el => el.classList.remove('active'));
            document.getElementById('view-' + pageName).classList.add('active');
            document.getElementById('nav-' + pageName).classList.add('active');
        }
        function switchSettingTab(menuItem, sectionId) {
            document.querySelectorAll('.settings-menu-item').forEach(el => el.classList.remove('active'));
            menuItem.classList.add('active');
            document.querySelectorAll('.settings-section').forEach(el => el.classList.remove('active'));
            document.getElementById(sectionId).classList.add('active');
        }

        let isRunning = false;
        let pollInterval = null;

        async function start() {
            const config = {
                baseUrl: document.getElementById('baseUrl').value,
                code: document.getElementById('code').value,
                names: document.getElementById('names').value,
                situations: document.getElementById('situations').value,
                startNum: parseInt(document.getElementById('startNum').value) || 0,
                endNum: parseInt(document.getElementById('endNum').value) || 0,
                parallel: parseInt(document.getElementById('parallel').value) || 1
            };
            const res = await fetch('/api/start', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(config) });
            const data = await res.json();
            if(data.error) { alert(data.error); return; }

            setRunning(true);
            document.getElementById('gallery').innerHTML = '';
            console.log(`Started ${data.jobCount} jobs...`);
            pollInterval = setInterval(pollUpdates, 1000);
        }

        async function stop() { await fetch('/api/stop', { method: 'POST' }); setRunning(false); }
        
        async function saveAll() {
             const path = document.getElementById('savePath').value;
             const res = await fetch('/api/save', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ path: path }) });
             const data = await res.json();
             alert(data.msg);
        }
        
        async function saveSettings() {
            const payload = {
                baseUrl: document.getElementById('baseUrl').value,
                code: document.getElementById('code').value,
                names: document.getElementById('names').value,
                situations: document.getElementById('situations').value,
                startNum: parseInt(document.getElementById('startNum').value) || 0,
                endNum: parseInt(document.getElementById('endNum').value) || 0,
                parallel: parseInt(document.getElementById('parallel').value) || 1,
                inputPath: document.getElementById('cfg_inputPath').value,
                outputPath: document.getElementById('cfg_outputPath').value
            };
            try {
                const res = await fetch('/api/config/save', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
                const data = await res.json();
                if(data.status === 'ok') { alert('설정이 저장되었습니다.'); } else { alert('저장 실패: ' + data.message); }
            } catch (e) { console.error(e); alert('통신 오류'); }
        }

        async function pollUpdates() {
            try {
                const res = await fetch('/api/updates');
                const events = await res.json();
                events.forEach(ev => {
                    if(ev.type === 'image') addImage(ev);
                    else if(ev.type === 'progress') updateProgress(ev);
                    else if(ev.type === 'complete') { setRunning(false); console.log('Completed'); }
                });
            } catch(e) { console.error(e); }
        }

        function addImage(imgData) {
            const div = document.createElement('div');
            // [수정됨] EXIF 결과에 따라 클래스 추가
            div.className = 'card ' + (imgData.hasExif ? 'exif-ok' : 'exif-fail');
            div.innerHTML = `<img src='/image/${imgData.id}' onclick='showModal(this.src)'><div class='card-info'><div class='card-title' title='${imgData.title}'>${imgData.title}</div><div class='card-meta'><span>${imgData.resolution}</span><a href='${imgData.url}' target='_blank' style='color:#aaa'>Link</a></div></div>`;
            document.getElementById('gallery').appendChild(div);
        }

        function updateProgress(data) {
            const pct = (data.done / data.total) * 100;
            document.getElementById('progressBar').style.width = pct + '%';
            document.getElementById('statusText').innerText = `${data.done} / ${data.total}`;
        }

        function setRunning(running) {
            isRunning = running;
            document.getElementById('btnStart').style.display = running ? 'none' : 'block';
            document.getElementById('btnStop').style.display = running ? 'block' : 'none';
            if(!running && pollInterval) clearInterval(pollInterval);
        }

        function showModal(src) {
            document.getElementById('modalImg').src = src;
            document.getElementById('modal').style.display = 'flex';
        }
    </script>
</body>
</html>";
    }
}