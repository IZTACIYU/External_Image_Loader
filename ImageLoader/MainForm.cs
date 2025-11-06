using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ImageMagick;       // MagickImage
using MetadataExtractor; // EXIF

namespace ImageLoader
{
    public partial class MainForm : Form
    {
        private const string Ptn = @"\{([^}]+)\}";
        private readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private CancellationTokenSource? _cts;

        public MainForm()
        {
            this.InitializeComponent();
            this.LoadConfig();
        }

        private void RunParsing()
        {
            _flp.Controls.Clear();

            var txt = _tIn.Text;
            if (string.IsNullOrEmpty(txt)) return;

            var mhs = Regex.Matches(txt, Ptn);
            if (mhs.Count == 0) return;

            var headers = new List<Label>();
            int maxW = 0;

            foreach (Match m in mhs)
            {
                var token = m.Groups[1].Value;

                // num, name, situation은 특수 토큰
                if (token == "num" || token == "name" || token == "situation") continue;

                var panel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };
                var header = new Label
                {
                    Text = $"{token}:", // 토큰 이름
                    AutoSize = true,
                    Margin = new Padding(3, 6, 3, 3),
                    Anchor = AnchorStyles.Left,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var txtBox = new TextBox
                {
                    Width = 150,
                    Margin = new Padding(3),
                    Anchor = AnchorStyles.Left
                };

                panel.Controls.Add(header);
                panel.Controls.Add(txtBox);

                _flp.Controls.Add(panel);


                if (header.PreferredWidth > maxW)
                    maxW = header.PreferredWidth;
                headers.Add(header);
            }

            foreach (var lbl in headers)
            {
                lbl.AutoSize = false;
                lbl.Width = maxW;
            }
        }
        private void SaveConfig()
        {
            var dat = new SimpleSaveForm
            {
                BaseURL = _tBs.Text,
                Code = _tIn.Text,
                NameToken = _tNam.Text,
                SituationToken = _tSit.Text,
                St_Num = (int)_numSt.Value,
                En_Num = (int)_numEn.Value,
                Multi_Call_Num = (int)_numPl.Value
            };

            var opt = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(dat, opt);

            try
            {
                var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                var fPath = Path.Combine(exeDir, "lnkConfig.json");

                File.WriteAllText(fPath, json);
                MessageBox.Show($"설정이 저장되었습니다.\n{fPath}", "저장 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다.\n{ex.Message}", "오류");
            }
        }
        private void LoadConfig()
        {
            var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            var fPath = Path.Combine(exeDir, "lnkConfig.json");

            if (!File.Exists(fPath)) return;

            try
            {
                var json = File.ReadAllText(fPath);
                var dat = JsonSerializer.Deserialize<SimpleSaveForm>(json);

                if (dat != null)
                {
                    _tBs.Text = dat.BaseURL;
                    _tIn.Text = dat.Code;
                    _tNam.Text = dat.NameToken;
                    _tSit.Text = dat.SituationToken;
                    _numSt.Value = dat.St_Num;
                    _numEn.Value = dat.En_Num;
                    _numPl.Value = dat.Multi_Call_Num;

                }
                else
                {
                    _tBs.Text = string.Empty;
                    _tIn.Text = string.Empty;
                    _tNam.Text = string.Empty;
                    _tSit.Text = string.Empty;

                    _numSt.Value = 0; // 11 -> 0
                    _numEn.Value = 0; // 11 -> 0
                    _numPl.Value = 1;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일 로드 실패:\n{ex.Message}", "로드 오류");
            }
        }


        private async void StartFetching()
        {
            if (_cts != null) return; // 실행 중

            // 작업 목록 생성
            List<Job> jobs;
            try
            {
                jobs = GenerateJobs();
                if (jobs.Count == 0)
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 생성 실패:\n{ex.Message}", "오류");
                return;
            }

            // UI 정리 && 상태 변경
            _flp.Controls.Clear();
            _cts = new CancellationTokenSource();
            SetButtons(running: true);

            try
            {
                // 동시 실행 수 FetchAllAsync에 전달
                await FetchAllAsync(jobs, (int)_numPl.Value, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 중지
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 실행 중 오류 발생:\n{ex.Message}", "실행 오류");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetButtons(running: false);
            }
        }
        private void StopFetching()
        {
            _cts?.Cancel();
        }
        private void SetButtons(bool running)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetButtons(running)));
                return;
            }

