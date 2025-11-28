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

        // 데이터 저장소 (메모리)
        private ConcurrentDictionary<string, byte[]> _imageCache = new();
        private ConcurrentQueue<object> _eventQueue = new(); // 프론트엔드로 보낼 이벤트
        private bool _isRunning = false;

        // 기존 로직 설정값
        private LoaderConfig _currentConfig = new();
        private PathConfig _pathConfig = new();

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            Console.WriteLine($"Web UI started at {_url}");
            Process.Start(new ProcessStartInfo { FileName = _url, UseShellExecute = true });

            Task.Run(() => ListenLoop());
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
                catch (HttpListenerException) { break; }
                catch (InvalidOperationException) { break; }
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var resp = ctx.Response;
            var path = req.Url.AbsolutePath.ToLower();

            try
            {
                if (req.HttpMethod == "GET" && path == "/")
                {
                    ServeHtml(resp);
                }
                else if (req.HttpMethod == "GET" && path.StartsWith("/image/"))
                {
                    ServeImage(resp, path.Replace("/image/", ""));
                }
                else if (req.HttpMethod == "GET" && path == "/api/updates")
                {
                    ServeUpdates(resp);
                }
                else if (req.HttpMethod == "POST" && path == "/api/start")
                {
                    await HandleStart(req, resp);
                }
                else if (req.HttpMethod == "POST" && path == "/api/stop")
                {
                    StopFetching();
                    RespondJson(resp, new { status = "stopped" });
                }
                else if (req.HttpMethod == "POST" && path == "/api/save")
                {
                    await HandleSave(req, resp);
                }
                else
                {
                    resp.StatusCode = 404;
                    resp.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                resp.StatusCode = 500;
                resp.Close();
            }
        }

        // --- Core Logic (Adapted from MainForm) ---

        private async Task HandleStart(HttpListenerRequest req, HttpListenerResponse resp)
        {
            if (_isRunning) { RespondJson(resp, new { error = "Already running" }); return; }

            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            var json = await reader.ReadToEndAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            // 설정 파싱
            string baseUrl = data.GetProperty("baseUrl").GetString();
            string code = data.GetProperty("code").GetString();
            string names = data.GetProperty("names").GetString();
            string situations = data.GetProperty("situations").GetString();
            int startNum = data.GetProperty("startNum").GetInt32();
            int endNum = data.GetProperty("endNum").GetInt32();
            int parallel = data.GetProperty("parallel").GetInt32();

            // 작업 생성 로직 (MainForm에서 가져옴)
            var jobs = GenerateJobs(baseUrl, code, names, situations, startNum, endNum);

            if (jobs.Count == 0)
            {
                RespondJson(resp, new { error = "No jobs generated" });
                return;
            }

            _cts = new CancellationTokenSource();
            _isRunning = true;
            _imageCache.Clear(); // 이전 캐시 클리어 (선택사항)

            // 백그라운드 작업 시작
            _ = Task.Run(() => FetchAllAsync(jobs, parallel, _cts.Token));

            RespondJson(resp, new { status = "started", jobCount = jobs.Count });
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
                    finally
                    {
                        throttler.Release();
                        done++;
                        _eventQueue.Enqueue(new { type = "progress", done, total = jobs.Count });
                    }
                }).ToArray();

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _eventQueue.Enqueue(new { type = "error", message = ex.Message });
            }
            finally
            {
                _isRunning = false;
                _eventQueue.Enqueue(new { type = "complete" });
                if (_cts != null) { _cts.Dispose(); _cts = null; }
            }
        }

        private async Task FetchOneAsync(Job job, CancellationToken ct)
        {
            try
            {
                var resp = await _http.GetAsync(job.Url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _eventQueue.Enqueue(new { type = "log", msg = $"HTTP {(int)resp.StatusCode}: {job.Url}", isError = true });
                    return;
                }

                var data = await resp.Content.ReadAsByteArrayAsync(ct);

                // EXIF, Image Load 체크 (MainForm 로직 간소화)
                bool exifSuccess = false;
                string resolution = "Unknown";

                using (var ms = new MemoryStream(data))
                {
                    // EXIF 체크 (MainForm의 CheckExif 로직이 필요하다면 여기에 복사하거나 static으로 참조)
                    // 여기서는 간단히 ImageMagick으로 열리는지만 확인
                    try
                    {
                        using var img = new MagickImage(ms);
                        resolution = $"{img.Width}x{img.Height}";
                    }
                    catch { return; } // 이미지 아님
                }

                string id = Guid.NewGuid().ToString();
                _imageCache[id] = data;

                // 타이틀 생성
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
                    jobTokens = job.Tokens // 저장용
                });
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                    _eventQueue.Enqueue(new { type = "log", msg = $"Err: {ex.Message}", isError = true });
            }
        }

        private List<Job> GenerateJobs(string baseUrl, string codePart, string namesStr, string sitsStr, int numStart, int numEnd)
        {
            // MainForm의 GenerateJobs 로직 재구성
            var jobs = new List<Job>();
            bool hasName = codePart.Contains("{name}");
            bool hasNum = codePart.Contains("{num}");
            bool hasSit = codePart.Contains("{situation}");

            var nameList = hasName ? namesStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string> { null };
            var sitList = hasSit ? sitsStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string> { null };

            if (!hasNum) { numStart = 0; numEnd = 0; }

            foreach (var name in nameList)
            {
                foreach (var sit in sitList)
                {
                    for (int num = numStart; num <= numEnd; num++)
                    {
                        var job = new Job { Tokens = new Dictionary<string, string>() };
                        var tempUrl = codePart;

                        if (hasName && name != null) { tempUrl = tempUrl.Replace("{name}", Uri.EscapeDataString(name)); job.Tokens["name"] = name; }
                        if (hasSit && sit != null) { tempUrl = tempUrl.Replace("{situation}", Uri.EscapeDataString(sit)); job.Tokens["situation"] = sit; }
                        if (hasNum) { tempUrl = tempUrl.Replace("{num}", num.ToString()); job.Tokens["num"] = num.ToString(); }

                        // 기타 토큰 처리는 UI상 복잡하여 생략하거나 필요 시 추가 구현

                        job.Url = baseUrl + tempUrl;
                        jobs.Add(job);
                    }
                }
            }

            if (jobs.Count == 0 && !codePart.Contains("{"))
                jobs.Add(new Job { Url = baseUrl + codePart, Tokens = new Dictionary<string, string>() });

            return jobs;
        }

        private void StopFetching()
        {
            _cts?.Cancel();
            _isRunning = false;
        }

        private async Task HandleSave(HttpListenerRequest req, HttpListenerResponse resp)
        {
            // 저장 로직 (간소화)
            using var reader = new StreamReader(req.InputStream);
            var json = await reader.ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<JsonElement>(json);

            string path = payload.GetProperty("path").GetString();
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

            int saved = 0;
            foreach (var kvp in _imageCache)
            {
                // 실제로는 Job 정보와 매핑해야 하지만, 여기선 간단히 저장 예시
                // 완벽히 하려면 _imageCache에 Job 정보도 같이 담아야 함
                string fName = $"{Guid.NewGuid()}.png";
                File.WriteAllBytes(Path.Combine(path, fName), kvp.Value);
                saved++;
            }
            RespondJson(resp, new { msg = $"{saved} saved" });
        }


        // --- Helpers ---

        private void ServeImage(HttpListenerResponse resp, string id)
        {
            if (_imageCache.TryGetValue(id, out var data))
            {
                resp.ContentType = "image/png";
                resp.ContentLength64 = data.Length;
                resp.OutputStream.Write(data, 0, data.Length);
            }
            else
            {
                resp.StatusCode = 404;
            }
            resp.Close();
        }

        private void ServeUpdates(HttpListenerResponse resp)
        {
            // 폴링 방식: 쌓인 이벤트를 모두 보냄
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

        public void Dispose()
        {
            StopFetching();
            _listener?.Stop();
        }

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
        }
        body { margin: 0; font-family: 'Segoe UI', sans-serif; background: var(--bg-color); color: var(--text-main); display: flex; height: 100vh; overflow: hidden; }
        
        /* Sidebar */
        .sidebar { width: 320px; background: var(--panel-color); padding: 20px; display: flex; flex-direction: column; gap: 15px; border-right: 1px solid #333; overflow-y: auto; }
        h2 { margin: 0 0 10px 0; color: var(--accent); font-size: 1.2rem; }
        .group { display: flex; flex-direction: column; gap: 5px; }
        label { font-size: 0.85rem; color: var(--text-sub); }

        /* Input Full Width Fix */
        input, textarea { 
            background: var(--input-bg); 
            border: 1px solid #444; 
            color: white; 
            padding: 8px; 
            border-radius: 4px; 
            font-family: inherit; 
            width: 100%; 
            box-sizing: border-box; 
        }

        textarea { resize: vertical; min-height: 60px; }
        input:focus, textarea:focus { outline: none; border-color: var(--accent); }
        
        .row { display: flex; gap: 10px; width: 100%; }
        
        .btn-row { display: flex; gap: 10px; margin-top: 10px; }
        
        button { flex: 1; padding: 10px; border: none; border-radius: 4px; font-weight: bold; cursor: pointer; transition: 0.2s; }
        .btn-start { background: var(--accent); color: black; }
        .btn-stop { background: var(--error); color: black; display: none; }
        .btn-save { background: var(--input-bg); color: var(--success); border: 1px solid var(--success); }
        button:hover { opacity: 0.9; }
        button:disabled { opacity: 0.5; cursor: not-allowed; }

        /* Main Content */
        .main { flex: 1; display: flex; flex-direction: column; padding: 20px; overflow: hidden; }
        
        /* [수정됨] Status Bar Layout: Column direction (Bar Top, Text Bottom) */
        .status-container { 
            display: flex; 
            flex-direction: column; 
            gap: 8px; 
            margin-bottom: 15px; 
        }
        
        .progress-container { 
            width: 100%;    /* 전체 너비 사용 */
            height: 8px;    /* 조금 더 두껍게 */
            background: #333; 
            border-radius: 4px; 
            overflow: hidden; 
        }
        
        .progress-bar { height: 100%; background: var(--success); width: 0%; transition: width 0.3s; }
        
        .status-text { 
            color: var(--text-sub); 
            font-size: 0.9rem; 
            text-align: left; /* 좌측 정렬 */
        }
        
        .gallery { flex: 1; overflow-y: auto; display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 15px; align-content: start; }
        .card { background: var(--panel-color); border-radius: 8px; overflow: hidden; border: 1px solid #333; transition: transform 0.2s; position: relative; }
        .card:hover { transform: translateY(-3px); border-color: var(--accent); }
        .card img { width: 100%; height: 200px; object-fit: cover; display: block; cursor: pointer; }
        .card-info { padding: 10px; font-size: 0.8rem; }
        .card-title { font-weight: bold; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-bottom: 4px; }
        .card-meta { color: var(--text-sub); display: flex; justify-content: space-between; }

        /* Log Console */
        .log-panel { height: 100px; background: #000; margin-top: 10px; padding: 10px; font-family: monospace; font-size: 0.8rem; overflow-y: auto; border-radius: 4px; color: #aaa; }
        .log-err { color: var(--error); }

        /* Modal */
        .modal { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.9); display: none; justify-content: center; align-items: center; z-index: 1000; }
        .modal img { max-width: 90%; max-height: 90%; box-shadow: 0 0 20px rgba(0,0,0,0.5); }
    </style>
</head>
<body>
    <div class='sidebar'>
        <h2>ImageFlow WebUI</h2>
        <div class='group'>
            <label>Base URL</label>
            <input type='text' id='baseUrl' placeholder='고정 링크'>
        </div>
        <div class='group'>
            <label>Code (Template)</label>
            <input type='text' id='code' placeholder='{name}, {situation}, {num}'>
        </div>
        <div class='group'>
            <label>Name List ({name})</label>
            <textarea id='names' placeholder='쉼표(,)로 구분'></textarea>
        </div>
        <div class='group'>
            <label>Situation List ({situation})</label>
            <textarea id='situations' placeholder='쉼표(,)로 구분'></textarea>
        </div>
        <div class='row'>
            <div class='group' style='flex:1'>
                <label>Start #</label>
                <input type='number' id='startNum' value='0'>
            </div>
            <div class='group' style='flex:1'>
                <label>End #</label>
                <input type='number' id='endNum' value='0'>
            </div>
        </div>
        <div class='group'>
            <label>Parallel Requests</label>
            <input type='number' id='parallel' value='4' min='1' max='64'>
        </div>
        <div class='btn-row'>
            <button class='btn-start' id='btnStart' onclick='start()'>Start</button>
            <button class='btn-stop' id='btnStop' onclick='stop()'>Stop</button>
        </div>
        <div class='group' style='margin-top:auto'>
            <label>Save Path</label>
            <input type='text' id='savePath' value='C:\ImageFlow_Output'>
            <button class='btn-save' onclick='saveAll()' style='margin-top:5px'>Save All</button>
        </div>
    </div>

    <div class='main'>
        <div class='status-container'>
            <div class='progress-container'>
                <div class='progress-bar' id='progressBar'></div>
            </div>
            <span id='statusText' class='status-text'>Ready</span>
        </div>
        
        <div class='gallery' id='gallery'></div>
        <div class='log-panel' id='logPanel'></div>
    </div>

    <div class='modal' id='modal' onclick='this.style.display=""none""'>
        <img id='modalImg' src=''>
    </div>

    <script>
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

            const res = await fetch('/api/start', { 
                method: 'POST', 
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(config) 
            });
            
            const data = await res.json();
            if(data.error) {
                log(data.error, true);
                return;
            }

            setRunning(true);
            document.getElementById('gallery').innerHTML = '';
            log(`Started ${data.jobCount} jobs...`);
            pollInterval = setInterval(pollUpdates, 1000);
        }

        async function stop() {
            await fetch('/api/stop', { method: 'POST' });
            setRunning(false);
            log('Stopped by user.');
        }

        async function saveAll() {
            const path = document.getElementById('savePath').value;
            const res = await fetch('/api/save', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({ path: path }) 
            });
            const data = await res.json();
            alert(data.msg || 'Done');
        }

        async function pollUpdates() {
            try {
                const res = await fetch('/api/updates');
                const events = await res.json();
                
                events.forEach(ev => {
                    if(ev.type === 'image') addImage(ev);
                    else if(ev.type === 'progress') updateProgress(ev);
                    else if(ev.type === 'log') log(ev.msg, ev.isError);
                    else if(ev.type === 'complete') {
                        setRunning(false);
                        log('All jobs completed.');
                    }
                });
            } catch(e) {
                console.error(e);
            }
        }

        function addImage(imgData) {
            const div = document.createElement('div');
            div.className = 'card';
            div.innerHTML = `
                <img src='/image/${imgData.id}' onclick='showModal(this.src)'>
                <div class='card-info'>
                    <div class='card-title' title='${imgData.title}'>${imgData.title}</div>
                    <div class='card-meta'>
                        <span>${imgData.resolution}</span>
                        <a href='${imgData.url}' target='_blank' style='color:#aaa'>Link</a>
                    </div>
                </div>`;
            
            // [수정됨] prepend(앞에 추가) -> appendChild(뒤에 추가)
            document.getElementById('gallery').appendChild(div);
            
            // 자동 스크롤 (선택사항: 이미지가 추가될 때 화면을 아래로 내리고 싶으면 주석 해제)
            // document.querySelector('.gallery').scrollTop = document.querySelector('.gallery').scrollHeight;
        }

        function updateProgress(data) {
            const pct = (data.done / data.total) * 100;
            document.getElementById('progressBar').style.width = pct + '%';
            document.getElementById('statusText').innerText = `${data.done} / ${data.total} Completed`;
        }

        function log(msg, isErr) {
            const p = document.createElement('div');
            p.innerText = `[${new Date().toLocaleTimeString()}] ${msg}`;
            if(isErr) p.className = 'log-err';
            const panel = document.getElementById('logPanel');
            panel.prepend(p);
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