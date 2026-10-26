using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MonitorHub
{
    // Botão flutuante com o ícone do app, fixo no canto inferior direito de cada monitor (na área de trabalho)
    public class FloatBtn : Form
    {
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string c, string n);
        [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);

        public const int Size_ = 46, Margin_ = 3, Pad = 12;   // quadrado, margem até a borda/barra, área da sombra
        static readonly List<FloatBtn> all = new List<FloatBtn>();
        static Action<Screen> onClick;
        public Screen Scr; bool over;

        [StructLayout(LayoutKind.Sequential)] struct BLEND { public byte Op, Flags, Alpha, Fmt; }
        [StructLayout(LayoutKind.Sequential)] struct PT { public int X, Y; public PT(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] struct SZ { public int W, H; public SZ(int w, int h) { W = w; H = h; } }
        [DllImport("user32.dll")] static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dst, ref PT pos, ref SZ size, IntPtr src, ref PT srcPos, int key, ref BLEND b, int flags);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);

        public static void Build(Action<Screen> click)
        {
            onClick = click;
            foreach (var b in all) b.Dispose(); all.Clear();
            foreach (var s in Screen.AllScreens) { var b = new FloatBtn(s); all.Add(b); b.Show(); b.Pin(); }
        }

        FloatBtn(Screen s)
        {
            Scr = s; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Cursor = Cursors.Hand;
            var wa = s.WorkingArea; int W = Size_ + Pad * 2;
            Bounds = new Rectangle(wa.Right - Size_ - Margin_ - Pad, wa.Bottom - Size_ - Margin_ - Pad, W, W);
        }
        // toolwindow + noactivate + layered (transparência por pixel para a sombra)
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80 | 0x08000000 | 0x80000; return cp; } }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Render(); }

        // fica preso à área de trabalho: acima do papel de parede, abaixo das janelas
        void Pin()
        {
            var prog = FindWindow("Progman", null);
            if (prog != IntPtr.Zero) SetWindowLongPtr(Handle, -8, prog);
            SetWindowPos(Handle, (IntPtr)1, 0, 0, 0, 0, 0x13);
        }

        void Render()
        {
            int W = Size_ + Pad * 2;
            using (var bmp = new Bitmap(W, W, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.Clear(Color.Transparent);
                    // sombra suave: camadas expandindo com alpha decrescente, levemente deslocada para baixo
                    for (int i = Pad; i >= 1; i--)
                    {
                        int a = (int)(34 * Math.Pow(1 - i / (float)(Pad + 1), 2));
                        using (var p = MainForm.Round(new RectangleF(Pad - i, Pad - i + 2, Size_ + i * 2, Size_ + i * 2), 12 + i * 2))
                        using (var b = new SolidBrush(Color.FromArgb(a, 0, 0, 0))) g.FillPath(b, p);
                    }
                    var r = new RectangleF(Pad, Pad, Size_, Size_);
                    using (var p = MainForm.Round(r, 12))
                    using (var b = new SolidBrush(over ? Color.FromArgb(255, 26, 26, 30) : Color.FromArgb(255, 8, 8, 10))) g.FillPath(b, p);
                    if (Program.Mascot != null) g.DrawImage(Program.Mascot, new Rectangle(Pad + 7, Pad + 7, Size_ - 14, Size_ - 14));
                }
                IntPtr sdc = GetDC(IntPtr.Zero), mdc = CreateCompatibleDC(sdc), hb = bmp.GetHbitmap(Color.FromArgb(0)), old = SelectObject(mdc, hb);
                var size = new SZ(W, W); var src = new PT(0, 0); var pos = new PT(Left, Top);
                var bl = new BLEND { Op = 0, Flags = 0, Alpha = 255, Fmt = 1 };
                UpdateLayeredWindow(Handle, sdc, ref pos, ref size, mdc, ref src, 0, ref bl, 2);
                SelectObject(mdc, old); DeleteObject(hb); DeleteDC(mdc); ReleaseDC(IntPtr.Zero, sdc);
            }
        }
        protected override void OnMouseEnter(EventArgs e) { over = true; Render(); }
        protected override void OnMouseLeave(EventArgs e) { over = false; Render(); }
        protected override void OnMouseUp(MouseEventArgs e) { if (e.Button == MouseButtons.Left && onClick != null) onClick(Scr); Pin(); }
    }
}
