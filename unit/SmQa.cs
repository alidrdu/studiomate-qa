// StudioMate QA - birim / davranış testleri (tek dosyalık test çalıştırıcı)
//
// Yöntem: Test edilen uygulama (StudioMate.exe, geliştirme derlemesi) yansıma (reflection) ile yüklenir.
// Üretim kodu değiştirilmez, kaynak kodu bu depoda yoktur. Hesap sonuçları, burada bağımsız
// yazılmış "kâğıt-kalem" referans hesaplarıyla (oracle) ve değişmezlerle (invariant) karşılaştırılır.
//
// Derleme (C# 5, csc.exe):  bkz. run-unit.ps1
// Çalıştırma:               SmQa.exe <StudioMate.exe yolu> [sonuc-dosyasi.txt]

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace SmQa
{
    class AssertFail : Exception { public AssertFail(string m) : base(m) { } }

    static class A
    {
        public static int Count;
        public static void True(bool c, string msg) { Count++; if (!c) throw new AssertFail(msg); }
        public static void Eq(object exp, object act, string msg)
        {
            Count++;
            if (!object.Equals(exp, act)) throw new AssertFail(msg + "  [beklenen=" + exp + ", gerçek=" + act + "]");
        }
        public static void Near(double exp, double act, double tol, string msg)
        {
            Count++;
            if (Math.Abs(exp - act) > tol) throw new AssertFail(msg + "  [beklenen=" + exp.ToString("0.#####", CultureInfo.InvariantCulture) + ", gerçek=" + act.ToString("0.#####", CultureInfo.InvariantCulture) + "]");
        }
    }

    class Result { public string Group, Name, Status, Detail; }

    // ---------------------------------------------------------------- yansıma yardımcıları
    static class R
    {
        public const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static object Field(object o, string n) { return o.GetType().GetField(n, BF).GetValue(o); }
        public static void SetField(object o, string n, object v) { o.GetType().GetField(n, BF).SetValue(o, v); }
        public static object Prop(object o, string n) { return o.GetType().GetProperty(n, BF).GetValue(o, null); }
        public static void SetProp(object o, string n, object v) { o.GetType().GetProperty(n, BF).SetValue(o, v, null); }
        public static object Call(object o, string n, params object[] a)
        {
            try { return o.GetType().GetMethod(n, BF).Invoke(o, a); }
            catch (TargetInvocationException e) { throw e.InnerException; }
        }
    }

    // GridLayout / PaperLayout alanlarının çift duyarlıklı kopyası
    class Lay
    {
        public double PageW, PageH, SizeW, SizeH, Gap, GapY, X0, Y0, MaxGapX, MaxGapY;
        public int Cols, Rows, Used, PerPage;
        public static Lay From(object g)
        {
            Lay l = new Lay();
            l.PageW = Cv(R.Field(g, "PageW")); l.PageH = Cv(R.Field(g, "PageH"));
            l.SizeW = Cv(R.Field(g, "SizeW")); l.SizeH = Cv(R.Field(g, "SizeH"));
            l.Gap = Cv(R.Field(g, "Gap")); l.GapY = Cv(R.Field(g, "GapY"));
            l.X0 = Cv(R.Field(g, "X0")); l.Y0 = Cv(R.Field(g, "Y0"));
            l.MaxGapX = Cv(R.Field(g, "MaxGapX")); l.MaxGapY = Cv(R.Field(g, "MaxGapY"));
            l.Cols = (int)R.Field(g, "Cols"); l.Rows = (int)R.Field(g, "Rows"); l.Used = (int)R.Field(g, "Used");
            l.PerPage = (int)R.Prop(g, "PerPage");
            return l;
        }
        static double Cv(object o) { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
        public override string ToString()
        {
            return "sayfa=" + PageW + "x" + PageH + " boyut=" + SizeW + "x" + SizeH + " sutun=" + Cols + " satir=" + Rows + " kullanilan=" + Used +
                   " aralik=" + Gap + "/" + GapY + " X0=" + X0 + " Y0=" + Y0;
        }
    }

    // ---------------------------------------------------------------- Polo Kart (MainForm) sürücüsü
    class Polo : IDisposable
    {
        public Form F;
        object cbSize, cbMode, nudW, nudH;
        public Polo(Assembly asm)
        {
            Type t = asm.GetType("PoloKart.MainForm", true);
            F = (Form)Activator.CreateInstance(t, true);
            F.StartPosition = FormStartPosition.Manual;
            F.Location = new Point(-5000, -5000);
            F.ShowInTaskbar = false;
            F.Show();
            Application.DoEvents();
            cbSize = R.Field(F, "cbSize"); cbMode = R.Field(F, "cbMode");
            nudW = R.Field(F, "nudW"); nudH = R.Field(F, "nudH");
        }
        public void Dispose() { F.Close(); F.Dispose(); }

        // 0: 9x9, 1: 8x8, 2: 7x7, 3: 6x6, 4: 5x5
        public void Preset(int idx) { R.SetProp(cbSize, "SelectedIndex", idx); }
        public void Custom(decimal w, decimal h)
        {
            R.SetProp(cbSize, "SelectedIndex", 5);
            R.SetProp(nudW, "Value", w);
            R.SetProp(nudH, "Value", h);
        }
        public void Mode(int m) { R.SetProp(cbMode, "SelectedIndex", m); }   // 0: çoklu, 1: tekli
        public void Opt(int n, float gap, bool even)
        {
            R.SetField(F, "poloN", n); R.SetField(F, "poloGap", gap); R.SetField(F, "poloEven", even);
        }
        public Lay Build(bool land, bool useCount) { return Lay.From(R.Call(F, "Build", land, useCount)); }
        public Lay Compute() { return Lay.From(R.Call(F, "ComputeLayout")); }
        public int Max() { return (int)R.Call(F, "MaxPerPage"); }
    }

    static class Program
    {
        static Assembly asm;
        static List<Result> results = new List<Result>();
        static Type tPanel, tKind, tPhoto;
        static string tmpDir;

        static void T(string group, string name, Action a)
        {
            Result r = new Result(); r.Group = group; r.Name = name;
            try { a(); r.Status = "GECTI"; }
            catch (AssertFail e) { r.Status = "KALDI"; r.Detail = e.Message; }
            catch (Exception e) { r.Status = "HATA"; r.Detail = e.GetType().Name + ": " + e.Message; }
            results.Add(r);
            Console.WriteLine("[" + r.Status + "] " + group + " / " + name + (r.Detail != null ? "  -> " + r.Detail : ""));
        }

        // Gözlem: başarısızlık sayılmaz; tasarım kararı olabilecek, ürün sahibine sorulacak durumları kaydeder.
        static void Observe(string group, string name, Func<string> check)
        {
            Result r = new Result(); r.Group = group; r.Name = name;
            try { string d = check(); if (d == null) r.Status = "GECTI"; else { r.Status = "GOZLEM"; r.Detail = d; } }
            catch (Exception e) { r.Status = "HATA"; r.Detail = e.GetType().Name + ": " + e.Message; }
            results.Add(r);
            Console.WriteLine("[" + r.Status + "] " + group + " / " + name + (r.Detail != null ? "  -> " + r.Detail : ""));
        }

        // ------------------------------------------------------------ referans (oracle) hesapları
        // Sayfa 10x15 cm. Ondalık aritmetik: kayan nokta hatası yok.
        static int OracleFit(decimal pageW, decimal pageH, decimal w, decimal h)
        {
            if (w <= 0 || h <= 0) return 0;
            return (int)Math.Floor(pageW / w) * (int)Math.Floor(pageH / h);
        }
        static int OraclePoloMax(decimal w, decimal h)
        {
            return Math.Max(OracleFit(10m, 15m, w, h), OracleFit(15m, 10m, w, h));
        }
        static int OraclePx(decimal cm, int dpi) { return (int)Math.Round(cm * dpi / 2.54m, MidpointRounding.AwayFromZero); }

        // ------------------------------------------------------------ değişmez kontrolü
        const double Eps = 0.002;
        static void CheckInside(Lay l, string ctx)
        {
            if (l.Cols == 0 || l.Rows == 0) return;
            double gw = l.Cols * l.SizeW + (l.Cols - 1) * l.Gap;
            double gh = l.Rows * l.SizeH + (l.Rows - 1) * l.GapY;
            A.True(l.X0 >= -Eps, "sol kenardan taşıyor: " + ctx + " :: " + l);
            A.True(l.Y0 >= -Eps, "üst kenardan taşıyor: " + ctx + " :: " + l);
            A.True(l.X0 + gw <= l.PageW + Eps, "sağ kenardan taşıyor: " + ctx + " :: " + l);
            A.True(l.Y0 + gh <= l.PageH + Eps, "alt kenardan taşıyor: " + ctx + " :: " + l);
            A.True(l.Gap >= -Eps && l.GapY >= -Eps, "negatif aralık: " + ctx + " :: " + l);
        }

        static decimal[] SquareSizes = new decimal[] { 1m, 1.5m, 2m, 2.5m, 3m, 3.3m, 3.5m, 4m, 4.5m, 5m, 5.5m, 6m, 7m, 7.5m, 8m, 9m, 10m, 12m, 15m };

        [STAThread]
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length < 1) { Console.WriteLine("Kullanım: SmQa.exe <StudioMate.exe yolu> [sonuc.txt]"); return 2; }
            Environment.SetEnvironmentVariable("SM_NOTOUR", "1");
            Application.EnableVisualStyles();
            asm = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            tPanel = asm.GetType("PoloKart.IdPanel", true);
            tKind = asm.GetType("PoloKart.IdKind", true);
            tPhoto = asm.GetType("PoloKart.Photo", true);
            tmpDir = Path.Combine(Path.GetTempPath(), "smqa_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);

            RunIdTests();
            RunIdObservations();
            RunPoloTests();
            RunPixelTests();

            try { Directory.Delete(tmpDir, true); } catch { }

            int pass = 0, fail = 0, err = 0, obs = 0;
            foreach (Result r in results) { if (r.Status == "GECTI") pass++; else if (r.Status == "KALDI") fail++; else if (r.Status == "GOZLEM") obs++; else err++; }
            string sum = "TOPLAM: " + results.Count + " test, " + pass + " geçti, " + fail + " kaldı, " + err + " hata, " + obs + " gözlem; " + A.Count + " doğrulama (assert) yapıldı.";
            Console.WriteLine(sum);
            if (args.Length > 1)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# StudioMate QA birim testi sonucu");
                sb.AppendLine("Tarih: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                sb.AppendLine("Test edilen: " + Path.GetFileName(args[0]) + "  (sürüm " + System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.GetFullPath(args[0])).FileVersion + ")");
                sb.AppendLine(sum);
                sb.AppendLine();
                foreach (Result r in results)
                    sb.AppendLine("[" + r.Status + "] " + r.Group + " / " + r.Name + (r.Detail != null ? "  -> " + r.Detail : ""));
                File.WriteAllText(args[1], sb.ToString(), new UTF8Encoding(true));
            }
            return (fail + err) == 0 ? 0 : 1;
        }

        // ============================================================ KİMLİK FOTOĞRAFI: kâğıt / ölçü
        static object Kind(int i) { return Enum.ToObject(tKind, i); }
        static readonly string[] KindNames = new string[] { "Vesikalık", "Biyometrik", "Schengen", "GreenCard", "Çin" };

        static void PhotoSize(int kind, out float w, out float h)
        {
            object[] a = new object[] { Kind(kind), 0f, 0f };
            tPanel.GetMethod("PhotoSizeOf", R.BF).Invoke(null, a);
            w = (float)a[1]; h = (float)a[2];
        }
        static object[] Builtins(int kind) { return (object[])tPanel.GetMethod("Builtins", R.BF).Invoke(null, new object[] { Kind(kind) }); }
        static object Custom(int kind, float w, float h, int n, float gap, bool even)
        {
            MethodInfo m = tPanel.GetMethod("Custom", R.BF, null,
                new Type[] { tKind, typeof(float), typeof(float), typeof(int), typeof(float), typeof(bool) }, null);
            return m.Invoke(null, new object[] { Kind(kind), w, h, n, gap, even });
        }

        static void RunIdTests()
        {
            // Beklenen fotoğraf ölçüleri (cm) - belge şartnamelerine göre
            decimal[][] expect = new decimal[][] {
                new decimal[] { 4.5m, 6m },   // Vesikalık 45x60
                new decimal[] { 5m, 6m },     // Biyometrik 50x60
                new decimal[] { 3.5m, 4.5m }, // Schengen 35x45
                new decimal[] { 5m, 5m },     // GreenCard 50x50
                new decimal[] { 3.3m, 4.8m }  // Çin vizesi 33x48
            };
            T("Belge ölçüleri", "5 belge türünün fotoğraf ölçüsü şartnameyle aynı", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    float w, h; PhotoSize(k, out w, out h);
                    A.Near((double)expect[k][0], w, 1e-4, KindNames[k] + " genişlik");
                    A.Near((double)expect[k][1], h, 1e-4, KindNames[k] + " yükseklik");
                }
            });

            T("Hazır kâğıtlar", "Her hazır kâğıtta fotoğraflar kâğıda sığıyor (taşma yok)", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    float pw, ph; PhotoSize(k, out pw, out ph);
                    foreach (object p in Builtins(k))
                    {
                        double W = Convert.ToDouble(R.Field(p, "W")), H = Convert.ToDouble(R.Field(p, "H"));
                        int c = (int)R.Field(p, "Cols"), r = (int)R.Field(p, "Rows");
                        double gy = Convert.ToDouble(R.Field(p, "GapY"));
                        A.True(c * pw <= W + Eps, KindNames[k] + ": sütunlar kâğıt genişliğini aşıyor");
                        A.True(r * ph + (r - 1) * gy <= H + Eps, KindNames[k] + ": satırlar kâğıt yüksekliğini aşıyor");
                    }
                }
            });

            T("Hazır kâğıtlar", "Hazır kâğıt adetleri (Vesikalık 4/6, Biyometrik 4/6, Schengen 4/6, GreenCard 4, Çin 4/6)", delegate
            {
                int[][] counts = new int[][] { new[] { 4, 6 }, new[] { 4, 6 }, new[] { 4, 6 }, new[] { 4 }, new[] { 4, 6 } };
                for (int k = 0; k < 5; k++)
                {
                    object[] b = Builtins(k);
                    A.Eq(counts[k].Length, b.Length, KindNames[k] + " hazır kâğıt sayısı");
                    for (int i = 0; i < b.Length; i++)
                        A.Eq(counts[k][i], (int)R.Prop(b[i], "Count"), KindNames[k] + " kâğıt " + (i + 1) + " adedi");
                }
            });

            T("Hazır kâğıtlar", "Biyometrik ve GreenCard bitişik, diğerleri aralıklı dizilir", delegate
            {
                bool[] touching = new bool[] { false, true, false, true, false };
                for (int k = 0; k < 5; k++)
                    foreach (object p in Builtins(k))
                    {
                        A.Eq(touching[k], (bool)R.Field(p, "Touching"), KindNames[k] + " bitişik bayrağı");
                        A.True((bool)R.Field(p, "Builtin"), KindNames[k] + " hazır kâğıt silinemez işaretli olmalı");
                    }
            });

            // ---- Özel kâğıt: sığan adet = referans hesap (kâğıt yatay/dikey çevrilerek en çok sığdıran)
            T("Özel kâğıt", "Sığan adet, ondalık referans hesapla birebir aynı (5 tür x 29x29 ölçü)", delegate
            {
                decimal[] dims = new decimal[] { 1m, 2m, 3m, 3.3m, 3.5m, 4m, 4.5m, 5m, 6m, 7m, 8m, 9m, 10m, 12m, 13m, 14m, 15m, 16m, 20m, 21m, 29.7m, 30m };
                int cases = 0;
                for (int k = 0; k < 5; k++)
                {
                    float pwf, phf; PhotoSize(k, out pwf, out phf);
                    decimal pw = (decimal)Math.Round(pwf, 2), ph = (decimal)Math.Round(phf, 2);
                    foreach (decimal w in dims)
                        foreach (decimal h in dims)
                        {
                            int exp = Math.Max(OracleFit(w, h, pw, ph), OracleFit(h, w, pw, ph));
                            object p = Custom(k, (float)w, (float)h, 0, -1f, false);
                            int got = p == null ? 0 : (int)R.Prop(p, "Count");
                            A.Eq(exp, got, KindNames[k] + " kâğıt " + w + "x" + h);
                            cases++;
                        }
                }
                A.True(cases > 2000, "yeterli durum denenmedi");
            });

            T("Özel kâğıt", "Hiçbir fotoğraf sığmıyorsa null döner (sığmama durumu)", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    A.True(Custom(k, 2f, 2f, 0, -1f, false) == null, KindNames[k] + ": 2x2 cm kâğıt null olmalı");
                    A.True(Custom(k, 3f, 4f, 0, -1f, false) == null, KindNames[k] + ": 3x4 cm kâğıt null olmalı");
                }
            });

            T("Özel kâğıt", "Tam sığma sınırı: ölçü kâğıda tam eşitse 1 adet sığar", delegate
            {
                float w, h;
                for (int k = 0; k < 5; k++)
                {
                    PhotoSize(k, out w, out h);
                    object p = Custom(k, w, h, 0, -1f, false);
                    A.True(p != null, KindNames[k] + ": kâğıt = fotoğraf ölçüsü ise sığmalı");
                    A.Eq(1, (int)R.Prop(p, "Count"), KindNames[k] + ": tam 1 adet");
                }
            });

            T("Özel kâğıt", "Adet 0, 1, tam max, max+1, çok büyük ve negatif", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    object full = Custom(k, 10f, 15f, 0, -1f, false);
                    int max = (int)R.Prop(full, "Count");
                    A.Eq(0, (int)R.Field(full, "Used"), KindNames[k] + ": adet 0 -> Used=0 (izgaranın tamamı)");
                    int[] ns = new int[] { 1, 2, max - 1, max, max + 1, 1000, -1, -50 };
                    foreach (int n in ns)
                    {
                        object p = Custom(k, 10f, 15f, n, -1f, false);
                        int exp = (n > 0 && n < max) ? n : max;
                        A.Eq(exp, (int)R.Prop(p, "Count"), KindNames[k] + " istenen adet " + n);
                        int cols = (int)R.Field(p, "Cols"), rows = (int)R.Field(p, "Rows");
                        A.True(cols * rows >= exp, KindNames[k] + " adet " + n + ": blok adedi karşılamıyor");
                    }
                }
            });

            T("Özel kâğıt", "Aralık: negatif = varsayılan, 0 = bitişik, çok büyük = en fazla sığan", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    float pw, ph; PhotoSize(k, out pw, out ph);
                    object neg = Custom(k, 10f, 15f, 0, -0.3f, false);
                    A.Near(-1.0, Convert.ToDouble(R.Field(neg, "Gap")), 1e-6, KindNames[k] + ": negatif aralık varsayılana düşmeli");
                    object zero = Custom(k, 10f, 15f, 0, 0f, false);
                    A.Near(0.0, Convert.ToDouble(R.Field(zero, "Gap")), 1e-6, KindNames[k] + ": 0 aralık");
                    object tiny = Custom(k, 10f, 15f, 0, 0.0001f, false);
                    double tg = Convert.ToDouble(R.Field(tiny, "Gap"));
                    A.True(tg >= 0 && tg <= 0.0002, KindNames[k] + ": çok küçük aralık korunmalı");
                    object huge = Custom(k, 10f, 15f, 0, 100f, false);
                    double hg = Convert.ToDouble(R.Field(huge, "Gap"));
                    double maxGap = Convert.ToDouble(R.Call(huge, "MaxGap", pw, ph));
                    A.Near(maxGap, hg, 1e-4, KindNames[k] + ": büyük aralık sığabilecek en büyük değere kırpılmalı");
                    A.True(hg <= 15.0, KindNames[k] + ": aralık kâğıttan büyük olamaz");
                }
            });

            T("Özel kâğıt", "Dengeli blok: adet seçilince sütun x satır adedi karşılar, ızgarayı aşmaz, boş satır kalmaz", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    object full = Custom(k, 21f, 29.7f, 0, -1f, false);
                    int mc = (int)R.Field(full, "Cols"), mr = (int)R.Field(full, "Rows");
                    for (int n = 1; n < mc * mr; n++)
                    {
                        object p = Custom(k, 21f, 29.7f, n, -1f, false);
                        int c = (int)R.Field(p, "Cols"), r = (int)R.Field(p, "Rows");
                        A.True(c <= mc && r <= mr, KindNames[k] + " n=" + n + ": blok ızgarayı aşıyor");
                        A.True(c * r >= n, KindNames[k] + " n=" + n + ": blok adedi karşılamıyor");
                        A.True(c * r - n < c, KindNames[k] + " n=" + n + ": tamamen boş satır var (" + c + "x" + r + ")");
                    }
                }
            });
        }

        // Gözlem: dengeli blok seçimi "en az boş hücre" kuralını her zaman uygulamıyor mu?
        static void RunIdObservations()
        {
            Observe("Gözlem", "Özel kâğıt: dengeli blok, mümkün olan en az boş hücreyi bırakıyor mu?", delegate
            {
                List<string> viol = new List<string>();
                float[][] papers = new float[][] { new float[] { 21f, 29.7f }, new float[] { 30f, 40f }, new float[] { 50f, 70f } };
                for (int k = 0; k < 5; k++)
                    foreach (float[] pp in papers)
                    {
                        object full = Custom(k, pp[0], pp[1], 0, -1f, false);
                        if (full == null) continue;
                        int mc = (int)R.Field(full, "Cols"), mr = (int)R.Field(full, "Rows");
                        for (int n = 1; n < mc * mr; n++)
                        {
                            object b = Custom(k, pp[0], pp[1], n, -1f, false);
                            int waste = (int)R.Field(b, "Cols") * (int)R.Field(b, "Rows") - n;
                            int min = int.MaxValue;
                            for (int c = 1; c <= mc; c++)
                            {
                                int r = (n + c - 1) / c;
                                if (r <= mr && c * r - n < min) min = c * r - n;
                            }
                            if (waste > min) viol.Add(KindNames[k] + " " + pp[0] + "x" + pp[1] + " n=" + n + ": " + R.Field(b, "Cols") + "x" + R.Field(b, "Rows") + " (boş " + waste + "), olabilecek en az boş " + min);
                        }
                    }
                return viol.Count == 0 ? null : viol.Count + " durumda mümkün olandan fazla boş hücre; örn. " + viol[0];
            });
        }

        // ============================================================ POLO KART yerleşimi
        static void RunPoloTests()
        {
            Polo p = null;
            try
            {
                p = new Polo(asm);

                T("Polo Kart", "Çoklu mod: sığdığı kadar adet, ondalık referans hesapla birebir aynı (19x19 ölçü)", delegate
                {
                    int cases = 0;
                    foreach (decimal w in SquareSizes)
                        foreach (decimal h in SquareSizes)
                        {
                            p.Custom(w, h); p.Mode(0); p.Opt(0, -1f, false);
                            int exp = OraclePoloMax(w, h);
                            A.Eq(exp, p.Max(), "MaxPerPage " + w + "x" + h);
                            cases++;
                        }
                    A.True(cases == SquareSizes.Length * SquareSizes.Length, "durum sayısı");
                });

                T("Polo Kart", "Hazır ölçüler (9x9 ve 8x8 tek fotoğraf, 7x7, 6x6, 5x5) sayfa başı adet", delegate
                {
                    int[] exp = new int[] { 1, 1, OraclePoloMax(7m, 7m), OraclePoloMax(6m, 6m), OraclePoloMax(5m, 5m) };
                    for (int i = 0; i < 5; i++)
                    {
                        p.Preset(i); p.Mode(0); p.Opt(0, -1f, false);
                        A.Eq(exp[i], p.Max(), "hazır ölçü indeksi " + i);
                    }
                });

                T("Polo Kart", "Sayfaya sığmayan ölçü: 0 adet, çökmeden", delegate
                {
                    p.Mode(0); p.Opt(0, -1f, false);
                    p.Custom(15m, 15m);
                    A.Eq(0, p.Max(), "15x15 hiçbir yönde sığmaz");
                    A.Eq(0, p.Compute().PerPage, "ComputeLayout 15x15");
                    p.Custom(11m, 11m);
                    A.Eq(0, p.Max(), "11x11 sığmaz");
                    p.Custom(10m, 15m);
                    A.Eq(1, p.Max(), "10x15 tam sığar (1 adet)");
                    p.Custom(15m, 10m);
                    A.Eq(1, p.Max(), "15x10 yatay tam sığar (1 adet)");
                });

                T("Polo Kart", "Tüm yerleşimler sayfa içinde kalır ve aralıklar negatif olmaz (ölçü x mod x adet x aralık x eşit-yay)", delegate
                {
                    float[] gaps = new float[] { -1f, 0f, 0.0001f, 0.05f, 0.1f, 0.5f, 1f, 3f, 100f, -0.5f };
                    int cases = 0;
                    foreach (decimal w in SquareSizes)
                        foreach (decimal h in new decimal[] { 1m, 2m, 3.3m, 4.8m, 5m, 6m, 7m, 15m })
                            for (int mode = 0; mode < 2; mode++)
                            {
                                p.Custom(w, h); p.Mode(mode);
                                int max = p.Max();
                                int[] ns = new int[] { 0, 1, 2, Math.Max(1, max - 1), max, max + 1, 500, -3 };
                                foreach (int n in ns)
                                    foreach (float g in gaps)
                                        foreach (bool even in new bool[] { false, true })
                                        {
                                            p.Opt(n, g, even);
                                            Lay l = p.Compute();
                                            string ctx = w + "x" + h + " mod=" + mode + " n=" + n + " aralık=" + g + " eşit=" + even;
                                            CheckInside(l, ctx);
                                            cases++;
                                        }
                            }
                    A.True(cases > 5000, "yeterli durum denenmedi: " + cases);
                });

                T("Polo Kart", "Adet seçimi: 0, 1, max-1, max, max+1 -> sayfadaki adet = min(n, max); adet 0 = sığdığı kadar", delegate
                {
                    foreach (decimal w in new decimal[] { 2m, 3.3m, 4.5m, 5m, 6m })
                    {
                        p.Custom(w, w); p.Mode(0); p.Opt(0, -1f, false);
                        int max = p.Max();
                        foreach (int n in new int[] { 0, 1, 2, max - 1, max, max + 1, 999, -1 })
                        {
                            p.Opt(n, -1f, false);
                            Lay l = p.Compute();
                            int exp = (n > 0 && n < max) ? n : max;
                            A.Eq(exp, l.PerPage, w + "x" + w + " istenen adet " + n);
                            A.True(l.Cols * l.Rows >= l.PerPage, "blok adedi karşılamıyor " + l);
                        }
                    }
                });

                T("Polo Kart", "Tekli mod: her ölçüde sayfada 1 fotoğraf, yatayda ortalı", delegate
                {
                    foreach (decimal w in new decimal[] { 2m, 4.5m, 6m, 9m, 10m })
                    {
                        p.Custom(w, w); p.Mode(1); p.Opt(0, -1f, false);
                        Lay l = p.Compute();
                        A.Eq(1, l.PerPage, w + " tekli adet");
                        A.Eq(1, l.Cols, w + " tekli sütun"); A.Eq(1, l.Rows, w + " tekli satır");
                        A.Near((l.PageW - l.SizeW) / 2.0, l.X0, Eps, w + " tekli yatay ortalama");
                        CheckInside(l, "tekli " + w);
                    }
                });

                T("Polo Kart", "Elle aralık: istenen değer, o yönde sığabilecek en büyük değerle sınırlanır; grup ortalı", delegate
                {
                    foreach (decimal w in new decimal[] { 2m, 3m, 4m })
                    {
                        p.Custom(w, w); p.Mode(0);
                        foreach (float g in new float[] { 0f, 0.0001f, 0.2f, 0.5f, 2f, 50f })
                        {
                            p.Opt(0, g, false);
                            Lay l = p.Compute();
                            A.Near(Math.Min(g, l.MaxGapX), l.Gap, Eps, w + " yatay aralık g=" + g);
                            A.Near(Math.Min(g, l.MaxGapY), l.GapY, Eps, w + " dikey aralık g=" + g);
                            double gw = l.Cols * l.SizeW + (l.Cols - 1) * l.Gap;
                            double gh = l.Rows * l.SizeH + (l.Rows - 1) * l.GapY;
                            A.Near((l.PageW - gw) / 2.0, l.X0, Eps, w + " grup yatay ortalı g=" + g);
                            A.Near((l.PageH - gh) / 2.0, l.Y0, Eps, w + " grup dikey ortalı g=" + g);
                        }
                    }
                });

                T("Polo Kart", "Eşit yay: kenar boşlukları = fotoğraflar arası boşluk (yatay ve dikey ayrı ayrı)", delegate
                {
                    foreach (decimal w in new decimal[] { 2m, 3m, 4m, 4.5m })
                    {
                        p.Custom(w, w); p.Mode(0);
                        foreach (int n in new int[] { 0, 1, 2, 3, 4 })
                        {
                            p.Opt(n, -1f, true);
                            Lay l = p.Compute();
                            if (l.Cols == 0 || l.Rows == 0) continue;
                            double rightEdge = l.PageW - (l.X0 + l.Cols * l.SizeW + (l.Cols - 1) * l.Gap);
                            double bottomEdge = l.PageH - (l.Y0 + l.Rows * l.SizeH + (l.Rows - 1) * l.GapY);
                            A.Near(l.Gap, l.X0, Eps, w + " n=" + n + " sol kenar = aralık");
                            A.Near(l.Gap, rightEdge, Eps, w + " n=" + n + " sağ kenar = aralık");
                            A.Near(l.GapY, l.Y0, Eps, w + " n=" + n + " üst kenar = aralık");
                            A.Near(l.GapY, bottomEdge, Eps, w + " n=" + n + " alt kenar = aralık");
                        }
                    }
                });

                T("Polo Kart", "Kâğıt yönü: yatayda daha çok sığıyorsa yatay seçilir, eşitse dikey", delegate
                {
                    foreach (decimal w in SquareSizes)
                        foreach (decimal h in SquareSizes)
                        {
                            p.Custom(w, h); p.Mode(0); p.Opt(0, -1f, false);
                            int port = OracleFit(10m, 15m, w, h), land = OracleFit(15m, 10m, w, h);
                            if (port + land == 0) continue;
                            Lay l = p.Compute();
                            bool expLand = land > port;
                            A.Eq(expLand, l.PageW > l.PageH, "yön " + w + "x" + h + " (dikey " + port + ", yatay " + land + ")");
                        }
                });

                T("Polo Kart", "Aralık 0 (bitişik) ve varsayılan aralık: ikisi de sayfa içinde, bitişikte aralık tam 0", delegate
                {
                    p.Custom(3m, 3m); p.Mode(0);
                    p.Opt(0, -1f, false); Lay def = p.Compute();
                    p.Opt(0, 0f, false); Lay zero = p.Compute();
                    A.True(def.Gap >= 0, "varsayılan aralık negatif");
                    A.Near(0.0, zero.Gap, Eps, "aralık 0 -> bitişik");
                    CheckInside(def, "varsayılan"); CheckInside(zero, "bitişik");
                });
            }
            finally { if (p != null) p.Dispose(); }
        }

        // ============================================================ mm -> piksel (300 / 600 / 700 dpi)
        static void RunPixelTests()
        {
            T("Piksel dönüşümü", "Hazır kâğıtların çıktı boyutu = cm x dpi / 2,54 (300, 600, 700 dpi)", delegate
            {
                for (int k = 0; k < 5; k++)
                {
                    object[] b = Builtins(k);
                    foreach (object lay in b)
                    {
                        object panel = Activator.CreateInstance(tPanel, Kind(k));
                        tPanel.GetMethod("SetLayout", R.BF).Invoke(panel, new object[] { lay });
                        decimal W = (decimal)Math.Round(Convert.ToDouble(R.Field(lay, "W")), 3);
                        decimal H = (decimal)Math.Round(Convert.ToDouble(R.Field(lay, "H")), 3);
                        foreach (int dpi in new int[] { 300, 600, 700 })
                        {
                            using (Bitmap bmp = (Bitmap)tPanel.GetMethod("Render").Invoke(panel, new object[] { 0, dpi }))
                            {
                                A.Eq(OraclePx(W, dpi), bmp.Width, KindNames[k] + " " + W + "x" + H + " @" + dpi + " genişlik px");
                                A.Eq(OraclePx(H, dpi), bmp.Height, KindNames[k] + " " + W + "x" + H + " @" + dpi + " yükseklik px");
                                A.Near(dpi, bmp.HorizontalResolution, 0.5, "çözünürlük meta verisi @" + dpi);
                            }
                        }
                        ((IDisposable)panel).Dispose();
                    }
                }
            });

            T("Piksel dönüşümü", "Bilinen referans değerler: 10x15 cm @300 dpi = 1181x1772 px", delegate
            {
                A.Eq(1181, OraclePx(10m, 300), "10 cm @300 (referans)");
                A.Eq(1772, OraclePx(15m, 300), "15 cm @300 (referans)");
                object panel = Activator.CreateInstance(tPanel, Kind(0));
                using (Bitmap bmp = (Bitmap)tPanel.GetMethod("Render").Invoke(panel, new object[] { 0, 300 }))
                {
                    A.Eq(1181, bmp.Width, "vesikalık 10x15 @300 genişlik");
                    A.Eq(1772, bmp.Height, "vesikalık 10x15 @300 yükseklik");
                }
                ((IDisposable)panel).Dispose();
            });

            T("Piksel dönüşümü", "Özel kâğıt boyutlarında (0,1 cm adımlı örnekler) yuvarlama referansla aynı: en çok ±0 piksel", delegate
            {
                decimal[] sizes = new decimal[] { 7.6m, 10.1m, 12.7m, 13m, 14.8m, 15.2m, 17.8m, 20.3m, 21m, 29.7m };
                foreach (decimal s in sizes)
                {
                    object panel = Activator.CreateInstance(tPanel, Kind(0));
                    object lay = Custom(0, (float)s, (float)s, 0, -1f, false);
                    if (lay == null) { ((IDisposable)panel).Dispose(); continue; }
                    tPanel.GetMethod("SetLayout", R.BF).Invoke(panel, new object[] { lay });
                    decimal W = (decimal)Math.Round(Convert.ToDouble(R.Field(lay, "W")), 3);
                    decimal H = (decimal)Math.Round(Convert.ToDouble(R.Field(lay, "H")), 3);
                    foreach (int dpi in new int[] { 300, 600 })
                    {
                        using (Bitmap bmp = (Bitmap)tPanel.GetMethod("Render").Invoke(panel, new object[] { 0, dpi }))
                        {
                            A.Eq(OraclePx(W, dpi), bmp.Width, s + " cm kâğıt @" + dpi + " genişlik");
                            A.Eq(OraclePx(H, dpi), bmp.Height, s + " cm kâğıt @" + dpi + " yükseklik");
                        }
                    }
                    ((IDisposable)panel).Dispose();
                }
            });

            T("Piksel dönüşümü", "Tek fotoğraf çıktısı (RenderSingle): her belge türü ve dpi'da ölçü = referans (sentetik görsel)", delegate
            {
                string png = Path.Combine(tmpDir, "sentetik.png");
                using (Bitmap b = new Bitmap(600, 800))
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.Clear(Color.White);
                    using (System.Drawing.Drawing2D.LinearGradientBrush br = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 600, 800), Color.SteelBlue, Color.Khaki, 90f))
                        g.FillRectangle(br, 0, 0, 600, 800);
                    g.FillEllipse(Brushes.Peru, 200, 150, 200, 260);
                    b.Save(png, ImageFormat.Png);
                }
                for (int k = 0; k < 5; k++)
                {
                    object panel = Activator.CreateInstance(tPanel, Kind(k));
                    object photo = Activator.CreateInstance(tPhoto, true);
                    R.SetField(photo, "Path", png);
                    ((System.Collections.IList)R.Field(panel, "Photos")).Add(photo);
                    float fw, fh; PhotoSize(k, out fw, out fh);
                    decimal w = (decimal)Math.Round(fw, 2), h = (decimal)Math.Round(fh, 2);
                    foreach (int dpi in new int[] { 300, 600, 700 })
                    {
                        using (Bitmap bmp = (Bitmap)tPanel.GetMethod("RenderSingle").Invoke(panel, new object[] { 0, dpi }))
                        {
                            A.Eq(OraclePx(w, dpi), bmp.Width, KindNames[k] + " tek foto @" + dpi + " genişlik");
                            A.Eq(OraclePx(h, dpi), bmp.Height, KindNames[k] + " tek foto @" + dpi + " yükseklik");
                        }
                    }
                    ((IDisposable)panel).Dispose();
                }
            });
        }
    }
}