            _btSt.Enabled = !running;
            _btSp.Enabled = running;
            _btSav.Enabled = !running;
            _btPrs.Enabled = !running;
            _tBs.Enabled = !running;
            _tIn.Enabled = !running;

            // 특수/동적 컨트롤 활성화/비활성화
            _tNam.Enabled = !running;
            _tSit.Enabled = !running; // situation 텍스트박스
            _numSt.Enabled = !running;
            _numEn.Enabled = !running;
            _numPl.Enabled = !running;
            foreach (var tbx in _flp.Controls.OfType<FlowLayoutPanel>().SelectMany(p => p.Controls.OfType<TextBox>()))
            {
                tbx.Enabled = !running;
            }
        }

        private List<Job> GenerateJobs()
        {
            var jobs = new List<Job>();
            var baseUrl = _tBs.Text;
            var codePart = _tIn.Text;

            bool hasName = codePart.Contains("{name}");
            bool hasNum = codePart.Contains("{num}");
            bool hasSit = codePart.Contains("{situation}");

            var nameList = new List<string>();
            if (hasName)
            {
                if (string.IsNullOrWhiteSpace(_tNam.Text))
                {
                    MessageBox.Show("'{name}' 토큰이 있지만 이름 목록이 비어있습니다.", "입력 오류");
                    return jobs;
                }
                nameList.AddRange(_tNam.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else
            {
                nameList.Add(null);
            }

            var sitList = new List<string>();
            if (hasSit)
            {
                if (string.IsNullOrWhiteSpace(_tSit.Text))
                {
                    MessageBox.Show("'{situation}' 토큰이 있지만 값이 비어있습니다.", "입력 오류");
                    return jobs;
                }
                sitList.AddRange(_tSit.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
            else
            {
                sitList.Add(null);
            }


            int numStart = (int)_numSt.Value;
            int numEnd = (int)_numEn.Value;
            if (hasNum && numEnd < numStart)
            {
                MessageBox.Show("번호 범위가 올바르지 않습니다. (시작 <= 끝)", "입력 오류");
                return jobs;
            }
            if (!hasNum)
            {
                numStart = 0;
                numEnd = 0;
            }

            var otherTokens = new Dictionary<string, string>();
            foreach (var panel in _flp.Controls.OfType<FlowLayoutPanel>())
            {
                var lbl = panel.Controls.OfType<Label>().FirstOrDefault();
                var tbx = panel.Controls.OfType<TextBox>().FirstOrDefault();

                if (lbl == null || tbx == null) continue;
                var tokenName = lbl.Text.TrimEnd(':');
                var tokenValue = tbx.Text.Trim();

                if (string.IsNullOrEmpty(tokenValue))
                {
                    MessageBox.Show($"토큰 '{tokenName}'에 값이 입력되지 않았습니다.", "입력 오류");
                    return new List<Job>();
                }
                otherTokens[tokenName] = tokenValue;
            }

            foreach (var name in nameList)
            {
                foreach (var sit in sitList)
                {
                    for (int num = numStart; num <= numEnd; num++)
                    {
                        var job = new Job { Tokens = new Dictionary<string, string>() };
                        var tempUrl = codePart;

                        if (hasName)
                        {
                            tempUrl = tempUrl.Replace("{name}", Uri.EscapeDataString(name));
                            job.Tokens["name"] = name;
                        }
                        if (hasSit)
                        {
                            tempUrl = tempUrl.Replace("{situation}", Uri.EscapeDataString(sit));
                            job.Tokens["situation"] = sit;
                        }
                        if (hasNum)
                        {
                            tempUrl = tempUrl.Replace("{num}", num.ToString());
                            job.Tokens["num"] = num.ToString();
                        }
                        foreach (var token in otherTokens)
                        {
                            tempUrl = tempUrl.Replace($"{{{token.Key}}}", Uri.EscapeDataString(token.Value));
                            job.Tokens[token.Key] = token.Value;
                        }

                        job.Url = baseUrl + tempUrl;
                        jobs.Add(job);
                    }
                }
            }

            if (jobs.Count == 0 && Regex.IsMatch(codePart, Ptn))
            {
                MessageBox.Show("토큰이 있으나 입력된 값이 없습니다.", "알림");
            }
            else if (jobs.Count == 0 && !string.IsNullOrEmpty(codePart) && !codePart.Contains("{"))
            {
                jobs.Add(new Job { Url = baseUrl + codePart, Tokens = new Dictionary<string, string>() });
            }

            return jobs;
        }

        private async Task FetchAllAsync(List<Job> jobs, int parallelism, CancellationToken ct)
        {
            var throttler = new SemaphoreSlim(parallelism);
            int done = 0;

            var tasks = jobs.Select(async job =>
            {
                await throttler.WaitAsync(ct);
                try
                {
                    await FetchOneAsync(job, ct);
                }
                finally
                {
                    throttler.Release();
                    Interlocked.Increment(ref done);
                }
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        private async Task FetchOneAsync(Job job, CancellationToken ct)
        {
            HttpResponseMessage? resp = null;
            bool exifSuccess = false; // EXIF 파싱 성공 여부
            Image img = null;

            try
            {
                resp = await _http.GetAsync(job.Url, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    AddTile(job, null, $"HTTP {(int)resp.StatusCode}", ok: false, exifSuccess: false);
                    return;
                }

                var contentType = resp.Content.Headers.ContentType?.MediaType;

                if (!(contentType?.StartsWith("image/") ?? false))
                {
                    AddTile(job, null, "콘텐츠 타입 아님", ok: false, exifSuccess: false);
                    return;
                }

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);

                // 1. EXIF 읽기 시도
                ms.Position = 0;
                exifSuccess = ReadExif(ms);

                // 2. 이미지 로드
                ms.Position = 0; // MagickImage를 위해 스트림 리셋
                try
                {
                    using (var magickImage = new MagickImage(ms))
                    {
                        img = magickImage.ToBitmap();
                    }
                }
                catch (Exception ex)
                {
                    AddTile(job, null, $"이미지 파싱 실패: {ex.GetType().Name}", ok: false, exifSuccess: false);
                    return;
                }

                // 3. 타일 추가 (EXIF 정보 포함)
                AddTile(job, img, $"{img.Width}x{img.Height}", ok: true, exifSuccess: exifSuccess);
            }
            catch (TaskCanceledException)
            {
                AddTile(job, null, "취소됨", ok: false, exifSuccess: false);
            }
            catch (Exception ex)
            {
                AddTile(job, null, ex.GetType().Name, ok: false, exifSuccess: false);
            }
            finally
            {
                resp?.Dispose();
            }
        }

        private void AddTile(Job job, Image? img, string note, bool ok, bool exifSuccess)
        {

            if (InvokeRequired)
            {
                // exifSuccess 파라미터 추가
                BeginInvoke(new Action(() => AddTile(job, img, note, ok, exifSuccess)));
                return;
            }

            var panel = new Panel
            {
                Width = 220,
                Height = 260,
                Margin = new Padding(6),
                Padding = new Padding(6),
                BackColor = Color.White
            };

            var borderColor = ok ? Color.SeaGreen : Color.IndianRed;

            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(borderColor, 2);
                e.Graphics.DrawRectangle(pen, 1, 1, panel.Width - 3, panel.Height - 3);
            };

            var pb = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 200,
                Height = 160,
                Image = img,
                BackColor = Color.Gainsboro,
                Cursor = Cursors.Hand
            };

            var titleParts = new List<string>();
            if (job.Tokens.ContainsKey("name")) titleParts.Add(job.Tokens["name"]);
            if (job.Tokens.ContainsKey("situation")) titleParts.Add(job.Tokens["situation"]);
            if (job.Tokens.ContainsKey("num")) titleParts.Add(job.Tokens["num"]);
            foreach (var token in job.Tokens.Where(t => t.Key != "name" && t.Key != "situation" && t.Key != "num"))
            {
                titleParts.Add(token.Value);
            }
            var title = string.Join(" / ", titleParts);
            if (string.IsNullOrEmpty(title)) title = "[단일 요청]";

            pb.Click += (s, e) =>
            {
                var viewer = new Form { Text = $"{title} - {job.Url}", Width = 900, Height = 700 };
                var pic = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Image = img };
                var openBtn = new Button { Text = "브라우저로 열기", Dock = DockStyle.Bottom, Height = 30 };
                var chkExif = new Button { Text = "EXIF 확인", Dock = DockStyle.Bottom, Height = 30 };
                var saveOne = new Button { Text = "다운로드", Dock = DockStyle.Bottom, Height = 30 };
                openBtn.Click += (_, __) => Process.Start(new ProcessStartInfo { FileName = job.Url, UseShellExecute = true });

                viewer.Controls.Add(pic);
                viewer.Controls.Add(openBtn);
                viewer.Controls.Add(chkExif); // (중복 추가됨, 원본 코드와 동일하게 유지)
                viewer.Controls.Add(saveOne);
                viewer.Show();
            };

            var titleLbl = new Label
            {
                AutoSize = false,
                Width = 200,
                Height = 20,
                Text = title,
                Font = new Font(Font, FontStyle.Bold)
            };

            var meta = new LinkLabel
            {
                AutoSize = false,
                Width = 200,
                Height = 18,
                Text = note,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            meta.Links.Add(0, note.Length, job.Url);
            meta.LinkClicked += (s, e) =>
            {
                if (e.Link.LinkData is string u)
                    Process.Start(new ProcessStartInfo { FileName = u, UseShellExecute = true });
            };

            // EXIF 라벨
            var exifLbl = new Label
            {
                Text = "EXIF",
                ForeColor = Color.White,
                BackColor = exifSuccess ? Color.SeaGreen : Color.IndianRed,
                Font = new Font(Font, FontStyle.Bold),
                Size = new Size(40, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 레이아웃
            pb.Top = 6; pb.Left = 6;
            titleLbl.Top = pb.Bottom + 6; titleLbl.Left = 6;
            meta.Top = titleLbl.Bottom + 2; meta.Left = 6;
            exifLbl.Top = meta.Bottom + 4; exifLbl.Left = 6; // meta 라벨 아래에 배치

            panel.Controls.Add(pb);
            panel.Controls.Add(titleLbl);
            panel.Controls.Add(meta);
            panel.Controls.Add(exifLbl); // EXIF 라벨 추가

            _flp.Controls.Add(panel);
        }

        private bool ReadExif(MemoryStream ms)
        {
            try
            {
                ms.Position = 0; // 스트림 위치 리셋
                var directories = ImageMetadataReader.ReadMetadata(ms);

                var slug = new Dictionary<string, string>();
                var filterKeys = new HashSet<string>() { "Software", "Source", "Comment" };

                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        if (filterKeys.Contains(tag.Name))
                        {
                            slug[tag.Name] = tag.Description;
                        }
                        else if (tag.Name == "Textual Data")
                        {
                            var parts = tag.Description.Split(new[] { ':' }, 2);
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                string value = parts[1].Trim();
                                if (filterKeys.Contains(key))
                                {
                                    slug[key] = value;
                                }
                            }
                        }
                    }
                }

                if (slug.TryGetValue("Comment", out string commentJson))
                {
                    var metadata = JsonSerializer.Deserialize<RawParameters>(commentJson);
                    var simplified = new Simplified
                    {
                        Software = slug.GetValueOrDefault("Software"),
                        Source = IdentifyModel(slug.GetValueOrDefault("Source"), metadata.Prompt)
                    };

                    if (simplified.Software == "NovelAI")
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
        private string IdentifyModel(string software, string prompt)
        {
            string nameTag = "";

            var sf = software.Split(' ');
            var pr = prompt.Split(',');

            if (sf.Last() == "37C2B166") // V3 퍼리
                nameTag = software + "(NAI Diffusion Furry V3)";
            if (sf.Last() == "7BCCAA2C") // V3 아니메
                nameTag = software + "(NAI Diffusion Anime V3)";
            if (sf.Last() == "7ABFFA2A")
            {
                if (pr[0] == "fur dataset")
                    nameTag = software + "(NAI Diffusion V4 Curated + Furry)";
                else
                    nameTag = software + "(NAI Diffusion V4 Curated)";
            }   // V4 큐레이트 + 퍼리
            if (sf.Last() == "37442FCA")
            {
                if (pr[0] == "fur dataset")
                    nameTag = software + "(NAI Diffusion V4 Full + Furry)";
                else
                    nameTag = software + "(NAI Diffusion V4 Full)";
            }   // V4 풀 + 퍼리
            if (sf.Last() == "C02D4F98")
            {
                if (pr[0] == "fur dataset")
                    nameTag = software + "(NAI Diffusion V4.5 Curated + Furry)";
                else
                    nameTag = software + "(NAI Diffusion V4.5 Curated)";
            }   // V4.5 큐레이트 + 퍼리
            if (sf.Last() == "4BDE2A90")
            {
                if (pr[0] == "fur dataset")
                    nameTag = software + "(NAI Diffusion V4.5 Full + Furry)";
                else
                    nameTag = software + "(NAI Diffusion V4.5 Full)";
            }   // V4.5 풀 + 퍼리

            return nameTag;
        }
    }

    // --- (MainForm.Designer.cs - Bridge) ---
    public partial class MainForm
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.SetInstc();

            this.SuspendLayout();

            this.Controls.Add(this._pnlCt);
            this._pnlCt.Controls.Add(this._flp);

            this.SetLabel();
            this.SetInput();
            this.SetBtnID();
            this.SetSpcCtl();
            this.SetRunCtl();
            this.SetBtFnc();
            this.SetPnCnt();
            this.SetFlPnl();
            this.SetMForm();

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
    // --- (MainForm.Designer.cs - Bridge Elements) ---
    public partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label _lBs;
        private TextBox _tBs;
        private Button _btSav;

        private Label _lIn;
        private TextBox _tIn;
        private Button _btPrs;

        // 특수 토큰 컨트롤
        private Label _lblNam;
        private TextBox _tNam;

        private Label _lblSit;
        private TextBox _tSit;

        private Label _lblNum;
        private NumericUpDown _numSt;
        private NumericUpDown _numEn;

        // 실행 컨트롤
        private Label _lblPl;
        private NumericUpDown _numPl;
        private Button _btSt;
        private Button _btSp;

        private Panel _pnlCt;
        private FlowLayoutPanel _flp;

        private void SetInstc()
        {
            this._lBs = new Label();
            this._tBs = new TextBox();
            this._btSav = new Button();

            this._lIn = new Label();
            this._tIn = new TextBox();
            this._btPrs = new Button();

            this._pnlCt = new Panel();
            this._flp = new FlowLayoutPanel();

            // 특수 토큰
            this._lblNam = new Label();
            this._tNam = new TextBox();
            this._lblSit = new Label();
            this._tSit = new TextBox();
            this._lblNum = new Label();
            this._numSt = new NumericUpDown();
            this._numEn = new NumericUpDown();

            // 실행
            this._lblPl = new Label();
            this._numPl = new NumericUpDown();
            this._btSt = new Button();
            this._btSp = new Button();
        }
        private void SetLabel()
        {
            this._lBs.AutoSize = true;
            this._lBs.Location = new Point(12, 15);
            this._lBs.Name = "_lBs";
            this._lBs.Size = new Size(69, 12);
            this._lBs.Text = "베이스 URL:";

            this._lIn.AutoSize = true;
            this._lIn.Location = new Point(12, 37);
            this._lIn.Name = "_lIn";
            this._lIn.Size = new Size(69, 12);
            this._lIn.Text = "코드 부분  :";
        }
        private void SetInput()
        {
            this._tBs.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._tBs.Location = new Point(87, 12);
            this._tBs.Name = "_tBs";
            this._tBs.Size = new Size(310, 21);
            this._tBs.TabIndex = 0;
            this._tBs.PlaceholderText = "변하지 않는 고정 링크";


            this._tIn.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._tIn.Location = new Point(87, 34);
            this._tIn.Name = "_tIn";
            this._tIn.Size = new Size(310, 21);
            this._tIn.TabIndex = 2;
            this._tIn.PlaceholderText = "{토큰명}입력 후 파싱, name, num, situation은 특수 토큰";
        }
        private void SetBtnID()
        {
            this._btSav.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this._btSav.Location = new Point(398, 11);
            this._btSav.Name = "_btSav";
            this._btSav.Size = new Size(75, 21);
            this._btSav.TabIndex = 1;
            this._btSav.Text = "프리셋";
            this._btSav.UseVisualStyleBackColor = true;

            this._btPrs.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this._btPrs.Location = new Point(398, 33);
            this._btPrs.Name = "_btPrs";
            this._btPrs.Size = new Size(75, 21);
            this._btPrs.TabIndex = 3;
            this._btPrs.Text = "토큰 파싱";
            this._btPrs.UseVisualStyleBackColor = true;
        }
        private void SetSpcCtl()
        {
            // Row: Name
            this._lblNam.AutoSize = true;
            this._lblNam.Location = new Point(12, 60);
            this._lblNam.Name = "_lblNam";
            this._lblNam.Size = new Size(69, 12);
            this._lblNam.Text = "이름 목록:";
            this._tNam.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._tNam.Location = new Point(87, 57);
            this._tNam.Name = "_tNam";
            this._tNam.Size = new Size(386, 21);
            this._tNam.TabIndex = 4;
            this._tNam.PlaceholderText = "{name} 토큰 값 (쉼표로 구분)";

            // Row: Situation
            this._lblSit.AutoSize = true;
            this._lblSit.Location = new Point(12, 85);
            this._lblSit.Name = "_lblSit";
            this._lblSit.Size = new Size(69, 12);
            this._lblSit.Text = "상황 목록:";
            this._tSit.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._tSit.Location = new Point(87, 82);
            this._tSit.Name = "_tSit";
            this._tSit.Size = new Size(386, 21);
            this._tSit.TabIndex = 5;
            this._tSit.PlaceholderText = "{situation} 토큰 값 (쉼표로 구분)";

            // Row: Num
            this._lblNum.AutoSize = true;
            this._lblNum.Location = new Point(12, 110);
            this._lblNum.Name = "_lblNum";
            this._lblNum.Size = new Size(69, 12);
            this._lblNum.Text = "번호 범위:";
            this._numSt.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
            this._numSt.Location = new Point(87, 107);
            this._numSt.Name = "_numSt";
            this._numSt.Size = new Size(100, 21);
            this._numSt.TabIndex = 6;
            this._numSt.Maximum = 1000000;
            this._numEn.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
            this._numEn.Location = new Point(193, 107);
            this._numEn.Name = "_numEn";
            this._numEn.Size = new Size(100, 21);
            this._numEn.TabIndex = 7;
            this._numEn.Maximum = 1000000;
            this._numEn.Value = 0;
        }
        private void SetRunCtl()
        {
            // Row: Parallel & Start/Stop
            this._lblPl.AutoSize = true;
            this._lblPl.Location = new Point(12, 135);
            this._lblPl.Name = "_lblPl";
            this._lblPl.Size = new Size(69, 12);
            this._lblPl.Text = "동시 요청:";
            this._numPl.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
            this._numPl.Location = new Point(87, 132);
            this._numPl.Name = "_numPl";
            this._numPl.Size = new Size(70, 21);
            this._numPl.TabIndex = 8;
            this._numPl.Minimum = 1;
            this._numPl.Maximum = 64;
            this._numPl.Value = 1;

            this._btSt.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this._btSt.Location = new Point(317, 131);
            this._btSt.Name = "_btSt";
            this._btSt.Size = new Size(75, 23);
            this._btSt.TabIndex = 9;
            this._btSt.Text = "시작";
            this._btSt.UseVisualStyleBackColor = true;

            this._btSp.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this._btSp.Location = new Point(398, 131);
            this._btSp.Name = "_btSp";
            this._btSp.Size = new Size(75, 23);
            this._btSp.TabIndex = 10;
            this._btSp.Text = "중지";
            this._btSp.Enabled = false;
            this._btSp.UseVisualStyleBackColor = true;
        }

        private void SetBtFnc()
        {
            this._btSav.Click += new EventHandler((_, _) => this.SaveConfig());
            this._btPrs.Click += new EventHandler((_, _) => this.RunParsing());

            // 시작/중지 버튼 함수
            this._btSt.Click += new EventHandler((_, _) => this.StartFetching());
            this._btSp.Click += new EventHandler((_, _) => this.StopFetching());
        }
        private void SetPnCnt()
        {
            this._pnlCt.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this._pnlCt.Location = new Point(12, 162);
            this._pnlCt.Name = "_pnlCt";
            this._pnlCt.Size = new Size(460, 87);
            this._pnlCt.TabIndex = 11;
            this._pnlCt.BorderStyle = BorderStyle.FixedSingle;

            this._pnlCt.AutoScroll = true;
        }
        private void SetFlPnl()
        {
            this._flp.Dock = DockStyle.Top;
            this._flp.AutoScroll = false;
            this._flp.WrapContents = true;
            this._flp.AutoSize = true;
            this._flp.FlowDirection = FlowDirection.LeftToRight;
            this._flp.Location = new Point(0, 0);
            this._flp.Name = "_flp";
            this._flp.Size = new Size(458, 0);
        }
        private void SetMForm()
        {
            this.AutoScaleDimensions = new SizeF(7F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(484, 261);

            this.Controls.Add(this._lBs);
            this.Controls.Add(this._tBs);
            this.Controls.Add(this._btSav);

            this.Controls.Add(this._lIn);
            this.Controls.Add(this._tIn);
            this.Controls.Add(this._btPrs);

            // 특수 토큰 컨트롤
            this.Controls.Add(this._lblNam);
            this.Controls.Add(this._tNam);
            this.Controls.Add(this._lblSit);
            this.Controls.Add(this._tSit);
            this.Controls.Add(this._lblNum);
            this.Controls.Add(this._numSt);
            this.Controls.Add(this._numEn);

            // 실행 컨트롤
            this.Controls.Add(this._lblPl);
            this.Controls.Add(this._numPl);
            this.Controls.Add(this._btSt);
            this.Controls.Add(this._btSp);

            this.MinimumSize = new Size(500, 300);

            this.Name = "MainForm";
            this.Text = "이미지 호스팅 체크 V1.1";
        }
    }
}