using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MonitorHub
{
    // Auxiliar de calibração (cores ou CRT): mesmo padrão em todos os monitores.
    // clique / → / espaço = próximo · botão direito / ← = anterior · ↑ ↓ = ajuste (padrões interativos) · H = dicas · Esc = sair
    public class CalibForm : Form
    {
        static readonly string[][] ColorPats =
        {
            new[] { "Branco 100%", "Compare o tom do branco entre os monitores. Ajuste os ganhos R/G/B no menu do monitor até ficarem iguais (nem azulado, nem amarelado)." },
            new[] { "Cinza 50%", "Os dois devem ter o mesmo cinza neutro. Um tom rosado, verde ou azul indica ganho R/G/B desequilibrado." },
            new[] { "Cinza 20%", "Cinza escuro neutro. Ajuda a ver diferenças de cor em tons baixos." },
            new[] { "Degraus de cinza", "Os 16 degraus devem ser todos distinguíveis e neutros, sem tons coloridos." },
            new[] { "Rampa de cinza", "Deve ser suave, sem faixas marcadas e sem mudar de cor ao longo da rampa." },
            new[] { "Nível de preto", "Deve dar para ver os quadrados 2 a 20. Se os primeiros somem, aumente o brilho; se o fundo fica cinza, diminua." },
            new[] { "Nível de branco", "Deve dar para ver os quadrados 235 a 254 sobre o branco. Se somem, diminua o contraste." },
            new[] { "Gama 2.2", "Olhe de longe ou desfocando a vista: o quadrado central deve se misturar às listras. Mais claro = gama baixa; mais escuro = gama alta." },
            new[] { "Barras de cor", "Compare a saturação. Se um monitor estiver mais vibrante, reduza a saturação dele (modo sRGB ou driver AMD)." },
            new[] { "Rampas de cor", "Vermelho, verde e azul devem crescer suavemente, sem saltos nem mudança de tom." },
        };
        static readonly string[][] CrtPats =
        {
            new[] { "Grade", "As linhas devem ser retas e os quadrados iguais em toda a tela. Ajuste geometria: tamanho, posição, almofada (pincushion), trapézio e rotação." },
            new[] { "Monoscópio", "Os círculos devem ser redondos (não ovais). Retângulos vermelhos: 90% e 80% (áreas seguras). O centro deve coincidir com o centro físico do tubo." },
            new[] { "Convergência", "Linhas e pontos devem ser brancos puros. Franjas vermelhas, verdes ou azuis indicam má convergência (ajuste no menu ou ímãs do tubo)." },
            new[] { "Linearidade", "Todos os círculos devem ter o mesmo tamanho e forma. Se os das bordas estão maiores ou achatados, ajuste a linearidade." },
            new[] { "Overscan", "Cada borda marca 1% a 10% da imagem. Veja quanto o tubo esconde e ajuste o tamanho até a borda desejada aparecer." },
            new[] { "Barras 16 degraus", "Cada degrau (0 a F) deve ser distinto. Se os primeiros somem, aumente o brilho; se os últimos se fundem, diminua o contraste." },
            new[] { "Nitidez", "Linhas de 1 e 2 pixels e xadrez fino. Devem aparecer separadas e uniformes do centro aos cantos (foco)." },
            new[] { "Pureza de cor", "A cor deve ser uniforme em toda a tela, sem manchas (se houver, desmagnetize o tubo). ↑ ↓ trocam a cor." },
            new[] { "Rolagem", "Listras e quadrado em movimento: verifique rastros, tremulação e geometria em movimento. ↑ ↓ mudam a velocidade." },
            new[] { "Grade rolante", "A grade deve se mover sem ondular nem deformar. Ondulação indica fonte ou deflexão instável. ↑ ↓ mudam a velocidade." },
        };

        static string[][] Pats = ColorPats; static bool Crt;
        public static int Idx, Param; public static bool Help = true;
        static List<CalibForm> all = new List<CalibForm>();
        static Timer anim; static Stopwatch clock = Stopwatch.StartNew();
        static bool Animated { get { return Crt && (Idx == 8 || Idx == 9); } }

        // cores: todos os monitores (para comparar) · CRT: só o monitor indicado
        static CalibPanel panel;
        public static void Open(bool crt, Screen only = null, Form app = null)
        {
            Close_(); Crt = crt; Pats = crt ? CrtPats : ColorPats; Idx = 0; Param = 0;
            foreach (var s in (only != null ? new[] { only } : Screen.AllScreens))
            {
                var area = crt ? s.Bounds : s.WorkingArea;   // CRT: tela cheia de verdade
                var f = new CalibForm { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, Bounds = area, TopMost = crt, ShowInTaskbar = false, BackColor = Color.Black, KeyPreview = true };
                f.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
                f.Tag = area;
                all.Add(f); f.Show(); f.Bounds = area;
            }
            if (anim == null) { anim = new Timer { Interval = 16 }; anim.Tick += delegate { if (Animated) foreach (var f in all) f.Invalidate(); }; }
            anim.Start();
            // CRT: as dicas e os controles ficam num painel acima da janela do app, fora da imagem de teste
            if (crt && app != null) { panel = new CalibPanel(app); panel.Show(); panel.Sync(); }
            if (all.Count > 0) all[0].Activate();
        }
        public static string Title { get { return (Idx + 1) + "/" + Pats.Length + "  ·  " + L.T(Pats[Idx][0]); } }
        public static string Tip { get { return L.T(Pats[Idx][1]); } }
        public static bool Interactive { get { return Crt && Idx >= 7; } }
        public static void Next() { Go(1); } public static void Prev() { Go(-1); }
        public static void Adjust(int d) { Param += d; Redraw(); }
        public static void Stop() { Exit(); }
        public static void Key(KeyEventArgs e) { all.FirstOrDefault()?.OnKeyDown(e); }
        static void Close_() { foreach (var f in all.ToList()) f.Dispose(); all.Clear(); if (panel != null) { panel.Dispose(); panel = null; } }
        static void Redraw() { foreach (var f in all) f.Invalidate(); if (panel != null) panel.Sync(); }
        static void Go(int d) { Idx = (Idx + d + Pats.Length) % Pats.Length; Param = 0; Redraw(); }
        static void Exit() { if (anim != null) anim.Stop(); Close_(); }

        // garante que cada janela ocupe exatamente o seu monitor, independente dos outros
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (Tag is Rectangle && Bounds != (Rectangle)Tag) Bounds = (Rectangle)Tag; Invalidate(); }
        protected override void WndProc(ref Message m) { if (m.Msg == 0x02E0) { if (Tag is Rectangle) Bounds = (Rectangle)Tag; Invalidate(); return; } base.WndProc(ref m); }
        internal new void OnKeyDown(KeyEventArgs e) { Handle_(e); }
        protected override bool ProcessCmdKey(ref Message msg, Keys k) { Handle_(new KeyEventArgs(k)); return true; }
        void Handle_(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Exit();
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Back) Go(-1);
            else if (e.KeyCode == Keys.H) { Help = !Help; Redraw(); }
            else if (e.KeyCode == Keys.Up) { Param++; Redraw(); }
            else if (e.KeyCode == Keys.Down) { Param--; Redraw(); }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) Go(1);
        }
        int lastAct;
        protected override void OnActivated(EventArgs e) { lastAct = Environment.TickCount; base.OnActivated(e); }
        // o 1º clique só traz a tela de volta (ex.: depois de usar o painel da AMD); os seguintes trocam o padrão
        protected override void OnMouseDown(MouseEventArgs e) { if (Environment.TickCount - lastAct < 300) return; Go(e.Button == MouseButtons.Right ? -1 : 1); }

        static Color G(int v) { return Color.FromArgb(v, v, v); }
        static void Fill(Graphics g, Color c, float x, float y, float w, float h) { using (var b = new SolidBrush(c)) g.FillRectangle(b, x, y, w, h); }
        static void Box(Graphics g, Color c, float w, RectangleF r) { using (var p = new Pen(c, w)) g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; int W = ClientSize.Width, H = ClientSize.Height;
            g.Clear(Color.Black); g.SmoothingMode = SmoothingMode.None; g.InterpolationMode = InterpolationMode.NearestNeighbor;
            if (Crt) PaintCrt(g, W, H); else PaintColor(g, W, H);
            if (Help && !Crt)
            {
                bool small = W < 900; int bw = Math.Min(760, W - 16), bh = small ? 84 : 106;
                var r = new Rectangle((W - bw) / 2, H - bh - (small ? 8 : 30), bw, bh);
                using (var b = new SolidBrush(Color.FromArgb(220, 12, 12, 16))) using (var p = MainForm.Round(r, 24)) g.FillPath(b, p);
                TextRenderer.DrawText(g, (Idx + 1) + "/" + Pats.Length + "  ·  " + L.T(Pats[Idx][0]), UI.F("Segoe UI Semibold", small ? 9f : 12f), new Rectangle(r.X + 12, r.Y + 6, r.Width - 24, 20), Color.FromArgb(240, 70, 90), TextFormatFlags.Left);
                TextRenderer.DrawText(g, L.T(Pats[Idx][1]), UI.F("Segoe UI", small ? 7.5f : 10f), new Rectangle(r.X + 12, r.Y + (small ? 26 : 32), r.Width - 24, small ? 36 : 48), Color.Gainsboro, TextFormatFlags.Left | TextFormatFlags.WordBreak);
                TextRenderer.DrawText(g, L.T("clique / → próximo   ·   botão direito / ← anterior   ·   H esconde dicas   ·   Esc sai"), UI.F("Segoe UI", small ? 6.5f : 8.5f), new Rectangle(r.X + 12, r.Bottom - (small ? 16 : 22), r.Width - 24, 16), Color.Gray, TextFormatFlags.Left);
            }
        }

        // ================= cores =================
        static void PaintColor(Graphics g, int W, int H)
        {
            switch (Idx)
            {
                case 0: g.Clear(G(255)); break;
                case 1: g.Clear(G(128)); break;
                case 2: g.Clear(G(51)); break;
                case 3: for (int i = 0; i < 16; i++) Fill(g, G(Math.Min(255, i * 17)), W * i / 16f, 0, W / 16f + 1, H); break;
                case 4: for (int x = 0; x < W; x++) Fill(g, G(x * 255 / Math.Max(1, W - 1)), x, 0, 1, H); break;
                case 5: Squares(g, W, H, 0, i => (i + 1) * 2, 60); break;
                case 6: Squares(g, W, H, 255, i => 235 + i * 2, 180); break;
                case 7:
                    for (int y = 0; y < H; y++) Fill(g, y % 2 == 0 ? G(0) : G(255), 0, y, W, 1);
                    { float s = Math.Min(W, H) / 3f; Fill(g, G(186), (W - s) / 2, (H - s) / 2, s, s); }
                    break;
                case 8:
                    { var cs = new[] { Color.White, Color.Yellow, Color.Cyan, Color.Lime, Color.Magenta, Color.Red, Color.Blue, Color.Black };
                      for (int i = 0; i < cs.Length; i++) Fill(g, cs[i], W * i / (float)cs.Length, 0, W / (float)cs.Length + 1, H); }
                    break;
                case 9:
                    { var cs = new[] { Color.Red, Color.Lime, Color.Blue, Color.White };
                      for (int k = 0; k < 4; k++)
                          for (int x = 0; x < W; x++)
                          { float t = x / (float)Math.Max(1, W - 1); var c = cs[k];
                            Fill(g, Color.FromArgb((int)(c.R * t), (int)(c.G * t), (int)(c.B * t)), x, H * k / 4f, 1, H / 4f + 1); } }
                    break;
            }
        }
        static void Squares(Graphics g, int W, int H, int bg, Func<int, int> val, int label)
        {
            g.Clear(G(bg)); int n = 10; float s = Math.Min(W / (n + 2f), H / 4f);
            for (int i = 0; i < n; i++)
            {
                int v = val(i); float x = (W - n * s) / 2 + i * s;
                Fill(g, G(v), x + 4, (H - s) / 2, s - 8, s - 8);
                TextRenderer.DrawText(g, v.ToString(), UI.F("Segoe UI", 10f), new Point((int)x + 6, (int)((H + s) / 2)), G(label));
            }
        }

        // ================= CRT (área 4:3 centralizada) =================
        static void PaintCrt(Graphics g, int W, int H)
        {
            var R = new RectangleF(0, 0, W, H);   // ocupa a tela inteira (CRT costuma rodar em 320x240, 640x480...)
            float cw = R.Width / 16f, ch = R.Height / 12f;   // grade 16x12 (células quadradas em 4:3)
            float cx = R.X + R.Width / 2, cy = R.Y + R.Height / 2;
            g.SetClip(R);
            switch (Idx)
            {
                case 0: // grade (imagem de referência, esticada pixel a pixel na tela inteira)
                    if (Grid != null)
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(Grid, new Rectangle(0, 0, W, H), 0, 0, Grid.Width, Grid.Height, GraphicsUnit.Pixel);
                        g.PixelOffsetMode = PixelOffsetMode.Default;
                    }
                    break;
                case 1: // monoscópio
                    for (int i = 0; i <= 16; i++) Fill(g, G(90), R.X + i * cw - 1, R.Y, 2, R.Height);
                    for (int j = 0; j <= 12; j++) Fill(g, G(90), R.X, R.Y + j * ch - 1, R.Width, 2);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = new Pen(Color.White, 3))
                    {
                        float d = R.Height * 0.92f; g.DrawEllipse(p, cx - d / 2, cy - d / 2, d, d);
                        float k = R.Height / 4.2f;
                        foreach (var pt in new[] { new PointF(R.X + k * 0.62f, R.Y + k * 0.62f), new PointF(R.Right - k * 0.62f, R.Y + k * 0.62f), new PointF(R.X + k * 0.62f, R.Bottom - k * 0.62f), new PointF(R.Right - k * 0.62f, R.Bottom - k * 0.62f) })
                            g.DrawEllipse(p, pt.X - k / 2, pt.Y - k / 2, k, k);
                        g.DrawLine(p, cx, R.Y, cx, R.Bottom); g.DrawLine(p, R.X, cy, R.Right, cy);
                    }
                    using (var p = new Pen(Color.Red, 3))
                    {
                        Box(g, Color.Red, 3, Inset(R, 0.05f)); Box(g, Color.Red, 3, Inset(R, 0.10f));
                        float d = R.Height * 0.2f; g.DrawEllipse(p, cx - d / 2, cy - d / 2, d, d);
                    }
                    g.SmoothingMode = SmoothingMode.None;
                    Box(g, Color.White, 4, new RectangleF(R.X + 2, R.Y + 2, R.Width - 4, R.Height - 4));
                    break;
                case 2: // convergência
                    for (int i = 0; i <= 16; i++) Fill(g, Color.White, (int)(R.X + i * cw), R.Y, 1, R.Height);
                    for (int j = 0; j <= 12; j++) Fill(g, Color.White, R.X, (int)(R.Y + j * ch), R.Width, 1);
                    for (int i = 0; i < 16; i++) for (int j = 0; j < 12; j++) Fill(g, Color.White, (int)(R.X + i * cw + cw / 2), (int)(R.Y + j * ch + ch / 2), 2, 2);
                    break;
                case 3: // linearidade
                    for (int i = 0; i <= 16; i++) Fill(g, G(70), R.X + i * cw, R.Y, 1, R.Height);
                    for (int j = 0; j <= 12; j++) Fill(g, G(70), R.X, R.Y + j * ch, R.Width, 1);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = new Pen(Color.White, 2))
                    {
                        float d = ch * 3.2f;
                        for (int i = 0; i < 4; i++) for (int j = 0; j < 3; j++)
                        { float x = R.X + R.Width * (i + 0.5f) / 4, y = R.Y + R.Height * (j + 0.5f) / 3; g.DrawEllipse(p, x - d / 2, y - d / 2, d, d); }
                    }
                    g.SmoothingMode = SmoothingMode.None;
                    break;
                case 4: // overscan 1% a 10%
                    for (int k = 10; k >= 1; k--)
                    {
                        var c = k % 2 == 0 ? Color.White : Color.Red;
                        Box(g, c, 2, Inset(R, k * 2 / 100f));
                        TextRenderer.DrawText(g, k + "%", UI.F("Segoe UI", 8f), new Point((int)(cx - 10), (int)(R.Y + R.Height * k / 100f - 1)), c);
                        TextRenderer.DrawText(g, k + "%", UI.F("Segoe UI", 8f), new Point((int)(R.X + R.Width * k / 100f + 2), (int)(cy - 6)), c);
                    }
                    Box(g, Color.Lime, 2, new RectangleF(R.X + 1, R.Y + 1, R.Width - 2, R.Height - 2));
                    break;
                case 5: // barras 16 degraus R G B W
                    {
                        var cs = new[] { Color.Red, Color.Lime, Color.Blue, Color.White }; var nm = new[] { "R", "G", "B", "W" };
                        float lx = R.X + R.Width * 0.12f, bw = (R.Right - R.Width * 0.03f - lx) / 16f, bh = R.Height * 0.15f;
                        for (int i = 0; i < 16; i++) TextRenderer.DrawText(g, i.ToString("X"), UI.F("Consolas", 12f, FontStyle.Bold), new Point((int)(lx + i * bw + bw / 3), (int)(R.Y + R.Height * 0.06f)), Color.Orange);
                        for (int k = 0; k < 4; k++)
                        {
                            float y = R.Y + R.Height * 0.14f + k * bh * 1.25f;
                            TextRenderer.DrawText(g, nm[k], UI.F("Consolas", 14f, FontStyle.Bold), new Point((int)(R.X + R.Width * 0.04f), (int)(y + bh / 3)), cs[k]);
                            for (int i = 0; i < 16; i++) { int v = i * 17; Fill(g, Color.FromArgb(cs[k].R * v / 255, cs[k].G * v / 255, cs[k].B * v / 255), lx + i * bw, y, bw + 1, bh); }
                        }
                    }
                    break;
                case 6: // nitidez
                    {
                        float qh = R.Height / 4;
                        for (int x = (int)R.X; x < R.Right; x++) Fill(g, (x & 1) == 0 ? Color.White : Color.Black, x, R.Y, 1, qh);                // linhas verticais 1px
                        for (int x = (int)R.X; x < R.Right; x++) Fill(g, ((x >> 1) & 1) == 0 ? Color.White : Color.Black, x, R.Y + qh, 1, qh);     // 2px
                        for (int y = (int)(R.Y + qh * 2); y < R.Y + qh * 3; y++) Fill(g, (y & 1) == 0 ? Color.White : Color.Black, R.X, y, R.Width, 1); // horizontais 1px
                        for (int y = (int)(R.Y + qh * 3); y < R.Bottom; y += 2)                                                                     // xadrez 2px
                            for (int x = (int)R.X; x < R.Right; x += 2) if ((((x - (int)R.X) >> 1) + ((y - (int)(R.Y + qh * 3)) >> 1)) % 2 == 0) Fill(g, Color.White, x, y, 2, 2);
                    }
                    break;
                case 7: // pureza
                    { var cs = new[] { Color.Red, Color.Lime, Color.Blue, Color.White }; Fill(g, cs[((Param % 4) + 4) % 4], R.X, R.Y, R.Width, R.Height); }
                    break;
                case 8: // rolagem
                    {
                        float t = clock.ElapsedMilliseconds / 1000f, sp = 120 * (float)Math.Pow(1.4, Param);
                        float off = (t * sp) % (cw * 2);
                        for (float x = R.X - cw * 2 + off; x < R.Right; x += cw * 2) Fill(g, Color.White, x, R.Y, cw, R.Height / 2);
                        float sq = ch * 2, px = R.X + ((t * sp * 1.5f) % (R.Width + sq)) - sq;
                        Fill(g, Color.White, px, R.Y + R.Height * 0.62f, sq, sq);
                        Fill(g, G(80), R.X, R.Y + R.Height / 2, R.Width, 2);
                    }
                    break;
                case 9: // grade rolante
                    {
                        float t = clock.ElapsedMilliseconds / 1000f, sp = 60 * (float)Math.Pow(1.4, Param);
                        float ox = (t * sp) % cw, oy = (t * sp * 0.75f) % ch;
                        for (float x = R.X - cw + ox; x < R.Right; x += cw) Fill(g, Color.White, x, R.Y, 2, R.Height);
                        for (float y = R.Y - ch + oy; y < R.Bottom; y += ch) Fill(g, Color.White, R.X, y, R.Width, 2);
                    }
                    break;
            }
            g.ResetClip();
        }
        static Image grid; static bool gridTried;
        static Image Grid { get { if (!gridTried) { gridTried = true; try { grid = Image.FromStream(typeof(CalibForm).Assembly.GetManifestResourceStream("crt_grid.png")); } catch { } } return grid; } }
        static RectangleF Inset(RectangleF r, float f) { return new RectangleF(r.X + r.Width * f / 2, r.Y + r.Height * f / 2, r.Width * (1 - f), r.Height * (1 - f)); }
    }

    // painel de dicas/controles da calibração CRT, acima da janela do app
    public class CalibPanel : Form
    {
        Label title, tip; FlatBtn up, down;
        public CalibPanel(Form app)
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black; ForeColor = Color.Gainsboro; DoubleBuffered = true; KeyPreview = true;
            int w = app.Width, h = 150;
            var wa = Screen.FromControl(app).WorkingArea;
            Bounds = new Rectangle(app.Left, Math.Max(wa.Top, app.Top - h - 8), w, h);
            title = new Label { Left = 20, Top = 14, Width = w - 40, Height = 24, Font = UI.F("Segoe UI Semibold", 12f), ForeColor = Color.FromArgb(240, 70, 90) };
            tip = new Label { Left = 20, Top = 42, Width = w - 40, Height = 52, Font = UI.F("Segoe UI", 10f) };
            var prev = new FlatBtn("◀") { Left = 20, Top = h - 48, Width = 44, Height = 34, Radius = 17 };
            var next = new FlatBtn("▶") { Left = 70, Top = h - 48, Width = 44, Height = 34, Radius = 17 }; next.SetActive(true);
            up = new FlatBtn("▲") { Left = 134, Top = h - 48, Width = 44, Height = 34, Radius = 17 };
            down = new FlatBtn("▼") { Left = 184, Top = h - 48, Width = 44, Height = 34, Radius = 17 };
            var close = new FlatBtn("✕  " + L.T("Fechar")) { Top = h - 48, Height = 34, Radius = 17 };
            close.Width = TextRenderer.MeasureText(close.Text, close.Font).Width + 36; close.Left = w - close.Width - 20;
            prev.Click += delegate { CalibForm.Prev(); }; next.Click += delegate { CalibForm.Next(); };
            up.Click += delegate { CalibForm.Adjust(1); }; down.Click += delegate { CalibForm.Adjust(-1); };
            close.Click += delegate { CalibForm.Stop(); };
            Controls.AddRange(new Control[] { title, tip, prev, next, up, down, close });
        }
        public void Sync() { title.Text = CalibForm.Title; tip.Text = CalibForm.Tip; up.Visible = down.Visible = CalibForm.Interactive; }
        protected override bool ProcessCmdKey(ref Message msg, Keys k) { CalibForm.Key(new KeyEventArgs(k)); return true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = MainForm.Round(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 24))
            { using (var b = new SolidBrush(Color.FromArgb(16, 16, 22))) e.Graphics.FillPath(b, p); using (var pn = new Pen(Color.FromArgb(48, 48, 60))) e.Graphics.DrawPath(pn, p); }
        }
    }
}
