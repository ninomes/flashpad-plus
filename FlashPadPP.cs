using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace FlashPadPP
{
    internal class Page
    {
        public int Order = 0;
        public string Title = "";
        public string Content = "";
        public long Created = 0;
        public long Modified = 0;
        public string Id = "";
    }

    internal static class Store
    {
        public const char Sep = '\u0001';
        public const string Magic = "FLASHPADX 1.0";

        public static List<Page> Read(string path)
        {
            List<Page> pages = new List<Page>();
            if (!File.Exists(path)) return pages;

            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.UTF8); }
            catch { return pages; }

            string marker = Sep + "PAGE" + Sep;
            Page cur = null;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] f = null;
                if (line.Length > 0 && line.StartsWith(marker, StringComparison.Ordinal))
                {
                    string[] cand = line.Split(Sep);
                    if (cand.Length >= 7) f = cand;
                }

                if (f != null)
                {
                    if (cur != null)
                    {
                        cur.Content = sb.ToString().TrimEnd('\r', '\n');
                        pages.Add(cur);
                    }
                    sb.Length = 0;
                    cur = new Page();
                    int o = 0;
                    int.TryParse(f[2], out o);
                    cur.Order = o;
                    cur.Title = f[3];
                    long c = 0;
                    long.TryParse(f[4], out c);
                    cur.Created = c;
                    long m = 0;
                    long.TryParse(f[5], out m);
                    cur.Modified = m;
                    cur.Id = f[6];
                }
                else if (cur != null)
                {
                    sb.AppendLine(line);
                }
            }

            if (cur != null)
            {
                cur.Content = sb.ToString().TrimEnd('\r', '\n');
                pages.Add(cur);
            }
            return pages;
        }

        public static void Save(string path, List<Page> pages)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Magic);

            for (int i = 0; i < pages.Count; i++)
            {
                Page p = pages[i];
                p.Order = i;
                sb.Append(Sep).Append("PAGE").Append(Sep);
                sb.Append(p.Order).Append(Sep);
                sb.Append(p.Title).Append(Sep);
                sb.Append(p.Created).Append(Sep);
                sb.Append(p.Modified).Append(Sep);
                sb.Append(p.Id);
                sb.AppendLine();
                sb.Append(p.Content);
                sb.AppendLine();
            }

            string text = sb.ToString();
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text, new UTF8Encoding(true));

            if (File.Exists(path))
            {
                try { File.Replace(tmp, path, null); }
                catch
                {
                    try { File.Copy(tmp, path, true); File.Delete(tmp); }
                    catch { }
                }
            }
            else
            {
                try { File.Move(tmp, path); }
                catch { }
            }
        }

        public static void Backup(string path, int keep)
        {
            if (!File.Exists(path)) return;
            string dir = Path.Combine(Path.GetDirectoryName(path), "backup");
            try { if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); }
            catch { return; }

            string dest = Path.Combine(dir, "flashpadx-" + DateTime.Now.ToString("yyyyMMdd") + ".dat");
            if (!File.Exists(dest))
            {
                try { File.Copy(path, dest, true); }
                catch { return; }
            }

            string[] files;
            try { files = Directory.GetFiles(dir, "*.dat"); }
            catch { return; }
            Array.Sort(files);
            Array.Reverse(files);
            for (int i = keep; i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { }
            }
        }

        public static List<Page> ImportFlashPad(string dir)
        {
            List<Page> pages = new List<Page>();
            if (!Directory.Exists(dir)) return pages;

            string[] files;
            try { files = Directory.GetFiles(dir, "*.txt"); }
            catch { return pages; }

            List<Page> tmp = new List<Page>();

            for (int i = 0; i < files.Length; i++)
            {
                string[] lines;
                try { lines = File.ReadAllLines(files[i], Encoding.Unicode); }
                catch { continue; }
                if (lines.Length == 0) continue;

                int order = 0;
                int.TryParse(lines[0].Trim(), out order);

                string title = "未命名";
                if (lines.Length > 1)
                {
                    string t = lines[1].Trim();
                    if (t.Length > 0) title = t;
                }

                string body = "";
                if (lines.Length > 2)
                {
                    StringBuilder bs = new StringBuilder();
                    for (int k = 2; k < lines.Length; k++)
                    {
                        if (k > 2) bs.Append("\r\n");
                        bs.Append(lines[k]);
                    }
                    body = bs.ToString().TrimEnd('\r', '\n');
                }

                Page p = new Page();
                p.Order = order;
                p.Title = title;
                p.Content = body;
                p.Created = File.GetCreationTime(files[i]).Ticks;
                p.Modified = File.GetLastWriteTime(files[i]).Ticks;
                p.Id = Path.GetFileNameWithoutExtension(files[i]).ToUpperInvariant();
                tmp.Add(p);
            }

            tmp.Sort(delegate(Page a, Page b) { return a.Order.CompareTo(b.Order); });
            for (int i = 0; i < tmp.Count; i++)
            {
                tmp[i].Order = i;
                pages.Add(tmp[i]);
            }
            return pages;
        }

        public static Dictionary<string, string> ReadIni(string path)
        {
            Dictionary<string, string> h = new Dictionary<string, string>();
            if (!File.Exists(path)) return h;

            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.UTF8); }
            catch { return h; }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int eq = line.IndexOf('=');
                if (eq > 0)
                {
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    if (k.Length > 0) h[k] = v;
                }
            }
            return h;
        }

        public static void WriteIni(string path, Dictionary<string, string> h)
        {
            List<string> keys = new List<string>(h.Keys);
            keys.Sort(StringComparer.Ordinal);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < keys.Count; i++) sb.AppendLine(keys[i] + "=" + h[keys[i]]);
            try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); }
            catch { }
        }
    }

    internal class MainForm : Form
    {
        private const int HotKeyId = 1;
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 全局热键候选表。首选 Ctrl+Alt+F；被别的软件占用时按顺序往后试，
        // 状态栏会显示最终生效的那一个，不用用户自己去猜。
        private static readonly uint[] HotMods = new uint[]
        {
            MOD_CONTROL | MOD_ALT,
            MOD_CONTROL | MOD_ALT,
            MOD_CONTROL | MOD_ALT,
            MOD_CONTROL | MOD_SHIFT,
            MOD_CONTROL | MOD_ALT
        };
        private static readonly uint[] HotVks = new uint[]
        {
            0x46, // F
            0x51, // Q
            0x50, // P
            0x46, // F
            0x57  // W
        };
        private static readonly string[] HotNames = new string[]
        {
            "Ctrl+Alt+F",
            "Ctrl+Alt+Q",
            "Ctrl+Alt+P",
            "Ctrl+Shift+F",
            "Ctrl+Alt+W"
        };

        private List<Page> _pages = new List<Page>();
        private int _cur = -1;
        private bool _dirty = false;
        private bool _suspend = false;
        private bool _reallyExit = false;
        private bool _closeTipShown = false;
        private bool _hotkeyOk = false;
        private int _hotkeyIdx = -1;
        private string _hotkeyName = "";
        private string _baseDir;
        private string _dataFile;
        private string _iniFile;
        private Dictionary<string, string> _cfg = new Dictionary<string, string>();

        private SplitContainer _split;
        private ListBox _lst;
        private TextBox _txt;
        private StatusStrip _status;
        private ToolStripStatusLabel _lblStatus;
        private ToolStripStatusLabel _lblCount;
        private MenuStrip _menu;
        private ToolStripMenuItem _miNewPage;
        private ToolStripMenuItem _miRename;
        private ToolStripMenuItem _miDelPage;
        private ToolStripMenuItem _miImport;
        private ToolStripMenuItem _miExit;
        private ToolStripMenuItem _miFind;
        private ToolStripMenuItem _miDate;
        private ToolStripMenuItem _miLine;
        private ToolStripMenuItem _miLineDate;
        private ToolStripMenuItem _miBlank;
        private ToolStripMenuItem _miTop;
        private ToolStripMenuItem _miWrap;
        private ToolStripMenuItem _miFont;
        private ToolStripMenuItem _miTextColor;
        private ToolStripMenuItem _miBgColor;
        private ToolStripMenuItem _miSaveNow;
        private NotifyIcon _tray;
        private Timer _saveTimer;
        private ContextMenuStrip _lstMenu;

        public MainForm()
        {
            _baseDir = Application.StartupPath;
            _dataFile = Path.Combine(_baseDir, "flashpadx.dat");
            _iniFile = Path.Combine(_baseDir, "flashpadx.ini");

            BuildUi();
            LoadConfig();
            ApplyConfig();

            _pages = Store.Read(_dataFile);
            if (_pages.Count == 0)
            {
                List<Page> imported = Store.ImportFlashPad(@"D:\FlashPad\Data");
                if (imported.Count > 0) _pages = imported;
            }
            if (_pages.Count == 0)
            {
                Page p = NewPage("默认页");
                _pages.Add(p);
            }

            Store.Backup(_dataFile, 30);

            // 启动固定落到列表最末一项（编号最大的那一页，也就是“最后一条数据”），
            // 光标停在文末，打开就能往下写，不用每次手动翻到底。
            // 注意：这里不能用“修改时间最新的页”来定位——修改时间会被新建/重命名/导入
            // 等操作刷新，排在上面的旧页也可能变成“最新”，于是会跳到 26 而不是 27。
            int start = _pages.Count - 1;
            if (start < 0) start = 0;
            _cur = start;
            RefreshList();
            LoadPage(start);
            try { _lst.TopIndex = start; }
            catch { }
            try
            {
                _txt.SelectionStart = _txt.TextLength;
                _txt.SelectionLength = 0;
                _txt.ScrollToCaret();
            }
            catch { }
            SaveNow();
            SetStatus("就绪");
        }

        private void BuildUi()
        {
            this.Text = "FlashPad++";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(640, 420);
            this.Font = new Font("宋体", 12f);

            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int dw = Math.Min(1240, Math.Max(900, (int)Math.Round(wa.Width * 0.80)));
            int dh = Math.Min(840, Math.Max(620, (int)Math.Round(wa.Height * 0.86)));
            this.Width = dw;
            this.Height = dh;

            _menu = new MenuStrip();

            ToolStripMenuItem miFile = new ToolStripMenuItem("文件(&F)");
            _miNewPage = new ToolStripMenuItem("新建页面(&N)");
            _miNewPage.ShortcutKeys = Keys.Control | Keys.N;
            _miRename = new ToolStripMenuItem("重命名当前页面(&R)");
            _miRename.ShortcutKeys = Keys.F2;
            _miDelPage = new ToolStripMenuItem("删除当前页面");
            _miImport = new ToolStripMenuItem("从 FlashPad 导入…");
            ToolStripMenuItem miOpenDir = new ToolStripMenuItem("打开数据文件夹");
            _miExit = new ToolStripMenuItem("退出(&X)");
            miFile.DropDownItems.Add(_miNewPage);
            miFile.DropDownItems.Add(_miRename);
            miFile.DropDownItems.Add(_miDelPage);
            miFile.DropDownItems.Add(new ToolStripSeparator());
            miFile.DropDownItems.Add(_miImport);
            miFile.DropDownItems.Add(miOpenDir);
            miFile.DropDownItems.Add(new ToolStripSeparator());
            miFile.DropDownItems.Add(_miExit);

            ToolStripMenuItem miEdit = new ToolStripMenuItem("编辑(&E)");
            _miFind = new ToolStripMenuItem("查找(&F)");
            _miFind.ShortcutKeys = Keys.Control | Keys.F;
            _miDate = new ToolStripMenuItem("插入日期时间");
            _miDate.ShortcutKeys = Keys.Control | Keys.D;
            _miLine = new ToolStripMenuItem("插入分隔线");
            _miLineDate = new ToolStripMenuItem("插入带日期的分隔线");
            _miBlank = new ToolStripMenuItem("插入空行");
            miEdit.DropDownItems.Add(_miFind);
            miEdit.DropDownItems.Add(new ToolStripSeparator());
            miEdit.DropDownItems.Add(_miDate);
            miEdit.DropDownItems.Add(_miLine);
            miEdit.DropDownItems.Add(_miLineDate);
            miEdit.DropDownItems.Add(_miBlank);

            ToolStripMenuItem miView = new ToolStripMenuItem("视图(&V)");
            _miTop = new ToolStripMenuItem("窗口置顶");
            _miTop.CheckOnClick = true;
            _miWrap = new ToolStripMenuItem("自动换行");
            _miWrap.CheckOnClick = true;
            _miFont = new ToolStripMenuItem("字体…");
            _miTextColor = new ToolStripMenuItem("文字颜色…");
            _miBgColor = new ToolStripMenuItem("背景颜色…");
            _miSaveNow = new ToolStripMenuItem("立即保存");
            _miSaveNow.ShortcutKeys = Keys.Control | Keys.S;
            miView.DropDownItems.Add(_miTop);
            miView.DropDownItems.Add(_miWrap);
            miView.DropDownItems.Add(new ToolStripSeparator());
            miView.DropDownItems.Add(_miFont);
            miView.DropDownItems.Add(_miTextColor);
            miView.DropDownItems.Add(_miBgColor);
            miView.DropDownItems.Add(new ToolStripSeparator());
            miView.DropDownItems.Add(_miSaveNow);

            ToolStripMenuItem miHelp = new ToolStripMenuItem("帮助(&H)");
            ToolStripMenuItem miAbout = new ToolStripMenuItem("关于");
            miHelp.DropDownItems.Add(miAbout);

            _menu.Items.Add(miFile);
            _menu.Items.Add(miEdit);
            _menu.Items.Add(miView);
            _menu.Items.Add(miHelp);

            _status = new StatusStrip();
            _lblStatus = new ToolStripStatusLabel("就绪");
            _lblStatus.Spring = true;
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblCount = new ToolStripStatusLabel("");
            _status.Items.Add(_lblStatus);
            _status.Items.Add(_lblCount);

            _txt = new TextBox();
            _txt.Multiline = true;
            _txt.Dock = DockStyle.Fill;
            _txt.ScrollBars = ScrollBars.Vertical;
            _txt.WordWrap = true;
            _txt.AcceptsReturn = true;
            _txt.AcceptsTab = true;
            _txt.MaxLength = 0;
            _txt.HideSelection = false;
            _txt.BorderStyle = BorderStyle.None;
            _txt.BackColor = Color.White;
            _txt.ForeColor = Color.Black;
            _txt.Font = this.Font;

            _lst = new ListBox();
            _lst.Dock = DockStyle.Fill;
            _lst.DrawMode = DrawMode.OwnerDrawFixed;
            _lst.ItemHeight = 22;
            _lst.BorderStyle = BorderStyle.None;
            _lst.IntegralHeight = false;

            FlowLayoutPanel btnPanel = new FlowLayoutPanel();
            btnPanel.Dock = DockStyle.Bottom;
            btnPanel.Height = 34;
            btnPanel.Padding = new Padding(2, 3, 2, 2);

            Button btnNew = MakeButton("新建", 56);
            Button btnDel = MakeButton("删除", 56);
            Button btnUp = MakeButton("↑", 34);
            Button btnDown = MakeButton("↓", 34);
            btnPanel.Controls.Add(btnNew);
            btnPanel.Controls.Add(btnDel);
            btnPanel.Controls.Add(btnUp);
            btnPanel.Controls.Add(btnDown);

            Panel paneLine = new Panel();
            paneLine.Dock = DockStyle.Right;
            paneLine.Width = 1;
            paneLine.BackColor = Color.FromArgb(196, 196, 196);

            _split = new SplitContainer();
            _split.Dock = DockStyle.Fill;
            _split.Orientation = Orientation.Vertical;
            _split.SplitterWidth = 4;
            _split.BackColor = Color.FromArgb(214, 214, 214);
            _split.Panel1.Controls.Add(_lst);
            _split.Panel1.Controls.Add(btnPanel);
            _split.Panel1.Controls.Add(paneLine);
            _split.Panel2.Controls.Add(_txt);

            // 正文与中间分隔线之间留出约两个字宽的空档，贴边太挤；
            // 字体改变（菜单里调字号）时按新字号重新算一遍。
            _txt.FontChanged += delegate { ApplyTextInsets(); };
            ApplyTextInsets();
            // 留白区域必须跟正文同色，否则左侧会露出一条灰带
            _split.Panel2.BackColor = _txt.BackColor;
            _txt.BackColorChanged += delegate { _split.Panel2.BackColor = _txt.BackColor; };

            this.Controls.Add(_split);
            this.Controls.Add(_menu);
            this.Controls.Add(_status);
            this.MainMenuStrip = _menu;

            _lstMenu = new ContextMenuStrip();
            ToolStripMenuItem lmRen = new ToolStripMenuItem("重命名");
            lmRen.ShortcutKeyDisplayString = "F2";
            ToolStripMenuItem lmNew = new ToolStripMenuItem("新建页面");
            ToolStripMenuItem lmDel = new ToolStripMenuItem("删除页面");
            _lstMenu.Items.Add(lmRen);
            _lstMenu.Items.Add(new ToolStripSeparator());
            _lstMenu.Items.Add(lmNew);
            _lstMenu.Items.Add(lmDel);
            _lst.ContextMenuStrip = _lstMenu;

            _tray = new NotifyIcon();
            _tray.Icon = this.Icon != null ? this.Icon : SystemIcons.Application;
            _tray.Text = "FlashPad++";
            _tray.Visible = true;
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem tShow = new ToolStripMenuItem("显示");
            ToolStripMenuItem tHot = new ToolStripMenuItem("重新注册热键");
            ToolStripMenuItem tExit = new ToolStripMenuItem("退出");
            trayMenu.Items.Add(tShow);
            trayMenu.Items.Add(tHot);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(tExit);
            _tray.ContextMenuStrip = trayMenu;

            // 别的程序后来释放了组合键时，用户不用重启程序，点这一项重试即可。
            tHot.Click += delegate
            {
                TryRegisterHotKey();
                RefreshHotKeyStatus();
                if (_hotkeyOk)
                    _tray.ShowBalloonTip(2000, "热键已生效", _hotkeyName + " 可以随时呼出窗口", ToolTipIcon.Info);
                else
                    _tray.ShowBalloonTip(2500, "热键仍不可用", "候选的几种组合都被占用了。可先用托盘图标打开。", ToolTipIcon.Warning);
            };

            _saveTimer = new Timer();
            _saveTimer.Interval = 1200;

            btnNew.Click += delegate { DoNewPage(); };
            btnDel.Click += delegate { DoDeletePage(); };
            btnUp.Click += delegate { DoMove(-1); };
            btnDown.Click += delegate { DoMove(1); };

            _miNewPage.Click += delegate { DoNewPage(); };
            _miRename.Click += delegate { DoRename(); };
            _miDelPage.Click += delegate { DoDeletePage(); };
            _miImport.Click += delegate { DoImport(); };
            _miExit.Click += delegate { _reallyExit = true; this.Close(); };
            miOpenDir.Click += delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", _baseDir); }
                catch { }
            };

            _miFind.Click += delegate { DoFind(); };
            _miDate.Click += delegate { InsertText(DateTime.Now.ToString("yyyy-MM-dd HH:mm")); };
            _miLine.Click += delegate { InsertText(new string('-', 72)); };
            _miLineDate.Click += delegate
            {
                string line = new string('-', 28) + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " " + new string('-', 28);
                InsertText("\r\n" + line + "\r\n");
            };
            _miBlank.Click += delegate { InsertText("\r\n"); };

            _miTop.CheckedChanged += delegate { this.TopMost = _miTop.Checked; };
            _miWrap.CheckedChanged += delegate { _txt.WordWrap = _miWrap.Checked; };
            _miFont.Click += delegate
            {
                FontDialog fd = new FontDialog();
                fd.Font = _txt.Font;
                if (fd.ShowDialog() == DialogResult.OK) _txt.Font = fd.Font;
            };
            _miTextColor.Click += delegate
            {
                ColorDialog cd = new ColorDialog();
                cd.Color = _txt.ForeColor;
                if (cd.ShowDialog() == DialogResult.OK) _txt.ForeColor = cd.Color;
            };
            _miBgColor.Click += delegate
            {
                ColorDialog cd = new ColorDialog();
                cd.Color = _txt.BackColor;
                if (cd.ShowDialog() == DialogResult.OK) _txt.BackColor = cd.Color;
            };
            _miSaveNow.Click += delegate { CommitCurrent(); SaveNow(); SetStatus("已保存"); };

            miAbout.Click += delegate
            {
                MessageBox.Show(
                    "FlashPad++ 1.0\r\n\r\n单文件存储的快速便签本。\r\nCtrl+Alt+F 全局呼出，F2 重命名页面。\r\n数据文件：" + _dataFile,
                    "FlashPad++", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            lmRen.Click += delegate { DoRename(); };
            lmNew.Click += delegate { DoNewPage(); };
            lmDel.Click += delegate { DoDeletePage(); };

            _lstMenu.Opening += delegate(object s, System.ComponentModel.CancelEventArgs e)
            {
                Point pt = _lst.PointToClient(Cursor.Position);
                int idx = _lst.IndexFromPoint(pt);
                if (idx < 0)
                {
                    e.Cancel = true;
                    return;
                }
                if (idx != _lst.SelectedIndex) _lst.SelectedIndex = idx;
            };

            tShow.Click += delegate { ShowApp(); };
            tExit.Click += delegate { _reallyExit = true; this.Close(); };
            _tray.DoubleClick += delegate { ShowApp(); };

            _lst.DrawItem += new DrawItemEventHandler(LstDrawItem);
            _lst.SelectedIndexChanged += delegate { SelectPage(_lst.SelectedIndex); };
            _lst.DoubleClick += delegate { DoRename(); };

            _txt.TextChanged += delegate
            {
                if (_suspend) return;
                _dirty = true;
                _saveTimer.Stop();
                _saveTimer.Start();
                UpdateCount();
                SetStatus("编辑中…");
            };

            _saveTimer.Tick += delegate
            {
                _saveTimer.Stop();
                if (_dirty)
                {
                    CommitCurrent();
                    SaveNow();
                }
            };

            this.Shown += delegate { try { _split.SplitterDistance = 260; } catch { } };
        }

        private static Button MakeButton(string text, int width)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = width;
            b.Height = 26;
            b.FlatStyle = FlatStyle.System;
            return b;
        }

        // 正文区留白：左边两个字宽，上下一半字高，右边一个字宽。
        // 字号或字体一变就按新字宽重算，避免固定像素在小字号下过大、大字号下过小。
        private void ApplyTextInsets()
        {
            try
            {
                int ch = TextRenderer.MeasureText("中中", _txt.Font).Width / 2;
                if (ch < 6) { ch = 6; }
                int padTop = Math.Max(6, ch / 2);
                // 右边不设内边距：滚动条要贴住窗口右边，缩进会像一根飘着的条
                _split.Panel2.Padding = new Padding(ch * 2, padTop, 0, 4);
            }
            catch { }
        }

        private void LoadConfig()
        {
            _cfg = Store.ReadIni(_iniFile);

            int w = CfgInt("w", 0);
            int h = CfgInt("h", 0);
            if (w > 300 && h > 200)
            {
                this.Width = w;
                this.Height = h;
            }

            int l = CfgInt("l", -1);
            int t = CfgInt("t", -1);
            if (l >= 0 && t >= 0)
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                if (l < wa.Width - 100 && t < wa.Height - 100)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Left = l;
                    this.Top = t;
                }
            }
        }

        private void ApplyConfig()
        {
            string fn = CfgStr("font", "宋体");
            float fs = 12f;
            try
            {
                string s = CfgStr("fontsize", "12");
                float tmp;
                if (float.TryParse(s, out tmp) && tmp >= 6f && tmp <= 72f) fs = tmp;
            }
            catch { }

            int fst = CfgInt("fontstyle", 0);
            try { _txt.Font = new Font(fn, fs, (FontStyle)fst); }
            catch { _txt.Font = new Font("宋体", 12f); }

            try { _txt.ForeColor = Color.FromArgb(CfgInt("textcolor", -16777216)); }
            catch { }
            try { _txt.BackColor = Color.FromArgb(CfgInt("backcolor", -1)); }
            catch { }

            _txt.WordWrap = CfgBool("wordwrap", true);
            _miWrap.Checked = _txt.WordWrap;
            bool top = CfgBool("topmost", false);
            _miTop.Checked = top;
            this.TopMost = top;
        }

        private string CfgStr(string k, string d)
        {
            if (_cfg.ContainsKey(k) && _cfg[k] != null && _cfg[k].Length > 0) return _cfg[k];
            return d;
        }

        private int CfgInt(string k, int d)
        {
            if (_cfg.ContainsKey(k))
            {
                int v;
                if (int.TryParse(_cfg[k], out v)) return v;
            }
            return d;
        }

        private bool CfgBool(string k, bool d)
        {
            if (_cfg.ContainsKey(k)) return _cfg[k] == "1";
            return d;
        }

        private static Page NewPage(string title)
        {
            Page p = new Page();
            p.Title = title;
            p.Content = "";
            p.Created = DateTime.Now.Ticks;
            p.Modified = DateTime.Now.Ticks;
            p.Id = Guid.NewGuid().ToString("D").ToUpperInvariant();
            return p;
        }

        private void SetStatus(string text)
        {
            _lblStatus.Text = text;
        }

        private void UpdateCount()
        {
            _lblCount.Text = "本页 " + _txt.TextLength + " 字  |  共 " + _pages.Count + " 页";
        }

        private void RefreshList()
        {
            _suspend = true;
            _lst.BeginUpdate();
            _lst.Items.Clear();
            for (int i = 0; i < _pages.Count; i++) _lst.Items.Add(_pages[i].Title);
            _lst.EndUpdate();
            if (_cur >= 0 && _cur < _lst.Items.Count) _lst.SelectedIndex = _cur;
            _suspend = false;
            UpdateCount();
            _lst.Invalidate();
        }

        private void LstDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _pages.Count) return;
            Page p = _pages[e.Index];

            e.DrawBackground();

            string label = string.Format("{0,2}  {1}", e.Index + 1, p.Title);
            if (p.Content.Length == 0) label = label + "  ·";

            using (SolidBrush brush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(label, e.Font, brush, e.Bounds.Left + 4, e.Bounds.Top + 3);
            }
            e.DrawFocusRectangle();
        }

        private void SelectPage(int i)
        {
            if (_suspend) return;
            if (i < 0 || i == _cur) return;
            CommitCurrent();
            LoadPage(i);
        }

        private void LoadPage(int i)
        {
            if (i < 0 || i >= _pages.Count) return;
            _suspend = true;
            _txt.Text = _pages[i].Content;
            _txt.SelectionStart = 0;
            _txt.SelectionLength = 0;
            _suspend = false;
            _cur = i;
            _dirty = false;
            UpdateCount();
        }

        private void CommitCurrent()
        {
            if (_cur < 0 || _cur >= _pages.Count) return;
            Page p = _pages[_cur];
            if (p.Content != _txt.Text)
            {
                p.Content = _txt.Text;
                p.Modified = DateTime.Now.Ticks;
            }
        }

        private void SaveNow()
        {
            Store.Save(_dataFile, _pages);
            _dirty = false;
        }

        private void DoNewPage()
        {
            CommitCurrent();
            Page p = NewPage("新建页(" + (_pages.Count + 1) + ")");
            _pages.Add(p);
            _cur = _pages.Count - 1;
            RefreshList();
            LoadPage(_cur);
            SaveNow();
            _txt.Focus();
            SetStatus("已新建页面");
        }

        private void DoDeletePage()
        {
            if (_cur < 0) return;
            if (_pages.Count <= 1)
            {
                MessageBox.Show("至少要保留一个页面。", "FlashPad++");
                return;
            }
            string name = _pages[_cur].Title;
            DialogResult r = MessageBox.Show("确定删除页面「" + name + "」？此操作不可撤销。", "FlashPad++", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            _pages.RemoveAt(_cur);
            if (_cur >= _pages.Count) _cur = _pages.Count - 1;
            RefreshList();
            LoadPage(_cur);
            SaveNow();
            SetStatus("已删除");
        }

        private void DoRename()
        {
            if (_cur < 0) return;
            string old = _pages[_cur].Title;
            string v = Prompt.Show("重命名页面", "页面名称：", old);
            if (v == null) return;
            v = v.Trim();
            if (v.Length == 0) return;
            _pages[_cur].Title = v;
            _pages[_cur].Modified = DateTime.Now.Ticks;
            RefreshList();
            SaveNow();
            SetStatus("已重命名");
        }

        private void DoMove(int delta)
        {
            if (_cur < 0) return;
            int target = _cur + delta;
            if (target < 0 || target >= _pages.Count) return;
            CommitCurrent();
            Page tmp = _pages[_cur];
            _pages.RemoveAt(_cur);
            _pages.Insert(target, tmp);
            _cur = target;
            RefreshList();
            LoadPage(_cur);
            SaveNow();
        }

        private void DoImport()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "选择 FlashPad 的 Data 目录";
            if (Directory.Exists(@"D:\FlashPad")) dlg.SelectedPath = @"D:\FlashPad";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string src = dlg.SelectedPath;
            string sub = Path.Combine(src, "Data");
            if (Directory.Exists(sub)) src = sub;

            List<Page> add = Store.ImportFlashPad(src);
            if (add.Count == 0)
            {
                MessageBox.Show("该目录里没有找到可导入的页面。", "FlashPad++");
                return;
            }
            for (int i = 0; i < add.Count; i++) _pages.Add(add[i]);
            RefreshList();
            SaveNow();
            MessageBox.Show("已导入 " + add.Count + " 个页面。", "FlashPad++");
        }

        private void DoFind()
        {
            string kw = Prompt.Show("查找", "查找内容：", "");
            if (kw == null || kw.Length == 0) return;

            int start = _txt.SelectionStart + _txt.SelectionLength;
            if (start >= _txt.TextLength) start = 0;

            int idx = _txt.Text.IndexOf(kw, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0 && start > 0) idx = _txt.Text.IndexOf(kw, 0, StringComparison.OrdinalIgnoreCase);

            if (idx < 0)
            {
                MessageBox.Show("没有找到指定内容。", "FlashPad++");
                return;
            }
            _txt.SelectionStart = idx;
            _txt.SelectionLength = kw.Length;
            _txt.ScrollToCaret();
            _txt.Focus();
            SetStatus("已找到");
        }

        private void InsertText(string s)
        {
            int pos = _txt.SelectionStart;
            _txt.SelectedText = s;
            _txt.SelectionStart = pos + s.Length;
            _txt.Focus();
        }

        private void ShowApp()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();
        }

        // 从候选表里挑第一个能注册成功的；全部失败返回 false。
        private bool TryRegisterHotKey()
        {
            ReleaseHotKey();
            for (int i = 0; i < HotNames.Length; i++)
            {
                bool ok = false;
                try
                {
                    ok = RegisterHotKey(this.Handle, HotKeyId, HotMods[i], HotVks[i]);
                }
                catch { ok = false; }
                if (ok)
                {
                    _hotkeyIdx = i;
                    _hotkeyName = HotNames[i];
                    _hotkeyOk = true;
                    return true;
                }
            }
            _hotkeyIdx = -1;
            _hotkeyName = "";
            _hotkeyOk = false;
            return false;
        }

        private void ReleaseHotKey()
        {
            if (_hotkeyIdx < 0) return;
            try { UnregisterHotKey(this.Handle, HotKeyId); } catch { }
            _hotkeyIdx = -1;
        }

        private void RefreshHotKeyStatus()
        {
            if (_hotkeyOk) SetStatus("就绪  |  " + _hotkeyName + " 呼出");
            else SetStatus("就绪  |  热键未生效（组合键被占用），可从托盘图标打开");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            TryRegisterHotKey();
            RefreshHotKeyStatus();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotKeyId)
            {
                ShowApp();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                if (!_closeTipShown)
                {
                    _closeTipShown = true;
                    _tray.ShowBalloonTip(2500, "FlashPad++ 还在运行", "已最小化到任务栏。要真正退出，请右键托盘图标 → 退出。", ToolTipIcon.Info);
                }
                return;
            }

            CommitCurrent();
            SaveNow();

            if (this.WindowState == FormWindowState.Normal)
            {
                _cfg["l"] = this.Left.ToString();
                _cfg["t"] = this.Top.ToString();
                _cfg["w"] = this.Width.ToString();
                _cfg["h"] = this.Height.ToString();
            }
            _cfg["font"] = _txt.Font.Name;
            _cfg["fontsize"] = ((int)_txt.Font.Size).ToString();
            _cfg["fontstyle"] = ((int)_txt.Font.Style).ToString();
            _cfg["textcolor"] = _txt.ForeColor.ToArgb().ToString();
            _cfg["backcolor"] = _txt.BackColor.ToArgb().ToString();
            _cfg["wordwrap"] = _miWrap.Checked ? "1" : "0";
            _cfg["topmost"] = _miTop.Checked ? "1" : "0";
            Store.WriteIni(_iniFile, _cfg);

            ReleaseHotKey();
            _tray.Visible = false;
            _tray.Dispose();

            base.OnFormClosing(e);
        }
    }

    internal static class Prompt
    {
        public static string Show(string title, string label, string def)
        {
            Form f = new Form();
            f.Text = title;
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.StartPosition = FormStartPosition.CenterParent;
            f.MinimizeBox = false;
            f.MaximizeBox = false;
            f.ShowInTaskbar = false;
            f.ClientSize = new Size(360, 118);
            f.Font = new Font("宋体", 12f);

            Label lb = new Label();
            lb.Text = label;
            lb.SetBounds(12, 12, 330, 22);

            TextBox tb = new TextBox();
            tb.Text = def;
            tb.SetBounds(12, 40, 330, 24);

            Button ok = new Button();
            ok.Text = "确定";
            ok.DialogResult = DialogResult.OK;
            ok.SetBounds(176, 76, 74, 26);

            Button cc = new Button();
            cc.Text = "取消";
            cc.DialogResult = DialogResult.Cancel;
            cc.SetBounds(258, 76, 74, 26);

            f.Controls.Add(lb);
            f.Controls.Add(tb);
            f.Controls.Add(ok);
            f.Controls.Add(cc);
            f.AcceptButton = ok;
            f.CancelButton = cc;

            if (f.ShowDialog() == DialogResult.OK) return tb.Text;
            return null;
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created = false;
            System.Threading.Mutex mutex = new System.Threading.Mutex(true, "FlashPadX_SingleInstance", out created);
            if (!created)
            {
                MessageBox.Show("FlashPad++ 已经在运行了。", "FlashPad++");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            GC.KeepAlive(mutex);
        }
    }
}
