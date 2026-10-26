using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MonitorHub
{
    // ===================== WIN32 =====================
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields;
        public int dmPositionX, dmPositionY;
        public int dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }


    [StructLayout(LayoutKind.Sequential)] public struct LUID { public uint Low; public int High; }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_INFO
    {
        public LUID srcAdapter; public uint srcId, srcMode, srcFlags;
        public LUID tgtAdapter; public uint tgtId, tgtMode, tech, rot, scal, rNum, rDen, scan, avail, tgtFlags;
        public uint flags;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)] public struct MODE_INFO { [FieldOffset(0)] public int t; }

    [StructLayout(LayoutKind.Sequential)]
    public struct HEADER { public int type; public int size; public LUID adapter; public uint id; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SOURCE_NAME { public HEADER h; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string gdi; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TARGET_NAME
    {
        public HEADER h; public uint flags, tech; public ushort mfg, product; public uint connector;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string friendly;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string path;
    }
    [StructLayout(LayoutKind.Sequential)] public struct DPI_GET { public HEADER h; public int min, cur, max; }
    [StructLayout(LayoutKind.Sequential)] public struct DPI_SET { public HEADER h; public int rel; }

    public static class W
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool EnumDisplaySettings(string dev, int mode, ref DEVMODE dm);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool EnumDisplaySettingsEx(string dev, int mode, ref DEVMODE dm, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int ChangeDisplaySettingsEx(string dev, ref DEVMODE dm, IntPtr h, uint f, IntPtr p);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int ChangeDisplaySettingsEx(string dev, IntPtr dm, IntPtr h, uint f, IntPtr p);
        [DllImport("user32.dll")] public static extern int GetDisplayConfigBufferSizes(uint f, out uint np, out uint nm);
        [DllImport("user32.dll")] public static extern int QueryDisplayConfig(uint f, ref uint np, [Out] PATH_INFO[] p, ref uint nm, [Out] MODE_INFO[] m, IntPtr t);
        [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref SOURCE_NAME r);
        [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref TARGET_NAME r);
        [DllImport("user32.dll")] public static extern int SetDisplayConfig(uint np, [In] PATH_INFO[] p, uint nm, [In] MODE_INFO[] m, uint flags);
        [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref DPI_GET r);
        [DllImport("user32.dll")] public static extern int DisplayConfigSetDeviceInfo(ref DPI_SET r);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] static extern int SetWindowTheme(IntPtr h, string app, string id);
        // barra de rolagem escura (tema escuro do Windows)
        public static void Dark(IntPtr h) { try { SetWindowTheme(h, "DarkMode_Explorer", null); } catch { } }
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        // Per-Monitor (v1): pixels reais em cada monitor e sem o WinForms reescalar fontes/controles ao trocar de escala
        public static void DpiAware() { try { if (SetProcessDpiAwarenessContext((IntPtr)(-3))) return; } catch { } SetProcessDPIAware(); }

        public static readonly int[] Scales = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };

        public static DEVMODE NewDM() { var d = new DEVMODE(); d.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)); return d; }

        // Find adapter/source for a GDI name (\\.\DISPLAY1)
        public static bool FindSource(string gdi, out LUID ad, out uint id)
        {
            ad = new LUID(); id = 0;
            uint np, nm;
            if (GetDisplayConfigBufferSizes(2, out np, out nm) != 0) return false;
            var p = new PATH_INFO[np]; var m = new MODE_INFO[nm];
            if (QueryDisplayConfig(2, ref np, p, ref nm, m, IntPtr.Zero) != 0) return false;
            for (int i = 0; i < np; i++)
            {
                var s = new SOURCE_NAME();
                s.h.type = 1; s.h.size = Marshal.SizeOf(typeof(SOURCE_NAME)); s.h.adapter = p[i].srcAdapter; s.h.id = p[i].srcId;
                if (DisplayConfigGetDeviceInfo(ref s) == 0 && s.gdi == gdi) { ad = p[i].srcAdapter; id = p[i].srcId; return true; }
            }
            return false;
        }

        // returns available scale list and current scale
        public static List<int> GetScales(string gdi, out int current)
        {
            current = 100; var list = new List<int>();
            LUID ad; uint id;
            if (!FindSource(gdi, out ad, out id)) { list.Add(100); return list; }
            var g = new DPI_GET(); g.h.type = -3; g.h.size = Marshal.SizeOf(typeof(DPI_GET)); g.h.adapter = ad; g.h.id = id;
            if (DisplayConfigGetDeviceInfo(ref g) != 0) { list.Add(100); return list; }
            int rec = Math.Abs(g.min);
            for (int r = g.min; r <= g.max; r++) { int ix = rec + r; if (ix >= 0 && ix < Scales.Length) list.Add(Scales[ix]); }
            int ci = rec + g.cur; if (ci >= 0 && ci < Scales.Length) current = Scales[ci];
            return list;
        }

        public static bool SetScale(string gdi, int scale)
        {
            LUID ad; uint id;
            if (!FindSource(gdi, out ad, out id)) return false;
            var g = new DPI_GET(); g.h.type = -3; g.h.size = Marshal.SizeOf(typeof(DPI_GET)); g.h.adapter = ad; g.h.id = id;
            if (DisplayConfigGetDeviceInfo(ref g) != 0) return false;
            int target = Array.IndexOf(Scales, scale); if (target < 0) return false;
            var s = new DPI_SET(); s.h.type = -4; s.h.size = Marshal.SizeOf(typeof(DPI_SET)); s.h.adapter = ad; s.h.id = id;
            s.rel = target - Math.Abs(g.min);
            return DisplayConfigSetDeviceInfo(ref s) == 0;
        }
    }

    // ===================== MODEL =====================
    public class Mon
    {
        public string Id, Gdi, Friendly; public int Num;   // Id = monitor físico; Gdi = \\.\DISPLAYn atual (muda)
        public bool Enabled, Primary;
        public int W, H, X, Y, Scale, Hz;
        public Dictionary<long, List<int>> Rates = new Dictionary<long, List<int>>();
        public static long K(int w, int h) { return ((long)w << 20) | (uint)h; }
        public List<int> RatesFor(int w, int h) { List<int> l; return Rates.TryGetValue(K(w, h), out l) ? l : new List<int>(); }
        public List<Size> Modes = new List<Size>();
        public List<int> ScaleOptions = new List<int>();
        public Mon Clone() { return (Mon)MemberwiseClone(); }
        public string Short { get { return string.IsNullOrEmpty(Friendly) ? "Monitor " + Num : Friendly; } }
        public static Dictionary<string, int[]> SavedRes = new Dictionary<string, int[]>();
        static string ResFile { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorHub", "res.txt"); } }
        public static void LoadRes() { try { foreach (var l in System.IO.File.ReadAllLines(ResFile)) { var p = l.Split('='); var v = p[1].Split(','); SavedRes[p[0]] = new[] { int.Parse(v[0]), int.Parse(v[1]), int.Parse(v[2]) }; } } catch { } }
        public static void SaveRes(IEnumerable<Mon> ms)
        {
            foreach (var m in ms) if (m.Enabled) SavedRes[m.Id] = new[] { m.W, m.H, m.Hz };
            try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ResFile)); System.IO.File.WriteAllLines(ResFile, SavedRes.Select(k => k.Key + "=" + k.Value[0] + "," + k.Value[1] + "," + k.Value[2]).ToArray()); } catch { }
        }
    }

    public static class Display
    {
        const uint QDC_ALL = 1, QDC_ACTIVE = 2;

        // um monitor físico = um "alvo" (porta + aparelho). O ID vem do caminho do monitor no Windows,
        // que não muda quando ele é ligado/desligado (ao contrário de \\.\DISPLAYn, que o driver troca).
        class Tgt { public LUID Ad; public uint Id; public string Key, Friendly; }

        static bool Query(uint flags, out PATH_INFO[] p, out MODE_INFO[] m)
        {
            p = null; m = null; uint np, nm;
            if (W.GetDisplayConfigBufferSizes(flags, out np, out nm) != 0) return false;
            p = new PATH_INFO[np]; m = new MODE_INFO[nm];
            if (W.QueryDisplayConfig(flags, ref np, p, ref nm, m, IntPtr.Zero) != 0) return false;
            Array.Resize(ref p, (int)np); Array.Resize(ref m, (int)nm); return true;
        }
        static string TKey(LUID a, uint id) { return a.Low + ":" + a.High + ":" + id; }
        static Tgt TargetInfo(LUID a, uint id)
        {
            var t = new Tgt { Ad = a, Id = id, Key = TKey(a, id), Friendly = "" };
            var n = new TARGET_NAME(); n.h.type = 2; n.h.size = Marshal.SizeOf(typeof(TARGET_NAME)); n.h.adapter = a; n.h.id = id;
            if (W.DisplayConfigGetDeviceInfo(ref n) == 0)
            {
                if (!string.IsNullOrEmpty(n.path)) t.Key = n.path;
                t.Friendly = n.friendly ?? "";
            }
            return t;
        }
        static string SourceGdi(LUID a, uint id)
        {
            var s = new SOURCE_NAME(); s.h.type = 1; s.h.size = Marshal.SizeOf(typeof(SOURCE_NAME)); s.h.adapter = a; s.h.id = id;
            return W.DisplayConfigGetDeviceInfo(ref s) == 0 ? s.gdi : null;
        }

        public static List<Mon> Read()
        {
            var res = new List<Mon>();
            PATH_INFO[] all, act; MODE_INFO[] am, mm;
            if (!Query(QDC_ALL, out all, out am) || !Query(QDC_ACTIVE, out act, out mm)) return res;

            // monitores conectados (cabo ligado e aparelho respondendo)
            var seen = new HashSet<string>(); int num = 0;
            foreach (var p in all)
            {
                if (p.avail == 0) continue;
                string tk = TKey(p.tgtAdapter, p.tgtId); if (!seen.Add(tk)) continue;
                var t = TargetInfo(p.tgtAdapter, p.tgtId); num++;
                var m = new Mon { Id = t.Key, Friendly = t.Friendly, Num = num };
                var ap = act.Where(x => TKey(x.tgtAdapter, x.tgtId) == tk).ToList();
                if (ap.Count > 0) { m.Enabled = true; m.Gdi = SourceGdi(ap[0].srcAdapter, ap[0].srcId); }
                if (m.Gdi == null) m.Enabled = false;
                Fill(m);
                res.Add(m);
            }
            return res.OrderBy(m => m.Enabled ? 0 : 1).ThenBy(m => m.X).ToList();
        }

        static void Fill(Mon m)
        {

            if (m.Enabled)
            {
                var dm = W.NewDM();
                if (W.EnumDisplaySettings(m.Gdi, -1, ref dm)) { m.W = dm.dmPelsWidth; m.H = dm.dmPelsHeight; m.X = dm.dmPositionX; m.Y = dm.dmPositionY; m.Hz = dm.dmDisplayFrequency; }
                m.Primary = m.X == 0 && m.Y == 0;
                var set = new HashSet<long>();
                for (int k = 0; ; k++)
                {
                    var d2 = W.NewDM();
                    if (!W.EnumDisplaySettingsEx(m.Gdi, k, ref d2, 0x2)) break;   // EDS_RAWMODE = "Listar todos os modos"
                    if (d2.dmPelsWidth < 200 || Math.Max(d2.dmPelsWidth, d2.dmPelsHeight) > 3840 || Math.Min(d2.dmPelsWidth, d2.dmPelsHeight) > 2160) continue;
                    long key = Mon.K(d2.dmPelsWidth, d2.dmPelsHeight);
                    if (set.Add(key)) { m.Modes.Add(new Size(d2.dmPelsWidth, d2.dmPelsHeight)); m.Rates[key] = new List<int>(); }
                    if (d2.dmDisplayFrequency > 1 && !m.Rates[key].Contains(d2.dmDisplayFrequency)) m.Rates[key].Add(d2.dmDisplayFrequency);
                }
                foreach (var l in m.Rates.Values) l.Sort((a, b) => b.CompareTo(a));
                m.Modes = m.Modes.OrderByDescending(s => s.Width * s.Height).ToList();
                int cur; m.ScaleOptions = W.GetScales(m.Gdi, out cur); m.Scale = cur;
            }
            else
            {
                // desligado: usa a última resolução escolhida para ESTE monitor
                int[] sv;
                if (Mon.SavedRes.TryGetValue(m.Id, out sv)) { m.W = sv[0]; m.H = sv[1]; m.Hz = sv[2]; }
                m.ScaleOptions = new List<int> { 100 }; m.Scale = 100;
            }
            if (m.W == 0) { m.W = 1920; m.H = 1080; }
        }

        // liga/desliga monitores escolhendo exatamente qual porta/aparelho (SetDisplayConfig)
        static string Topology(HashSet<string> wantOn)
        {
            PATH_INFO[] all, act; MODE_INFO[] am, mm;
            if (!Query(QDC_ALL, out all, out am) || !Query(QDC_ACTIVE, out act, out mm)) return "QueryDisplayConfig";
            Func<LUID, uint, string> key = (a, id) => TargetInfo(a, id).Key;
            var curOn = new HashSet<string>(act.Select(p => key(p.tgtAdapter, p.tgtId)));
            if (curOn.SetEquals(wantOn)) return null;

            var paths = act.Where(p => wantOn.Contains(key(p.tgtAdapter, p.tgtId))).ToList();
            // recria a lista de modos só com os usados pelos caminhos mantidos
            var modes = new List<MODE_INFO>(); var map = new Dictionary<uint, uint>();
            Func<uint, uint> remap = i => { if (i == 0xFFFFFFFF || i >= mm.Length) return 0xFFFFFFFF; uint n; if (!map.TryGetValue(i, out n)) { n = (uint)modes.Count; modes.Add(mm[i]); map[i] = n; } return n; };
            for (int i = 0; i < paths.Count; i++) { var p = paths[i]; p.srcMode = remap(p.srcMode); p.tgtMode = remap(p.tgtMode); paths[i] = p; }

            foreach (var k in wantOn.Where(k => !curOn.Contains(k)))
            {
                var used = new HashSet<string>(paths.Select(p => p.srcAdapter.Low + ":" + p.srcAdapter.High + ":" + p.srcId));
                var cand = all.Where(p => p.avail != 0 && key(p.tgtAdapter, p.tgtId) == k && !used.Contains(p.srcAdapter.Low + ":" + p.srcAdapter.High + ":" + p.srcId)).ToList();
                if (cand.Count == 0) continue;
                var np = cand[0]; np.flags = 1; np.srcFlags = 0; np.tgtFlags = 0; np.srcMode = 0xFFFFFFFF; np.tgtMode = 0xFFFFFFFF;
                paths.Add(np);
            }
            if (paths.Count == 0) return "0";
            const uint APPLY = 0x80, SUPPLIED = 0x20, ALLOW = 0x400, SAVE = 0x200;
            int r = W.SetDisplayConfig((uint)paths.Count, paths.ToArray(), (uint)modes.Count, modes.Count > 0 ? modes.ToArray() : null, APPLY | SUPPLIED | ALLOW | SAVE);
            if (r != 0) return r.ToString();
            System.Threading.Thread.Sleep(1200);
            return null;
        }

        // torna o monitor (pelo ID) o principal, mantendo os demais onde estão em relação a ele
        static void SwitchPrimary(string id)
        {
            var cur = Read().Where(x => x.Enabled).ToList();
            var np = cur.FirstOrDefault(x => x.Id == id);
            if (np == null || np.Primary) return;
            int ox = np.X, oy = np.Y;
            foreach (var c in cur.OrderBy(x => x == np ? 0 : 1))
            {
                var dm = W.NewDM(); dm.dmFields = 0x20; dm.dmPositionX = c.X - ox; dm.dmPositionY = c.Y - oy;
                W.ChangeDisplaySettingsEx(c.Gdi, ref dm, IntPtr.Zero, 0x1 | 0x10000000 | (c == np ? 0x10u : 0u), IntPtr.Zero);
            }
            W.ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(1000);
        }

        public static string Apply(List<Mon> mons)
        {
            var on = mons.Where(m => m.Enabled).ToList();
            if (on.Count == 0) return L.T("Pelo menos um monitor precisa estar ativo.");
            var pri = on.FirstOrDefault(m => m.Primary) ?? on[0];
            foreach (var m in mons) m.Primary = (m == pri);

            // 1) liga/desliga na ordem segura: liga os novos -> troca o principal -> só então desliga os que saem
            //    (o Windows recusa desligar o monitor que ainda é o principal: erro 87)
            var want = new HashSet<string>(on.Select(m => m.Id));
            var curOn = new HashSet<string>(Read().Where(x => x.Enabled).Select(x => x.Id));
            var err = Topology(new HashSet<string>(curOn.Union(want)));
            if (err == null) SwitchPrimary(pri.Id);
            if (err == null) err = Topology(want);
            if (err != null) return L.F("Falha ao configurar {0} (código {1}).", "Windows", err);

            // 2) descobre o \\.\DISPLAYn atual de cada monitor ligado
            var now = Read();
            foreach (var m in on)
            {
                var c = now.FirstOrDefault(x => x.Id == m.Id && x.Enabled);
                if (c == null) return L.F("Falha ao configurar {0} (código {1}).", m.Short, "?");
                m.Gdi = c.Gdi;
                if (c.Modes.Count > 0 && !c.Modes.Any(z => z.Width == m.W && z.Height == m.H)) { m.W = c.W; m.H = c.H; m.Hz = c.Hz; }
            }

            // 3) resolução, Hz e posição (o layout escolhido; o principal fica em 0,0)
            int ox = pri.X, oy = pri.Y;
            foreach (var m in on) { m.X -= ox; m.Y -= oy; }
            const uint UPDATE = 0x1, NORESET = 0x10000000, SETPRI = 0x10;
            const int F_POS = 0x20, F_W = 0x80000, F_H = 0x100000;
            foreach (var m in on.OrderBy(mm => mm.Primary ? 0 : 1))
            {
                var dm = W.NewDM();
                dm.dmFields = F_POS | F_W | F_H;
                if (m.Hz > 1) { dm.dmDisplayFrequency = m.Hz; dm.dmFields |= 0x400000; }
                dm.dmPelsWidth = m.W; dm.dmPelsHeight = m.H; dm.dmPositionX = m.X; dm.dmPositionY = m.Y;
                int r = W.ChangeDisplaySettingsEx(m.Gdi, ref dm, IntPtr.Zero, UPDATE | NORESET | (m.Primary ? SETPRI : 0), IntPtr.Zero);
                if (r != 0) return L.F("Falha ao configurar {0} (código {1}).", m.Short, r);
            }
            W.ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
            System.Threading.Thread.Sleep(1200);
            // 4) escala do Windows
            foreach (var m in on) W.SetScale(m.Gdi, m.Scale);
            return null;
        }
    }

    // ===================== UI =====================
    public class FlatBtn : Button
    {
        public Color Base = Color.FromArgb(24, 24, 30), Hover = Color.FromArgb(42, 42, 54), Border = Color.FromArgb(48, 48, 60);
        public int Radius = 10; public bool Active, Shrink = true; protected bool over, down;
        public FlatBtn(string t)
        {
            Text = t; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; BackColor = Color.Transparent;
            ForeColor = Color.Gainsboro; Font = UI.F("Segoe UI", 9.5f); Cursor = Cursors.Hand; Height = 34;
        }
        public void SetActive(bool a) { Active = a; ForeColor = a ? Color.White : Color.Gainsboro; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { over = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Color.Black);
            Color acc = Color.FromArgb(200, 30, 50);
            Color c = Active ? (over ? Color.FromArgb(225, 45, 65) : acc) : (over ? Hover : Base);
            if (down) c = ControlPaint.Dark(c, 0.1f);
            if (!Enabled) c = Active ? Color.FromArgb(90, 30, 40) : Color.FromArgb(20, 20, 24);
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var gp = MainForm.Round(r, Radius * 2))
            {
                using (var br = new SolidBrush(c)) g.FillPath(br, gp);
                if (!Active && Radius > 0) using (var p = new Pen(Border)) g.DrawPath(p, gp);
            }
            var fc = Enabled ? ForeColor : Color.FromArgb(150, 150, 160);
            // ✕ e – desenhados com linhas, centralizados de verdade (a fonte deixa o glifo torto)
            if (Text == "✎")   // lápis desenhado com linhas, centralizado
            {
                float c0 = Width / 2f, c1 = Height / 2f, k = Math.Min(Width, Height) * 0.2f;
                using (var p = new Pen(fc, 1.6f)) { p.StartCap = p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                    var a = new PointF(c0 - k, c1 + k); var b = new PointF(c0 + k, c1 - k); float t = k * 0.35f;
                    g.DrawPolygon(p, new[] { a, new PointF(a.X + t * 1.4f, a.Y), new PointF(b.X + t * 0.7f, b.Y + t * 0.7f), new PointF(b.X - t * 0.7f, b.Y - t * 0.7f), new PointF(a.X, a.Y - t * 1.4f) }); }
                return;
            }
            if (Text == "✕" || Text == "–")
            {
                float c0 = Width / 2f, c1 = Height / 2f, k = Math.Min(Width, Height) * 0.17f;
                using (var p = new Pen(fc, 1.6f)) { p.StartCap = p.EndCap = LineCap.Round;
                    if (Text == "✕") { g.DrawLine(p, c0 - k, c1 - k, c0 + k, c1 + k); g.DrawLine(p, c0 - k, c1 + k, c0 + k, c1 - k); }
                    else g.DrawLine(p, c0 - k, c1, c0 + k, c1); }
                return;
            }
            var font = Font;   // Shrink: diminui a fonte até o texto caber inteiro (sem "...")
            if (Shrink) { float fs = Font.Size; while (fs > 9f && UI.Measure(Text, font).Width > Width - 14) { fs -= 0.5f; font = new Font(Font.FontFamily, fs, Font.Style, Font.Unit); } }
            UI.Text(g, Text, font, new Rectangle(0, 0, Width, Height), fc, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    public class DarkDrop : Control
    {
        public List<object> Items = new List<object>(); int idx = -1; bool over;
        public event EventHandler SelectedIndexChanged;
        public DarkDrop() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Font = UI.F("Segoe UI", 10f); Cursor = Cursors.Hand; ForeColor = Color.Gainsboro; }
        public object SelectedItem
        {
            get { return idx >= 0 && idx < Items.Count ? Items[idx] : null; }
            set { int i = Items.IndexOf(value); if (i != idx) { idx = i; Invalidate(); if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty); } }
        }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnMouseEnter(EventArgs e) { over = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { over = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Parent.BackColor);
            using (var gp = MainForm.Round(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 18))
            {
                using (var b = new SolidBrush(over && Enabled ? Color.FromArgb(32, 32, 42) : Color.FromArgb(22, 22, 30))) g.FillPath(b, gp);
                using (var p = new Pen(over && Enabled ? Color.FromArgb(200, 30, 50) : Color.FromArgb(48, 48, 60))) g.DrawPath(p, gp);
            }
            var fc = Enabled ? ForeColor : Color.FromArgb(90, 90, 100);
            UI.Text(g, SelectedItem == null ? "—" : SelectedItem.ToString(), Font, new Rectangle(14, 0, Width - 40, Height), fc, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            float cx = Width - 20, cy = Height / 2f;
            using (var p = new Pen(Enabled ? Color.FromArgb(200, 30, 50) : fc, 2f)) { p.StartCap = p.EndCap = LineCap.Round; g.DrawLines(p, new[] { new PointF(cx - 5, cy - 2.5f), new PointF(cx, cy + 2.5f), new PointF(cx + 5, cy - 2.5f) }); }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (!Enabled || Items.Count == 0) return;
            var lb = new ListBox { BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(22, 22, 30), ForeColor = Color.Gainsboro, Font = Font, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 28, IntegralHeight = false };
            foreach (var it in Items) lb.Items.Add(it);
            lb.Width = Width - 8; lb.Height = Math.Min(10, Items.Count) * 28;
            int hot = idx;
            lb.DrawItem += delegate(object o, DrawItemEventArgs de)
            {
                if (de.Index < 0) return; bool h = de.Index == hot;
                using (var b = new SolidBrush(Color.FromArgb(22, 22, 30))) de.Graphics.FillRectangle(b, de.Bounds);
                if (h) { de.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var gp = MainForm.Round(new RectangleF(de.Bounds.X + 3, de.Bounds.Y + 2, de.Bounds.Width - 6, de.Bounds.Height - 4), 12)) using (var b = new SolidBrush(Color.FromArgb(200, 30, 50))) de.Graphics.FillPath(b, gp); }
                UI.Text(de.Graphics, "  " + lb.Items[de.Index], Font, de.Bounds, h ? Color.White : Color.Gainsboro, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            lb.MouseMove += delegate(object o, MouseEventArgs me) { int i = lb.IndexFromPoint(me.Location); if (i != hot) { hot = i; lb.Invalidate(); } };
            var host = new ToolStripControlHost(lb) { Padding = Padding.Empty, Margin = Padding.Empty, AutoSize = false, Size = lb.Size };
            var dd = new ToolStripDropDown { Padding = new Padding(4), BackColor = Color.FromArgb(22, 22, 30), DropShadowEnabled = true };
            dd.Renderer = new ToolStripProfessionalRenderer(new DarkColors());
            dd.Items.Add(host);
            lb.MouseClick += delegate(object o, MouseEventArgs me) { int i = lb.IndexFromPoint(me.Location); if (i >= 0) { dd.Close(); SelectedItem = Items[i]; } };
            dd.Show(this, new Point(0, Height + 4));
        }
        class DarkColors : ProfessionalColorTable { public override Color ToolStripBorder { get { return Color.FromArgb(48, 48, 60); } } public override Color ToolStripDropDownBackground { get { return Color.FromArgb(22, 22, 30); } } }
    }

    class DarkMenu : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(18, 18, 24); } }
        public override Color MenuBorder { get { return Color.FromArgb(48, 48, 60); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(200, 30, 50); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(200, 30, 50); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(200, 30, 50); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(200, 30, 50); } }
    }

    // "Iniciar com o Windows": entrada em HKCU\...\Run, abrindo direto na bandeja (--tray)
    public static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", Name = "MonitorHub";
        static string FileP { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorHub", "startup.txt"); } }
        public static bool On
        {
            get { try { return System.IO.File.ReadAllText(FileP).Trim() != "0"; } catch { return true; } }   // ligado por padrão
            set { try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FileP)); System.IO.File.WriteAllText(FileP, value ? "1" : "0"); } catch { } Sync(); }
        }
        // grava/remove a entrada (e corrige o caminho se o exe foi movido)
        public static void Sync()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (On) k.SetValue(Name, "\"" + Application.ExecutablePath + "\" --tray"); else k.DeleteValue(Name, false);
                }
            }
            catch { }
        }
    }

    // card de configuração salva: miniatura do layout dos monitores + nome embaixo
    public class PresetCard : FlatBtn
    {
        Presets.Preset p;
        public PresetCard(Presets.Preset pr) : base(pr.Name) { p = pr; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent != null ? Parent.BackColor : Color.Black);
            var card = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var gp = MainForm.Round(card, 20))
            {
                using (var b = new SolidBrush(down ? Color.FromArgb(20, 20, 26) : over ? Color.FromArgb(34, 34, 44) : Color.FromArgb(24, 24, 30))) g.FillPath(b, gp);
                using (var pn = new Pen(over ? Color.FromArgb(200, 30, 50) : Color.FromArgb(48, 48, 60))) g.DrawPath(pn, gp);
            }
            // miniatura
            var area = new RectangleF(10, 8, Width - 20, Height - 40);
            var on = p.Mons.Where(m => m.Enabled).ToList();
            if (on.Count > 0)
            {
                float l = on.Min(m => m.X), t = on.Min(m => m.Y), r = on.Max(m => m.X + m.W), bt = on.Max(m => m.Y + m.H);
                float sc = Math.Min(area.Width / Math.Max(1, r - l), area.Height / Math.Max(1, bt - t));
                if (on.Count == 1)   // um só monitor: tamanho sugere a resolução (720p pequeno ... 4K quase cheio)
                {
                    int hh = Math.Min(on[0].W, on[0].H);
                    sc *= hh < 720 ? 0.45f : hh <= 1080 ? 0.6f : hh <= 1440 ? 0.75f : 0.92f;
                }
                float ox = area.X + (area.Width - (r - l) * sc) / 2, oy = area.Y + (area.Height - (bt - t) * sc) / 2;
                foreach (var m in on)
                {
                    var mr = new RectangleF(ox + (m.X - l) * sc + 1.5f, oy + (m.Y - t) * sc + 1.5f, m.W * sc - 3, m.H * sc - 3);
                    if (mr.Width < 3 || mr.Height < 3) continue;
                    using (var mp = MainForm.Round(mr, 6))
                    {
                        using (var b = new SolidBrush(m.Primary ? Color.FromArgb(38, 70, 125) : Color.FromArgb(28, 46, 80))) g.FillPath(b, mp);
                        using (var pn = new Pen(m.Primary ? Color.FromArgb(230, 50, 70) : Color.FromArgb(80, 100, 140), 1.2f)) g.DrawPath(pn, mp);
                    }
                }
            }
            // nome (diminui a fonte até caber inteiro)
            var font = UI.F("Segoe UI Semibold", 9f); float fs = 9f;
            while (fs > 6.5f && UI.Measure(Text, font).Width > Width - 14) { fs -= 0.5f; font = UI.F("Segoe UI Semibold", fs); }
            UI.Text(g, Text, font, new Rectangle(0, Height - 30, Width, 26), Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    // fontes em pixels fixos (equivalente a 100%): o visual não muda com a escala do Windows
    public static class UI
    {
        public const float Z = 1.4f;   // tamanho geral dos textos
        public static Font F(string name, float pt, FontStyle st = FontStyle.Regular) { return new Font(name, pt * Z * 96f / 72f, st, GraphicsUnit.Pixel); }

        // Texto desenhado com GDI+ (respeita os pixels fixos). O TextRenderer (GDI) reescala a fonte pela
        // escala do Windows em telas com 125%+ e cortava os textos dos botões.
        static readonly Graphics mg = Graphics.FromImage(new Bitmap(1, 1));
        public static Size Measure(string s, Font f) { var z = mg.MeasureString(s ?? "", f, PointF.Empty, StringFormat.GenericTypographic); return new Size((int)Math.Ceiling(z.Width) + 4, (int)Math.Ceiling(z.Height)); }
        public static Size Measure(Graphics g, string s, Font f) { return Measure(s, f); }
        public static void Text(Graphics g, string s, Font f, Rectangle r, Color c, TextFormatFlags fl = TextFormatFlags.Left)
        {
            var sf = new StringFormat(StringFormat.GenericTypographic);
            sf.Alignment = (fl & TextFormatFlags.HorizontalCenter) != 0 ? StringAlignment.Center : (fl & TextFormatFlags.Right) != 0 ? StringAlignment.Far : StringAlignment.Near;
            sf.LineAlignment = (fl & TextFormatFlags.VerticalCenter) != 0 ? StringAlignment.Center : (fl & TextFormatFlags.Bottom) != 0 ? StringAlignment.Far : StringAlignment.Near;
            if ((fl & TextFormatFlags.WordBreak) == 0) sf.FormatFlags |= StringFormatFlags.NoWrap;
            sf.Trimming = (fl & TextFormatFlags.EndEllipsis) != 0 ? StringTrimming.EllipsisCharacter : StringTrimming.None;
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var b = new SolidBrush(c)) g.DrawString(s ?? "", f, b, r, sf);
        }
        public static void Text(Graphics g, string s, Font f, Point p, Color c)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var b = new SolidBrush(c)) g.DrawString(s ?? "", f, b, p, StringFormat.GenericTypographic);
        }
    }

    public class RoundPanel : Panel
    {
        public RoundPanel() { DoubleBuffered = true; BackColor = Color.Black; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var gp = MainForm.Round(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 28))
            { using (var b = new SolidBrush(Color.FromArgb(16, 16, 22))) e.Graphics.FillPath(b, gp); using (var p = new Pen(Color.FromArgb(40, 40, 52))) e.Graphics.DrawPath(p, gp); }
        }
    }

    // configurações salvas (%AppData%\MonitorHub\presets.txt)
    public static class Presets
    {
        public class Entry { public string Gdi; public bool Enabled, Primary; public int X, Y, W, H, Hz, Scale; }
        public class Preset { public string Name; public List<Entry> Mons = new List<Entry>(); }
        static string FileP { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorHub", "presets.txt"); } }

        public static List<Preset> Load()
        {
            var res = new List<Preset>();
            try
            {
                foreach (var l in System.IO.File.ReadAllLines(FileP))
                {
                    var t = l.Split('\t'); if (t.Length < 2) continue;
                    var p = new Preset { Name = t[0] };
                    foreach (var e in t[1].Split(';'))
                    {
                        var v = e.Split('|'); if (v.Length < 9) continue;
                        p.Mons.Add(new Entry { Gdi = v[0], Enabled = v[1] == "1", Primary = v[2] == "1", X = int.Parse(v[3]), Y = int.Parse(v[4]), W = int.Parse(v[5]), H = int.Parse(v[6]), Hz = int.Parse(v[7]), Scale = int.Parse(v[8]) });
                    }
                    res.Add(p);
                }
            }
            catch { }
            return res;
        }
        static void Write(List<Preset> all)
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FileP));
                System.IO.File.WriteAllLines(FileP, all.Select(p => p.Name + "\t" + string.Join(";", p.Mons.Select(e =>
                    string.Join("|", new[] { e.Gdi, e.Enabled ? "1" : "0", e.Primary ? "1" : "0", e.X.ToString(), e.Y.ToString(), e.W.ToString(), e.H.ToString(), e.Hz.ToString(), e.Scale.ToString() })))).ToArray());
            }
            catch { }
        }
        public static void Save(string name, List<Mon> mons)
        {
            name = name.Replace("\t", " ");
            var all = Load(); all.RemoveAll(p => p.Name == name);
            var pr = mons.FirstOrDefault(m => m.Enabled && m.Primary) ?? mons.First(m => m.Enabled);
            var np = new Preset { Name = name };
            foreach (var m in mons)
                np.Mons.Add(new Entry { Gdi = m.Id, Enabled = m.Enabled, Primary = m == pr, X = m.X - pr.X, Y = m.Y - pr.Y, W = m.W, H = m.H, Hz = m.Hz, Scale = m.Scale });
            all.Add(np); Write(all);
        }
        public static void Order(List<string> names)
        {
            var all = Load();
            Write(all.OrderBy(p => { int i = names.IndexOf(p.Name); return i < 0 ? int.MaxValue : i; }).ToList());
        }
        public static void Rename(string from, string to)
        {
            to = to.Replace("\t", " ");
            var all = Load(); all.RemoveAll(p => p.Name == to);
            foreach (var p in all) if (p.Name == from) p.Name = to;
            Write(all);
        }
        public static void Delete(string name) { var all = Load(); all.RemoveAll(p => p.Name == name); Write(all); }
    }

    public class MainForm : Form
    {
        List<Mon> mons; int sel;
        Panel canvas; FlowLayoutPanel scaleRow; DarkDrop resBox, hzBox; FlatBtn priBtn, applyBtn, disBtn, crtBtn; Label info;
        Dictionary<Mon, RectangleF> hit = new Dictionary<Mon, RectangleF>();
        static readonly Color BG = Color.Black, FG = Color.Gainsboro, ACC = Color.FromArgb(230, 50, 70);

        public MainForm()
        {
            AutoScaleMode = AutoScaleMode.None; Text = "Monitor Hub"; BackColor = BG; ForeColor = FG; Font = UI.F("Segoe UI", 9.5f);
            ClientSize = BaseSize; FormBorderStyle = FormBorderStyle.None; DoubleBuffered = true;
            StartPosition = FormStartPosition.CenterScreen; Icon = Program.AppIcon; ShowInTaskbar = true;
            try { int v = 2; DwmSetWindowAttribute(Handle, 33, ref v, 4); } catch { }

            var close = new FlatBtn("✕") { Left = 860 - 46, Top = 0, Width = 46, Height = 32 }; close.Radius = 0; close.Base = BG; close.Hover = Color.FromArgb(200, 40, 50);
            close.Click += delegate { Hide(); }; Controls.Add(close);
            var min = new FlatBtn("–") { Left = 860 - 92, Top = 0, Width = 46, Height = 32 }; min.Radius = 0; min.Base = BG;
            min.Click += delegate { WindowState = FormWindowState.Minimized; }; Controls.Add(min);
            MouseDown += Drag;

            int y = 44;
            canvas = new DoubleBufferedPanel { Left = 20, Top = y, Width = 820, Height = 334, BackColor = BG };
            canvas.Paint += DrawMonitors; canvas.MouseDown += CanvasDown; canvas.MouseMove += CanvasMove; canvas.MouseUp += CanvasUp; Controls.Add(canvas); y += 344;

            priBtn = new FlatBtn(L.T("Definir como principal")) { Left = 20, Top = y - 4, Width = 340, Height = 42, Radius = 21 };
            priBtn.Font = UI.F("Segoe UI Semibold", 10.5f); priBtn.SetActive(true);
            priBtn.Click += delegate { var sm = mons[sel]; if (!sm.Enabled) PlaceRight(sm); foreach (var m in mons) m.Primary = false; mons[sel].Primary = true; mons[sel].Enabled = true; Refresh2(); };
            Controls.Add(priBtn);
            disBtn = new FlatBtn("⏻  " + L.T("Desabilitar")) { Left = priBtn.Right + 12, Top = priBtn.Top + 3, Width = 200, Height = 36, Radius = 18 };
            disBtn.Click += delegate
            {
                var m = mons[sel];
                if (m.Primary) { info.Text = L.T("O monitor principal não pode ser desativado."); return; }
                m.Enabled = false; info.Text = L.F("{0} será desativado. Clique em Aplicar.", m.Short); Refresh2();
            };
            Controls.Add(disBtn); y += 56;

            Controls.Add(Title(L.T("RESOLUÇÃO"), y)); y += 32;
            resBox = new DarkDrop { Left = 20, Top = y - 2, Width = 280, Height = 36 };
            resBox.SelectedIndexChanged += delegate { if (resBox.SelectedItem != null) { var s = ((ResItem)resBox.SelectedItem).S; mons[sel].W = s.Width; mons[sel].H = s.Height; FillHz(); canvas.Invalidate(); } };
            Controls.Add(resBox);
            var hzT = Title(L.T("TAXA DE ATUALIZAÇÃO"), y - 32); hzT.Left = 320; Controls.Add(hzT);
            hzBox = new DarkDrop { Left = 320, Top = y - 2, Width = 180, Height = 36 };
            hzBox.SelectedIndexChanged += delegate { if (hzBox.SelectedItem != null) { mons[sel].Hz = ((HzItem)hzBox.SelectedItem).Hz; canvas.Invalidate(); } };
            Controls.Add(hzBox); y += 44;

            int scaleY = y; Controls.Add(Title(L.T("ESCALA DO WINDOWS"), y)); y += 34;
            scaleRow = new FlowLayoutPanel { Left = 20, Top = y, Width = 820, Height = 46 }; Controls.Add(scaleRow); y += 56;

            info = new Label { Left = 20, Top = y + 10, Width = 225, Height = 24, ForeColor = Color.Gray }; Controls.Add(info);
            var cal = new FlatBtn("◐  " + L.T("Calibração de cores")) { Left = 250, Top = y, Width = 175 }; cal.Click += delegate { CalibForm.Open(false); }; Controls.Add(cal);
            crtBtn = new FlatBtn("▣  " + L.T("Calibração CRT")) { Left = 433, Top = y, Width = 150 }; crtBtn.Click += delegate { OpenCrt(); }; Controls.Add(crtBtn);
            var reload = new FlatBtn(L.T("Recarregar")) { Left = 591, Top = y, Width = 110 }; reload.Click += delegate { Load2(); }; Controls.Add(reload);
            applyBtn = new FlatBtn(L.T("Aplicar")) { Left = 710, Top = y, Width = 130, Height = 40 };
            applyBtn.SetActive(true); applyBtn.Font = UI.F("Segoe UI Semibold", 10.5f); applyBtn.Click += DoApply; Controls.Add(applyBtn);
            // linha de baixo: cada botão do tamanho do seu texto (nada cortado), alinhados à direita
            int rx = 840;
            foreach (var b in new FlatBtn[] { applyBtn, reload, crtBtn, cal })
            {
                b.Width = Math.Max(b.Width, UI.Measure(b.Text, b.Font).Width + 36);
                rx -= b.Width; b.Left = rx; rx -= 8;
            }
            info.Left = 360; info.Top = scaleY; info.Width = 480; info.Height = 22; info.TextAlign = ContentAlignment.MiddleRight;

            // desloca o conteúdo para abrir espaço para a coluna global à esquerda
            foreach (Control c in Controls) c.Left += LW;
            close.Left = BaseSize.Width - 46; min.Left = BaseSize.Width - 92;
            BuildSide();

            Load2();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += delegate { if (IsHandleCreated) BeginInvoke((Action)PlaceNearTray); };
        }
        protected override void WndProc(ref Message m)
        {
            // mudança de escala/DPI: não deixa o Windows esticar a janela; mantém o tamanho fixo e reposiciona
            if (m.Msg == 0x02E0) { m.Result = IntPtr.Zero; ClientSize = BaseSize; if (IsHandleCreated) BeginInvoke((Action)PlaceNearTray); return; }
            base.WndProc(ref m);
        }

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int m, int w, int l);
        void Drag(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Color.FromArgb(50, 50, 60))) e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
        // ---------- coluna global: configurações salvas ----------
        FlowLayoutPanel presetList;
        void BuildSide()
        {
            int x = 16, w = LW - 20;
            var lang = new FlatBtn("🌐  Language · " + L.Codes[L.Cur].ToUpper()) { Left = x, Top = 40, Width = 190, Height = 34, Radius = 15 };
            lang.Font = UI.F("Segoe UI", 8.5f);
            lang.Click += delegate
            {
                var cm = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new DarkMenu()), BackColor = Color.FromArgb(18, 18, 24), ForeColor = Color.Gainsboro, ShowImageMargin = false };
                for (int i = 0; i < L.Names.Length; i++)
                {
                    int ii = i; var it = new ToolStripMenuItem((i == L.Cur ? "●  " : "    ") + L.Names[i]) { ForeColor = Color.Gainsboro };
                    it.Click += delegate { if (ii != L.Cur) { L.Set(ii); Program.Restart(); } };
                    cm.Items.Add(it);
                }
                cm.Show(lang, new Point(0, lang.Height + 4));
            };
            Controls.Add(lang);
            int top = 88;
            var t = Title(L.T("CONFIGURAÇÕES"), top); t.Left = x + 4; Controls.Add(t);
            var save = new FlatBtn("＋  " + L.T("Salvar configuração")) { Left = x, Top = top + 32, Width = w, Height = 38, Radius = 19 };
            save.SetActive(true); save.Font = UI.F("Segoe UI Semibold", 9.5f);
            save.Click += delegate
            {
                var n = Ask(L.T("Nome da configuração"), "", 28); if (string.IsNullOrEmpty(n)) return;
                Presets.Save(n, mons); FillPresets(); info.Text = L.F("Configuração \"{0}\" salva.", n);
            };
            Controls.Add(save);
            var t2 = Title(L.T("SALVAS"), top + 84); t2.Left = x + 4; Controls.Add(t2);
            var box = new RoundPanel { Left = x, Top = top + 112, Width = w, Height = BaseSize.Height - (top + 112) - 16 - 46 };
            var st = new FlatBtn("") { Left = x, Top = BaseSize.Height - 16 - 34, Width = w, Height = 34, Radius = 17 };
            st.Font = UI.F("Segoe UI", 9f);
            Action upd = () => { st.Text = (Startup.On ? "☑  " : "☐  ") + L.T("Iniciar com o Windows"); st.SetActive(false); st.Invalidate(); };
            st.Click += delegate { Startup.On = !Startup.On; upd(); };
            upd(); Controls.Add(st);
            Controls.Add(box);
            presetList = new FlowLayoutPanel { Left = 6, Top = 8, Width = w - 10, Height = box.Height - 16, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.FromArgb(16, 16, 22) };
            box.Controls.Add(presetList);
            presetList.HandleCreated += delegate { W.Dark(presetList.Handle); };
            FillPresets();
        }
        void FillPresets()
        {
            presetList.Controls.Clear();
            var all = Presets.Load();
            presetList.HorizontalScroll.Maximum = 0; presetList.HorizontalScroll.Visible = false;
            bool needV = all.Count * 98 > presetList.Height;
            int rowW = presetList.Width - (needV ? SystemInformation.VerticalScrollBarWidth + 2 : 4);
            if (all.Count == 0) presetList.Controls.Add(new Label { Text = L.T("Nenhuma ainda."), AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(6, 6, 0, 0) });
            foreach (var p in all)
            {
                var pp = p; var row = new Panel { Width = rowW, Height = 92, Margin = new Padding(0, 0, 0, 6), BackColor = Color.FromArgb(16, 16, 22) };
                var b = new PresetCard(p) { Left = 0, Top = 0, Width = row.Width - 38, Height = 90 };
                // arrastar para cima/baixo reordena; clique simples aplica
                bool down = false, moved = false; int y0 = 0;
                b.MouseDown += (o, e) => { if (e.Button == MouseButtons.Left) { down = true; moved = false; y0 = Cursor.Position.Y; } };
                b.MouseMove += (o, e) =>
                {
                    if (!down) return;
                    if (!moved && Math.Abs(Cursor.Position.Y - y0) < 6) return;
                    moved = true; b.Cursor = Cursors.SizeNS;
                    var pt = presetList.PointToClient(Cursor.Position); int idx = presetList.Controls.Count - 1;
                    for (int i = 0; i < presetList.Controls.Count; i++) { var c = presetList.Controls[i]; if (pt.Y < c.Top + c.Height / 2) { idx = i; break; } }
                    if (presetList.Controls.GetChildIndex(row) != idx) presetList.Controls.SetChildIndex(row, idx);
                };
                b.MouseUp += (o, e) =>
                {
                    if (!down) return; down = false; b.Cursor = Cursors.Hand;
                    if (moved) Presets.Order(presetList.Controls.Cast<Control>().Select(c => (string)c.Tag).Where(n => n != null).ToList());
                };
                b.Click += delegate { if (!moved) ApplyPreset(pp); };
                row.Tag = p.Name;
                var del = new FlatBtn("✕") { Left = row.Width - 33, Top = 48, Width = 32, Height = 32, Radius = 16 };
                del.Click += delegate { Presets.Delete(pp.Name); FillPresets(); };
                var edit = new FlatBtn("✎") { Left = row.Width - 33, Top = 10, Width = 32, Height = 32, Radius = 16 };
                edit.Click += delegate
                {
                    var n = Ask(L.T("Nome da configuração"), pp.Name, 28);
                    if (!string.IsNullOrEmpty(n) && n != pp.Name) { Presets.Rename(pp.Name, n); FillPresets(); }
                };
                row.Controls.Add(b); row.Controls.Add(edit); row.Controls.Add(del); presetList.Controls.Add(row);
            }
        }
        void ApplyPreset(Presets.Preset p)
        {
            var list = Display.Read();
            foreach (var e in p.Mons)
            {
                var m = list.FirstOrDefault(x => x.Id == e.Gdi) ?? list.FirstOrDefault(x => x.Gdi == e.Gdi); if (m == null) continue;
                m.Enabled = e.Enabled; m.Primary = e.Primary; m.X = e.X; m.Y = e.Y;
                if (e.Enabled) { m.W = e.W; m.H = e.H; m.Hz = e.Hz; m.Scale = e.Scale; }
            }
            mons = list; sel = Math.Max(0, mons.FindIndex(x => x.Primary)); Refresh2();
            info.Text = L.F("Aplicando \"{0}\"...", p.Name);
            ApplyCurrent();
        }
        string Ask(string title, string init, int max)
        {
            var f = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(340, 120), BackColor = Color.FromArgb(18, 18, 24), ShowInTaskbar = false };
            var lb = new Label { Text = title, Left = 16, Top = 12, AutoSize = true, ForeColor = ACC, Font = UI.F("Segoe UI Semibold", 9f) };
            var tb = new TextBox { Left = 16, Top = 38, Width = 308, Text = init, MaxLength = max, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(28, 28, 36), ForeColor = Color.White, Font = UI.F("Segoe UI", 11f) };
            var ok = new FlatBtn(L.T("Salvar")) { Left = 224, Top = 76, Width = 100 }; ok.SetActive(true);
            var cc = new FlatBtn(L.T("Cancelar")) { Left = 116, Top = 76, Width = 100 };
            ok.Click += delegate { f.DialogResult = DialogResult.OK; }; cc.Click += delegate { f.Close(); };
            f.AcceptButton = ok; f.CancelButton = cc; f.Controls.AddRange(new Control[] { lb, tb, ok, cc });
            return f.ShowDialog(this) == DialogResult.OK ? tb.Text.Trim() : null;
        }

        Label Title(string t, int y)
        {
            // rótulo só com o texto (altura justa, sem fundo próprio) para não cobrir o campo de baixo
            var f = UI.F("Segoe UI Semibold", 10.5f); var sz = UI.Measure(t, f);
            return new Label { Text = t, Left = 20, Top = y, AutoSize = false, Width = sz.Width + 8, Height = sz.Height + 2, BackColor = Color.Transparent, ForeColor = Color.FromArgb(230, 70, 90), Font = f, UseCompatibleTextRendering = true };
        }
        Label Lbl(string t, int x, int y) { return new Label { Text = t, Left = x, Top = y, AutoSize = true }; }

        class HzItem { public int Hz; public override string ToString() { return Hz + " Hz"; } }
        void FillHz()
        {
            var m = mons[sel]; hzBox.Items.Clear();
            var list = m.RatesFor(m.W, m.H);
            if (list.Count > 0 && !list.Contains(m.Hz)) m.Hz = list[0];
            foreach (var h in list) { var it = new HzItem { Hz = h }; hzBox.Items.Add(it); if (h == m.Hz) hzBox.SelectedItem = it; }
            hzBox.Enabled = m.Enabled && list.Count > 0;
        }
        class ResItem { public Size S; public override string ToString() { return S.Width + " × " + S.Height; } }
        class DoubleBufferedPanel : Panel { public DoubleBufferedPanel() { DoubleBuffered = true; } }

        public void Load2() { mons = Display.Read(); sel = Math.Max(0, mons.FindIndex(m => m.Primary)); Refresh2(); info.Text = L.F("{0} monitor(es) detectado(s).", mons.Count); }

        void Refresh2()
        {
            if (mons.Count == 0) return;
            if (sel >= mons.Count) sel = 0;
            var m = mons[sel];
            resBox.Items.Clear();
            foreach (var s in m.Modes) { var it = new ResItem { S = s }; resBox.Items.Add(it); if (s.Width == m.W && s.Height == m.H) resBox.SelectedItem = it; }
            resBox.Enabled = m.Enabled; FillHz();
            disBtn.Enabled = m.Enabled && !m.Primary;
            priBtn.Enabled = !m.Primary; priBtn.Text = m.Primary ? L.F("{0} é o principal", m.Short) : L.F("Definir {0} como principal", m.Short);
            // botão do tamanho do texto, encostado à esquerda; "Desabilitar" logo ao lado
            priBtn.Width = UI.Measure(priBtn.Text, priBtn.Font).Width + 44;
            disBtn.Width = UI.Measure(disBtn.Text, disBtn.Font).Width + 40;
            disBtn.Left = priBtn.Right + 10;

            scaleRow.Controls.Clear();
            foreach (var sc in m.ScaleOptions)
            {
                int s2 = sc; var b = new FlatBtn(sc + "%") { Width = 74 }; b.Width = Math.Max(74, UI.Measure(b.Text, b.Font).Width + 24);
                b.SetActive(sc == m.Scale); b.Enabled = m.Enabled;
                b.Click += delegate { mons[sel].Scale = s2; Refresh2(); }; scaleRow.Controls.Add(b);
            }
            canvas.Invalidate();
        }

        public Screen AnchorScr; static readonly Size BaseSize = new Size(860 + LW, 670); const int LW = 276;
        // abre acima/à esquerda do botão flutuante, no monitor escolhido
        public void PlaceNearTray()
        {
            if (ClientSize != BaseSize) ClientSize = BaseSize;
            var sc = (AnchorScr == null ? null : Screen.AllScreens.FirstOrDefault(z => z.DeviceName == AnchorScr.DeviceName)) ?? Screen.PrimaryScreen; var wa = sc.WorkingArea; var bd = sc.Bounds; int m = 8;
            int x = wa.Right - Width - FloatBtn.Margin_, y = wa.Bottom - FloatBtn.Size_ - FloatBtn.Margin_ * 2 - Height - m;
            if (y < wa.Top) y = wa.Top; if (x < wa.Left) x = wa.Left;
            if (wa.Top > bd.Top) y = wa.Top + m;

            if (wa.Left > bd.Left) x = wa.Left + m;
            Location = new Point(x, y);
        }
        public void OpenAt(Screen s)
        {
            if (Visible && WindowState == FormWindowState.Normal && AnchorScr != null && s != null && AnchorScr.DeviceName == s.DeviceName) { Hide(); return; }
            AnchorScr = s; Load2(); WindowState = FormWindowState.Normal; PlaceNearTray();
            // animação: sobe e aparece a partir do botão
            var end = Location; Opacity = 0; Location = new Point(end.X, end.Y + 24); Show(); Activate();
            var t = new Timer { Interval = 12 }; int k = 0;
            t.Tick += delegate { k++; float f = Math.Min(1f, k / 12f); float ease = 1 - (1 - f) * (1 - f);
                Opacity = ease; Location = new Point(end.X, end.Y + (int)(24 * (1 - ease))); if (f >= 1) { t.Stop(); t.Dispose(); } };
            t.Start();
        }
        public static GraphicsPath Round(RectangleF r, float d)
        {
            var p = new GraphicsPath();
            d = Math.Min(d, Math.Min(r.Width, r.Height));
            if (d < 1 || r.Width < 1 || r.Height < 1) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }

        // ---------- desenho/arraste dos monitores (layout real) ----------
        Dictionary<Mon, RectangleF> chips = new Dictionary<Mon, RectangleF>();
        float vsc = 1, vox, voy; int vbx, vby;
        Mon dragMon; Point dragStart; int dragX0, dragY0; bool dragMoved;

        Mon PrimaryMon() { return mons.FirstOrDefault(x => x.Enabled && x.Primary) ?? mons.FirstOrDefault(x => x.Enabled); }
        // ativa um monitor desligado, sempre à direita dos que já estão ativos
        void PlaceRight(Mon m)
        {
            var on = mons.Where(x => x.Enabled && x != m).ToList(); var pr = PrimaryMon();
            if (on.Count == 0) { m.X = 0; m.Y = 0; return; }
            m.X = on.Max(x => x.X + x.W); m.Y = pr != null && pr != m ? pr.Y : on[0].Y;
        }
        RectangleF ToView(Rectangle r) { return new RectangleF(vox + (r.X - vbx) * vsc, voy + (r.Y - vby) * vsc, r.Width * vsc, r.Height * vsc); }

        void ComputeView()
        {
            var mon = mons.Where(m => m.Enabled).Select(m => new Rectangle(m.X, m.Y, m.W, m.H)).ToList();
            if (mon.Count == 0) return;
            var box = Rectangle.FromLTRB(mon.Min(r => r.Left), mon.Min(r => r.Top), mon.Max(r => r.Right), mon.Max(r => r.Bottom));
            vsc = Math.Min((canvas.Width - 60f) / Math.Max(1, box.Width), (canvas.Height - 100f) / Math.Max(1, box.Height));
            vbx = box.X; vby = box.Y;
            vox = (canvas.Width - box.Width * vsc) / 2; voy = 40 + (canvas.Height - 64 - box.Height * vsc) / 2;
        }

        void DrawMonitors(object s, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; hit.Clear(); chips.Clear();
            using (var cg = Round(new RectangleF(0.5f, 0.5f, canvas.Width - 1.5f, canvas.Height - 1.5f), 28))
            { using (var cb = new SolidBrush(Color.FromArgb(16, 16, 22))) g.FillPath(cb, cg); using (var cpn = new Pen(Color.FromArgb(40, 40, 52))) g.DrawPath(cpn, cg); }
            if (mons == null || mons.Count == 0) return;
            if (dragMon == null) ComputeView();
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // monitores desativados: botões "＋ Ativar X" no topo
            float cx = 16;
            foreach (var m in mons.Where(x => !x.Enabled))
            {
                var f = UI.F("Segoe UI", 8.5f); string t = "＋  " + L.F("Ativar {0}", m.Short);
                float w = g.MeasureString(t, f).Width + 22; var r = new RectangleF(cx, 12, w, 26);
                using (var p = Round(r, 26)) { using (var b = new SolidBrush(Color.FromArgb(28, 28, 36))) g.FillPath(b, p); using (var pn = new Pen(Color.FromArgb(70, 70, 84))) g.DrawPath(pn, p); }
                g.DrawString(t, f, new SolidBrush(Color.FromArgb(215, 215, 225)), r, sf);
                chips[m] = r; cx += w + 8;
            }

            foreach (var m in mons.Where(x => x.Enabled))
            {
                var r = ToView(new Rectangle(m.X, m.Y, m.W, m.H)); r.Inflate(-3, -3); hit[m] = r;
                bool isSel = mons.IndexOf(m) == sel;
                if (r.Width < 4 || r.Height < 4) continue;
                using (var path = Round(r, 16))
                {
                    using (var br = new LinearGradientBrush(r, Color.FromArgb(30, 60, 110), Color.FromArgb(10, 25, 55), 90f)) g.FillPath(br, path);
                    using (var pen = new Pen(isSel ? ACC : Color.FromArgb(70, 70, 80), isSel ? 2.5f : 1.2f)) g.DrawPath(pen, path);
                }
                if (m.Primary)
                {
                    var tf = UI.F("Segoe UI Semibold", 8f); var tsz = g.MeasureString(L.T("PRINCIPAL"), tf);
                    var tag = new RectangleF(r.X + r.Width / 2 - tsz.Width / 2 - 8, r.Y + 6, tsz.Width + 16, 24);
                    using (var tp = Round(tag, 18)) using (var tb = new SolidBrush(ACC)) g.FillPath(tb, tp);
                    g.DrawString(L.T("PRINCIPAL"), tf, Brushes.White, tag, sf);
                }
                string nm = m.Short.Replace("Monitor ", ""); float fs = Math.Max(9, Math.Min(r.Height / 5, 26));
                var nf = UI.F("Segoe UI Semibold", fs);
                while (fs > 6 && g.MeasureString(nm, nf).Width > r.Width - 16) { fs -= 0.5f; nf = UI.F("Segoe UI Semibold", fs); }
                var nsf = new StringFormat(sf); nsf.FormatFlags |= StringFormatFlags.NoWrap; nsf.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(nm, nf, Brushes.White, new RectangleF(r.X + 8, r.Y, r.Width - 16, r.Height), nsf);
                var subF = UI.F("Segoe UI", 7.5f);
                g.DrawString(m.W + "×" + m.H + " · " + m.Hz + "Hz · " + m.Scale + "%", subF, new SolidBrush(Color.FromArgb(150, 170, 200)), new RectangleF(r.X, r.Bottom - 22, r.Width, 18), nsf);
            }
            var sm = mons[sel];
            if (sm.Enabled && mons.Count(x => x.Enabled) > 1)
            {
                var pr = PrimaryMon();
                g.DrawString(sm.Short + "  ·  x " + (sm.X - pr.X) + "   y " + (sm.Y - pr.Y), UI.F("Segoe UI Semibold", 8.5f), new SolidBrush(Color.FromArgb(200, 200, 215)), 18, canvas.Height - 24);
            }
            if (mons.Count(x => x.Enabled) > 1)
                g.DrawString(L.T("arraste ou use as setas (Shift = 10px, Ctrl = 100px)"), UI.F("Segoe UI", 8f), new SolidBrush(Color.FromArgb(90, 90, 105)), new RectangleF(0, canvas.Height - 22, canvas.Width - 18, 18), new StringFormat { Alignment = StringAlignment.Far });
        }

        void CanvasDown(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || mons == null) return;
            foreach (var kv in hit)
                if (kv.Value.Contains(e.Location))
                { dragMon = kv.Key; dragStart = e.Location; dragX0 = dragMon.X; dragY0 = dragMon.Y; dragMoved = false; return; }
        }
        void CanvasMove(object s, MouseEventArgs e)
        {
            if (dragMon == null) return;
            int dx = e.X - dragStart.X, dy = e.Y - dragStart.Y;
            if (Math.Abs(dx) + Math.Abs(dy) > 4) dragMoved = true;
            if (!dragMoved) return;
            dragMon.X = dragX0 + (int)(dx / vsc); dragMon.Y = dragY0 + (int)(dy / vsc); canvas.Invalidate();
        }
        // calibração CRT só no monitor selecionado
        void OpenCrt()
        {
            var m = mons[sel]; var scr = Screen.AllScreens.FirstOrDefault(z => z.DeviceName == m.Gdi);
            if (!m.Enabled || scr == null) { info.Text = L.T("Esse monitor precisa estar ativo."); return; }
            CalibForm.Open(true, scr, this);
        }

        // menu do botão direito sobre um monitor
        void MonitorMenu(Mon m, Point at)
        {
            sel = mons.IndexOf(m); Refresh2();
            var cm = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new DarkMenu()), BackColor = Color.FromArgb(18, 18, 24), ShowImageMargin = false };
            Func<string, Action, bool, ToolStripMenuItem> add = (t, a, en) => { var it = new ToolStripMenuItem(t) { ForeColor = en ? Color.Gainsboro : Color.FromArgb(90, 90, 100), Enabled = en }; it.Click += delegate { a(); }; cm.Items.Add(it); return it; };
            add("⏻   " + L.T("Desabilitar"), () => disBtn.PerformClick(), !m.Primary);
            add("★   " + L.T("Definir como principal"), () => priBtn.PerformClick(), !m.Primary);
            cm.Items.Add(new ToolStripSeparator());
            add("▣   " + L.T("Calibrar geometria CRT"), OpenCrt, true);
            cm.Show(canvas, at);
        }

        void CanvasUp(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                foreach (var kv in hit) if (kv.Value.Contains(e.Location)) { MonitorMenu(kv.Key, e.Location); return; }
                return;
            }
            var m = dragMon; dragMon = null;
            if (m != null && dragMoved) { Touch(m); info.Text = L.F("{0} reposicionado. Clique em Aplicar.", m.Short); sel = mons.IndexOf(m); Refresh2(); return; }
            CanvasClick(s, e);
        }

        // Windows exige que os monitores se toquem. Se o monitor ficou solto ou sobreposto,
        // desliza só o necessário em UM eixo até encostar, mantendo o ajuste fino do outro eixo.
        void Touch(Mon m)
        {
            var others = mons.Where(o => o.Enabled && o != m).ToList();
            if (others.Count == 0) return;
            Func<Rectangle, bool> overlaps = r => others.Any(z => r.IntersectsWith(new Rectangle(z.X, z.Y, z.W, z.H)));
            var cur = new Rectangle(m.X, m.Y, m.W, m.H);
            bool touching = others.Any(o =>
                ((cur.Right == o.X || cur.X == o.X + o.W) && cur.Bottom > o.Y && cur.Y < o.Y + o.H) ||
                ((cur.Bottom == o.Y || cur.Y == o.Y + o.H) && cur.Right > o.X && cur.X < o.X + o.W));
            if (touching && !overlaps(cur)) return;
            Point best = new Point(m.X, m.Y); long bd = long.MaxValue;
            foreach (var o in others)
            {
                var c = new List<Point>();
                if (m.Y + m.H > o.Y && m.Y < o.Y + o.H) { c.Add(new Point(o.X + o.W, m.Y)); c.Add(new Point(o.X - m.W, m.Y)); }
                if (m.X + m.W > o.X && m.X < o.X + o.W) { c.Add(new Point(m.X, o.Y + o.H)); c.Add(new Point(m.X, o.Y - m.H)); }
                foreach (var p in c)
                {
                    if (overlaps(new Rectangle(p.X, p.Y, m.W, m.H))) continue;
                    long d = (long)(p.X - m.X) * (p.X - m.X) + (long)(p.Y - m.Y) * (p.Y - m.Y);
                    if (d < bd) { bd = d; best = p; }
                }
            }
            m.X = best.X; m.Y = best.Y;
        }

        // setas movem o monitor selecionado: 1px · Shift 10px · Ctrl 100px
        protected override bool ProcessCmdKey(ref Message msg, Keys k)
        {
            var key = k & Keys.KeyCode; int step = (k & Keys.Shift) != 0 ? 10 : (k & Keys.Control) != 0 ? 100 : 1;
            if (mons != null && mons.Count > 0 && (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down))
            {
                var m = mons[sel]; if (!m.Enabled) return true;
                if (key == Keys.Left) m.X -= step; if (key == Keys.Right) m.X += step; if (key == Keys.Up) m.Y -= step; if (key == Keys.Down) m.Y += step;
                info.Text = L.F("{0} ajustado. Clique em Aplicar.", m.Short); canvas.Invalidate(); return true;
            }
            return base.ProcessCmdKey(ref msg, k);
        }

        void CanvasClick(object s, MouseEventArgs e)
        {
            foreach (var kv in chips)
                if (kv.Value.Contains(e.Location))
                {
                    var m = kv.Key; PlaceRight(m); m.Enabled = true; sel = mons.IndexOf(m);
                    info.Text = L.F("{0} será ativado. Clique em Aplicar.", m.Short); Refresh2(); return;
                }
            foreach (var kv in hit)
                if (kv.Value.Contains(e.Location)) { sel = mons.IndexOf(kv.Key); Refresh2(); return; }
        }

        void DoApply(object s, EventArgs e) { ApplyCurrent(); }
        void ApplyCurrent()
        {
            foreach (var m in mons.Where(x => x.Enabled && !x.Primary).ToList()) Touch(m);
            var backup = Display.Read();
            var err = Display.Apply(mons.Select(m => m.Clone()).ToList());
            if (err != null) { MessageBox.Show(err, "Monitor Hub"); Load2(); PlaceNearTray(); return; }
            if (!new ConfirmForm().Confirmed) { Display.Apply(backup); info.Text = L.T("Alterações revertidas."); }
            else { info.Text = L.T("Configurações aplicadas."); Mon.SaveRes(mons); }
            Load2(); PlaceNearTray();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } else base.OnFormClosing(e);
        }
    }

    // "Manter alterações?" with 15s countdown -> revert
    public class ConfirmForm : Form
    {
        public bool Confirmed; int left = 15; Label l;
        public ConfirmForm()
        {
            Text = "Monitor Hub"; BackColor = Color.FromArgb(10, 10, 12); ForeColor = Color.Gainsboro; Font = UI.F("Segoe UI", 10f);
            ClientSize = new Size(380, 140); FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterScreen; TopMost = true;
            l = new Label { Left = 20, Top = 20, Width = 340, Height = 50 }; Controls.Add(l);
            var keep = new FlatBtn(L.T("Manter")) { Left = 150, Top = 85, Width = 100 }; keep.SetActive(true);
            keep.Click += delegate { Confirmed = true; Close(); };
            var rev = new FlatBtn(L.T("Reverter")) { Left = 260, Top = 85, Width = 100 }; rev.Click += delegate { Close(); };
            Controls.Add(keep); Controls.Add(rev);
            var t = new Timer { Interval = 1000 };
            t.Tick += delegate { left--; Upd(); if (left <= 0) { t.Stop(); Close(); } };
            Upd(); t.Start(); ShowDialog();
        }
        void Upd() { l.Text = L.F("Manter estas configurações de vídeo?\nRevertendo em {0}s...", left); }
    }

    public static class Program
    {
        static System.Threading.Mutex mtx; static NotifyIcon trayRef;
        // reinicia o app (usado ao trocar o idioma)
        public static void Restart()
        {
            try { if (trayRef != null) trayRef.Visible = false; mtx.ReleaseMutex(); mtx.Dispose(); } catch { }
            System.Diagnostics.Process.Start(Application.ExecutablePath);
            Environment.Exit(0);
        }
        public static Icon AppIcon = SystemIcons.Application; public static Image Mascot;
        static void LogErr(object ex)
        {
            try { var d = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorHub"); System.IO.Directory.CreateDirectory(d); System.IO.File.AppendAllText(System.IO.Path.Combine(d, "erro.txt"), DateTime.Now + "\r\n" + ex + "\r\n\r\n"); } catch { }
        }
        [STAThread]
        public static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object o, System.Threading.ThreadExceptionEventArgs e) { LogErr(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object o, UnhandledExceptionEventArgs e) { LogErr(e.ExceptionObject); };
            bool isNew; mtx = new System.Threading.Mutex(true, "MonitorHub_single", out isNew);
            if (!isNew) { MessageBox.Show(L.T("O Monitor Hub já está aberto na bandeja do sistema."), "Monitor Hub"); return; }
            try { AppIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            try { Mascot = Image.FromStream(typeof(Program).Assembly.GetManifestResourceStream("mascot.png")); } catch { }
            W.DpiAware(); L.Load(); Mon.LoadRes(); Startup.Sync();
            bool trayOnly = args.Any(a => a == "--tray");
            Application.EnableVisualStyles();
            var form = new MainForm();
            var tray = new NotifyIcon { Icon = AppIcon, Text = "Monitor Hub", Visible = true }; trayRef = tray;
            var menu = new ContextMenuStrip();
            Action open = delegate { form.AnchorScr = null; form.Load2(); form.PlaceNearTray(); form.Show(); form.WindowState = FormWindowState.Normal; form.Activate(); };
            menu.Items.Add(L.T("Atalho"), null, delegate { open(); });
            var stItem = new ToolStripMenuItem(L.T("Iniciar com o Windows")) { Checked = Startup.On, CheckOnClick = true };
            stItem.CheckedChanged += delegate { Startup.On = stItem.Checked; };
            menu.Opening += delegate { stItem.Checked = Startup.On; };
            menu.Items.Add(stItem);
            menu.Items.Add(L.T("Sair"), null, delegate { tray.Visible = false; Application.Exit(); });

            tray.ContextMenuStrip = menu;
            tray.MouseClick += delegate(object o, MouseEventArgs e) { if (e.Button == MouseButtons.Left) open(); };
            var h = form.Handle;   // garante a janela criada mesmo iniciando só na bandeja
            form.PlaceNearTray(); if (!trayOnly) form.Show();
            FloatBtn.Build(form.OpenAt);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += delegate { form.BeginInvoke((Action)(() => FloatBtn.Build(form.OpenAt))); };
            Application.Run(); GC.KeepAlive(mtx);
            tray.Visible = false;
        }
    }
}
