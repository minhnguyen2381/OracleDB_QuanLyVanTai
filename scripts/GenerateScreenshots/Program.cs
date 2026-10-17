using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace GenerateScreenshots;

public static class Program
{
    private static readonly Color Navy = ColorTranslator.FromHtml("#0B3D91");
    private static readonly Color NavyDark = ColorTranslator.FromHtml("#07275E");
    private static readonly Color NavyHover = ColorTranslator.FromHtml("#164BA2");
    private static readonly Color Gold = ColorTranslator.FromHtml("#F5B700");
    private static readonly Color BgApp = ColorTranslator.FromHtml("#F4F6F9");
    private static readonly Color BorderCol = ColorTranslator.FromHtml("#E2E8F0");
    private static readonly Color TextDark = ColorTranslator.FromHtml("#0F172A");
    private static readonly Color TextMuted = ColorTranslator.FromHtml("#64748B");
    private static readonly Color Green = ColorTranslator.FromHtml("#10B981");
    private static readonly Color Red = ColorTranslator.FromHtml("#EF4444");
    private static readonly Color Blue = ColorTranslator.FromHtml("#2563EB");
    private static readonly Color Yellow = ColorTranslator.FromHtml("#F59E0B");
    private static readonly Color YellowBg = ColorTranslator.FromHtml("#FEF3C7");
    private static readonly Color YellowBorder = ColorTranslator.FromHtml("#FCD34D");
    private static readonly Color GreenBg = ColorTranslator.FromHtml("#DCFCE7");
    private static readonly Color GreenBorder = ColorTranslator.FromHtml("#86EFAC");

    private static readonly Font FontTitle = new("Segoe UI", 9f);
    private static readonly Font FontHdr = new("Segoe UI", 15f, FontStyle.Bold);
    private static readonly Font FontMenu = new("Segoe UI", 10f);
    private static readonly Font FontMenuB = new("Segoe UI", 10f, FontStyle.Bold);
    private static readonly Font FontBrand = new("Segoe UI", 20f, FontStyle.Bold);
    private static readonly Font FontSub = new("Segoe UI", 8.5f);
    private static readonly Font FontCardT = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font FontCardVal = new("Segoe UI", 18f, FontStyle.Bold);
    private static readonly Font FontGridHdr = new("Segoe UI", 9f, FontStyle.Bold);
    private static readonly Font FontGridCell = new("Segoe UI", 9f);
    private static readonly Font FontGridCellB = new("Segoe UI", 9f, FontStyle.Bold);
    private static readonly Font FontSmall = new("Segoe UI", 8.5f);
    private static readonly Font FontSmallB = new("Segoe UI", 8.5f, FontStyle.Bold);
    private static readonly Font FontBtn = new("Segoe UI", 9.5f, FontStyle.Bold);
    private static readonly Font FontSymbol = new("Segoe MDL2 Assets", 11f);
    private static readonly Font FontSymbolSm = new("Segoe MDL2 Assets", 9f);

    private static readonly Brush BNavy = new SolidBrush(Navy);
    private static readonly Brush BNavyDark = new SolidBrush(NavyDark);
    private static readonly Brush BNavyHover = new SolidBrush(NavyHover);
    private static readonly Brush BGold = new SolidBrush(Gold);
    private static readonly Brush BWhite = Brushes.White;
    private static readonly Brush BBg = new SolidBrush(BgApp);
    private static readonly Brush BDark = new SolidBrush(TextDark);
    private static readonly Brush BMuted = new SolidBrush(TextMuted);
    private static readonly Pen PBorder = new(BorderCol, 1f);

    private static readonly StringFormat SfCenter = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    public static void Main(string[] args)
    {
        var dir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, "database")) && Directory.GetParent(dir) != null)
        {
            dir = Directory.GetParent(dir)!.FullName;
        }
        var outDir = Path.Combine(dir, "docs", "images");
        if (args.Length > 0) outDir = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(outDir);

        Console.WriteLine($"Generating screenshots to {outDir}...");

        Gen01Login(outDir);
        Gen02Dashboard(outDir);
        Gen03SellSeatmap(outDir);
        Gen04SellPayment(outDir);
        Gen05Tickets(outDir);
        Gen06Passengers(outDir);
        Gen07Routes(outDir);
        Gen08Vehicles(outDir);
        Gen09Drivers(outDir);
        Gen10Schedules(outDir);
        Gen11Statistics(outDir);
        Gen12Staff(outDir);
        Gen13Backup(outDir);
        Gen14English(outDir);
        Gen15Excel(outDir);

        Console.WriteLine("All 15 screenshots generated successfully!");
    }

    private static (Bitmap Bmp, Graphics G) NewCanvas(int w, int h)
    {
        var bmp = new Bitmap(w, h);
        var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        return (bmp, g);
    }

    private static void SaveCanvas(Bitmap bmp, Graphics g, string outDir, string name)
    {
        var path = Path.Combine(outDir, $"{name}.png");
        bmp.Save(path, ImageFormat.Png);
        g.Dispose();
        bmp.Dispose();
        Console.WriteLine($"Saved {name}.png");
    }

    private static void DrawWindowBase(Graphics g, int w, int h, int activeIdx, string pageTitle, bool isEng = false, string user = "Nguyễn Văn Điều Hành", string role = "Điều hành (ROLE_OPERATOR)")
    {
        g.FillRectangle(BBg, 0, 0, w, h);

        // Title bar
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0")))
            g.FillRectangle(b, 0, 0, w, 32);

        var titleText = isEng ? "Transport Management & Ticket Booking - HaUI" : "Quản lý vận tải và đặt vé xe - HaUI";
        g.DrawString(titleText, FontTitle, BDark, 12, 8);
        g.DrawString("\uE921", FontSymbolSm, BDark, w - 90, 10);
        g.DrawString("\uE922", FontSymbolSm, BDark, w - 60, 10);
        g.DrawString("\uE8BB", FontSymbolSm, BDark, w - 30, 10);

        // Sidebar
        g.FillRectangle(BNavy, 0, 32, 230, h - 32);
        g.DrawString("HAUI", FontBrand, BWhite, 20, 46);
        g.FillRectangle(BGold, 20, 80, 42, 3);
        var subBrand = isEng ? "Transport & Ticketing System" : "Quản lý vận tải & bán vé xe";
        using (var b = new SolidBrush(Color.FromArgb(204, 255, 255, 255)))
            g.DrawString(subBrand, FontSub, b, 20, 88);

        var menusVi = new[] { "Tổng quan", "Bán vé", "Quản lý vé", "Hành khách", "Tuyến xe", "Phương tiện", "Tài xế", "Lịch chạy", "Thống kê", "Nhân viên", "Sao lưu" };
        var menusEn = new[] { "Dashboard", "Sell Tickets", "Tickets", "Passengers", "Routes", "Vehicles", "Drivers", "Schedules", "Statistics", "Staff", "Backup" };
        var menus = isEng ? menusEn : menusVi;
        var glyphs = new[] { "\uE80F", "\uE7BF", "\uE8A5", "\uE716", "\uE707", "\uE806", "\uE77B", "\uE787", "\uE9D2", "\uE7EF", "\uE74E" };

        var ym = 125;
        for (int i = 0; i < menus.Length; i++)
        {
            if (i == activeIdx)
            {
                g.FillRectangle(BNavyHover, 0, ym, 230, 38);
                g.FillRectangle(BGold, 0, ym, 4, 38);
                g.DrawString(glyphs[i], FontSymbol, BWhite, 18, ym + 11);
                g.DrawString(menus[i], FontMenuB, BWhite, 46, ym + 9);
            }
            else
            {
                using var b = new SolidBrush(Color.FromArgb(215, 225, 245));
                g.DrawString(glyphs[i], FontSymbol, b, 18, ym + 11);
                g.DrawString(menus[i], FontMenu, b, 46, ym + 9);
            }
            ym += 42;
        }

        // Logout button
        var logoutText = isEng ? "Sign out" : "Đăng xuất";
        g.FillRectangle(BNavyDark, 0, h - 46, 230, 46);
        g.DrawString("\uE7E8", FontSymbol, BWhite, 18, h - 30);
        g.DrawString(logoutText, FontMenu, BWhite, 46, h - 32);

        // Top Header
        g.FillRectangle(BWhite, 230, 32, w - 230, 56);
        g.DrawLine(PBorder, 230, 88, w, 88);
        g.DrawString(pageTitle, FontHdr, BDark, 254, 46);

        // User info
        g.DrawString(user, FontMenuB, BDark, w - 380, 42);
        g.DrawString(role, FontSmall, BNavy, w - 380, 62);

        // Lang button
        var langBtnText = isEng ? "English" : "Tiếng Việt";
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#F1F5F9")))
            g.FillRectangle(b, w - 135, 43, 110, 32);
        g.DrawRectangle(PBorder, w - 135, 43, 110, 32);
        g.DrawString("\uE774", FontSymbolSm, BDark, w - 124, 52);
        g.DrawString(langBtnText, FontSmallB, BDark, w - 104, 51);
    }

    private static void DrawGridTable(Graphics g, int x, int y, int w, int h, string[] cols, int[] widths, string[][] rows, int selectedIdx = -1)
    {
        g.FillRectangle(BWhite, x, y, w, h);
        g.DrawRectangle(PBorder, x, y, w, h);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#F1F5F9")))
            g.FillRectangle(b, x, y, w, 32);
        g.DrawLine(PBorder, x, y + 32, x + w, y + 32);

        int curX = x + 12;
        for (int c = 0; c < cols.Length; c++)
        {
            g.DrawString(cols[c], FontGridHdr, BDark, curX, y + 8);
            curX += widths[c];
        }

        int rowY = y + 34;
        for (int r = 0; r < rows.Length; r++)
        {
            if (rowY + 28 > y + h) break;
            var rowData = rows[r];

            if (r == selectedIdx)
            {
                using var b = new SolidBrush(ColorTranslator.FromHtml("#EFF6FF"));
                g.FillRectangle(b, x + 1, rowY, w - 2, 28);
                g.FillRectangle(BNavy, x + 1, rowY, 3, 28);
            }

            curX = x + 12;
            for (int c = 0; c < rowData.Length; c++)
            {
                var val = rowData[c];
                Brush cellBrush = (r == selectedIdx) ? BNavy : BDark;

                if (val.StartsWith("ACTIVE") || val.StartsWith("Đã thanh toán") || val.StartsWith("Hoạt động") || val.StartsWith("Thành công") || val.StartsWith("PAID") || val.StartsWith("Ready"))
                {
                    cellBrush = new SolidBrush(Green);
                }
                else if (val.StartsWith("CANCELLED") || val.StartsWith("Hết hạn") || val.StartsWith("Đã hủy") || val.StartsWith("Khóa") || val.StartsWith("Sold Out"))
                {
                    cellBrush = new SolidBrush(Red);
                }
                else if (val.StartsWith("BOOKED") || val.StartsWith("Chờ thanh toán") || val.StartsWith("Sắp hết hạn") || val.StartsWith("Bảo trì") || val.StartsWith("Boarding"))
                {
                    cellBrush = new SolidBrush(Yellow);
                }

                g.DrawString(val, FontGridCell, cellBrush, curX, rowY + 6);
                curX += widths[c];
            }
            g.DrawLine(PBorder, x, rowY + 28, x + w, rowY + 28);
            rowY += 28;
        }
    }

    private static void DrawFormPanel(Graphics g, int x, int y, int w, int h, string title, (string Label, string Value, bool Disabled)[] fields)
    {
        g.FillRectangle(BWhite, x, y, w, h);
        g.DrawRectangle(PBorder, x, y, w, h);

        g.DrawString(title, FontCardT, BNavy, x + 16, y + 14);
        g.DrawLine(PBorder, x + 16, y + 40, x + w - 16, y + 40);

        int fy = y + 50;
        foreach (var field in fields)
        {
            g.DrawString(field.Label, FontSmallB, BDark, x + 16, fy);
            fy += 18;
            using (var b = new SolidBrush(ColorTranslator.FromHtml("#F8FAFC")))
                g.FillRectangle(b, x + 16, fy, w - 32, 30);
            g.DrawRectangle(PBorder, x + 16, fy, w - 32, 30);
            Brush valBrush = field.Disabled ? BMuted : BDark;
            g.DrawString(field.Value, FontGridCell, valBrush, x + 24, fy + 6);
            fy += 38;
        }

        int by = y + h - 48;
        g.FillRectangle(BNavy, x + 16, by, 100, 32);
        g.DrawString("LƯU", FontBtn, BWhite, new RectangleF(x + 16, by, 100, 32), SfCenter);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0")))
            g.FillRectangle(b, x + 126, by, 90, 32);
        g.DrawString("HỦY", FontBtn, BDark, new RectangleF(x + 126, by, 90, 32), SfCenter);
    }

    // --- 01 LOGIN ---
    private static void Gen01Login(string outDir)
    {
        var (bmp, g) = NewCanvas(900, 540);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0")))
            g.FillRectangle(b, 0, 0, 900, 32);
        g.DrawString("Quản lý vận tải và đặt vé xe", FontTitle, BDark, 12, 8);
        g.DrawString("\uE921", FontSymbolSm, BDark, 810, 10);
        g.DrawString("\uE922", FontSymbolSm, BDark, 840, 10);
        g.DrawString("\uE8BB", FontSymbolSm, BDark, 870, 10);

        // Left brand
        g.FillRectangle(BNavy, 0, 32, 380, 508);
        using var fHuge = new Font("Segoe UI", 36f, FontStyle.Bold);
        g.DrawString("HAUI", fHuge, BWhite, 40, 140);
        g.FillRectangle(BGold, 42, 205, 64, 4);
        using var fAppT = new Font("Segoe UI", 16f, FontStyle.Bold);
        g.DrawString("Quản lý vận tải\nvà đặt vé xe", fAppT, BWhite, 40, 220);
        using var fHint = new Font("Segoe UI", 10f);
        using (var b = new SolidBrush(Color.FromArgb(204, 255, 255, 255)))
            g.DrawString("Đăng nhập bằng tài khoản Oracle do quản trị viên cấp để truy cập hệ thống.", fHint, b, new RectangleF(40, 285, 300, 100));

        // Right form
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#F8F9FA")))
            g.FillRectangle(b, 380, 32, 520, 508);
        using var fTitleMain = new Font("Segoe UI", 20f, FontStyle.Bold);
        g.DrawString("Đăng nhập hệ thống", fTitleMain, BNavy, 440, 90);

        g.DrawString("Tên đăng nhập", FontSmallB, BDark, 440, 145);
        g.FillRectangle(BWhite, 440, 170, 400, 36);
        using (var penFocus = new Pen(Navy, 1.5f))
            g.DrawRectangle(penFocus, 440, 170, 400, 36);
        using var fInput = new Font("Segoe UI", 11f);
        g.DrawString("operator01", fInput, BDark, 450, 177);

        g.DrawString("Mật khẩu", FontSmallB, BDark, 440, 220);
        g.FillRectangle(BWhite, 440, 245, 400, 36);
        g.DrawRectangle(PBorder, 440, 245, 400, 36);
        g.DrawString("••••••••••", fInput, BDark, 450, 252);

        g.DrawString("Ngôn ngữ", FontSmallB, BDark, 440, 295);
        g.FillRectangle(BWhite, 440, 320, 400, 36);
        g.DrawRectangle(PBorder, 440, 320, 400, 36);
        g.DrawString("Tiếng Việt (vi-VN)", fInput, BDark, 450, 327);
        g.DrawString("▼", FontTitle, BDark, 815, 331);

        g.FillRectangle(BNavy, 440, 385, 400, 42);
        g.DrawString("ĐĂNG NHẬP", FontBtn, BWhite, new RectangleF(440, 385, 400, 42), SfCenter);

        SaveCanvas(bmp, g, outDir, "01-login");
    }

    // --- 02 DASHBOARD ---
    private static void Gen02Dashboard(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 0, "Tổng quan");

        var cards = new[]
        {
            (Lbl: "Chuyến xe hôm nay", Val: "8 chuyến", Sub: "4 chuyến đang mở bán", Color: "#0B3D91"),
            (Lbl: "Vé đã bán hôm nay", Val: "142 vé", Sub: "Tăng 12% so với hôm qua", Color: "#10B981"),
            (Lbl: "Doanh thu thực hôm nay", Val: "24.650.000 ₫", Sub: "Đã trừ 0 ₫ hoàn tiền", Color: "#F59E0B"),
            (Lbl: "Tỷ lệ lấp đầy TB", Val: "78.5%", Sub: "TB 32/40 ghế mỗi chuyến", Color: "#6366F1")
        };

        for (int i = 0; i < 4; i++)
        {
            int cx = 254 + (i * 272);
            var cd = cards[i];
            g.FillRectangle(BWhite, cx, 106, 256, 92);
            g.DrawRectangle(PBorder, cx, 106, 256, 92);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(cd.Color)))
                g.FillRectangle(b, cx, 106, 4, 92);
            g.DrawString(cd.Lbl, FontSmall, BMuted, cx + 16, 118);
            g.DrawString(cd.Val, FontCardVal, BDark, cx + 16, 136);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(cd.Color)))
                g.DrawString(cd.Sub, FontSmall, b, cx + 16, 170);
        }

        // Chart Card
        g.FillRectangle(BWhite, 254, 214, 610, 330);
        g.DrawRectangle(PBorder, 254, 214, 610, 330);
        g.DrawString("Doanh thu thực 7 ngày gần nhất (nghìn đồng)", FontCardT, BDark, 274, 228);
        g.DrawLine(PBorder, 274, 254, 844, 254);
        var days = new[] { "12/10", "13/10", "14/10", "15/10", "16/10", "17/10", "18/10" };
        var revs = new[] { 18500, 21200, 19800, 26400, 24100, 29800, 24650 };
        for (int d = 0; d < 7; d++)
        {
            int bx = 295 + (d * 78);
            int bh = (int)((revs[d] / 35000f) * 190);
            int by = 500 - bh;
            Brush bBrush = (d == 6) ? BGold : BNavy;
            g.FillRectangle(bBrush, bx, by, 44, bh);
            g.DrawString($"{Math.Round(revs[d] / 1000.0, 1)}tr", FontSmallB, BDark, bx + 6, by - 18);
            g.DrawString(days[d], FontSmall, BMuted, bx + 8, 506);
        }

        // Upcoming Trips
        g.FillRectangle(BWhite, 880, 214, 456, 330);
        g.DrawRectangle(PBorder, 880, 214, 456, 330);
        g.DrawString("Chuyến xe sắp khởi hành hôm nay", FontCardT, BDark, 900, 228);
        g.DrawLine(PBorder, 900, 254, 1316, 254);
        var nextTrips = new[]
        {
            (R: "Hà Nội → Hải Phòng", I: "08:30 | 29B-196.88", S: "38/40", St: "Sắp chạy", C: Green),
            (R: "Hà Nội → Nam Định", I: "09:00 | 29B-205.12", S: "26/29", St: "Sắp chạy", C: Green),
            (R: "Hà Nội → Quảng Ninh", I: "09:45 | 29B-311.05", S: "34/40", St: "Đang đón khách", C: Green),
            (R: "Hà Nội → Đà Nẵng", I: "11:00 | 29B-402.19", S: "40/40", St: "Hết vé", C: Red)
        };
        int ty = 268;
        foreach (var tr in nextTrips)
        {
            using (var b = new SolidBrush(ColorTranslator.FromHtml("#F8FAFC")))
                g.FillRectangle(b, 896, ty, 424, 56);
            g.DrawRectangle(PBorder, 896, ty, 424, 56);
            g.DrawString(tr.R, FontGridHdr, BNavy, 908, ty + 8);
            g.DrawString(tr.I, FontSmall, BMuted, 908, ty + 30);
            using (var b = new SolidBrush(tr.C))
                g.DrawString($"Ghế: {tr.S}", FontGridHdr, b, 1230, ty + 8);
            g.DrawString(tr.St, FontSmall, BMuted, 1230, ty + 30);
            ty += 64;
        }

        // Bottom Table
        g.FillRectangle(BWhite, 254, 560, 1082, 236);
        g.DrawRectangle(PBorder, 254, 560, 1082, 236);
        g.DrawString("Tình trạng tuyến xe trọng điểm", FontCardT, BDark, 274, 574);
        var dashCols = new[] { "Tuyến xe", "Quãng đường", "Giá cơ bản", "Chuyến/ngày", "Vé đã bán", "Doanh thu thực", "Tỷ lệ lấp đầy" };
        var dashW = new[] { 200, 130, 130, 140, 130, 180, 140 };
        var dashRows = new[]
        {
            new[] { "Hà Nội - Hải Phòng", "105 km", "120.000 ₫", "4 chuyến", "74 vé", "8.880.000 ₫", "92.5%" },
            new[] { "Hà Nội - Quảng Ninh", "155 km", "160.000 ₫", "3 chuyến", "48 vé", "7.680.000 ₫", "80.0%" },
            new[] { "Hà Nội - Nam Định", "90 km", "100.000 ₫", "2 chuyến", "20 vé", "2.000.000 ₫", "69.0%" },
            new[] { "Hà Nội - Đà Nẵng", "760 km", "450.000 ₫", "1 chuyến", "40 vé", "18.000.000 ₫", "100.0%" },
            new[] { "Hà Nội - Sapa", "315 km", "280.000 ₫", "2 chuyến", "32 vé", "8.960.000 ₫", "80.0%" }
        };
        DrawGridTable(g, 274, 602, 1042, 180, dashCols, dashW, dashRows, 0);

        SaveCanvas(bmp, g, outDir, "02-dashboard");
    }

    // --- 03 SELL SEATMAP ---
    private static void Gen03SellSeatmap(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 1, "Bán vé");

        // Top Filter Bar
        g.FillRectangle(BWhite, 254, 102, 1082, 60);
        g.DrawRectangle(PBorder, 254, 102, 1082, 60);
        g.DrawString("Điểm đi:", FontSmallB, BDark, 268, 110);
        g.DrawString("Hà Nội", FontGridCell, BDark, 268, 130);
        g.DrawString("Điểm đến:", FontSmallB, BDark, 410, 110);
        g.DrawString("Hải Phòng", FontGridCell, BDark, 410, 130);
        g.DrawString("Ngày đi:", FontSmallB, BDark, 570, 110);
        g.DrawString("19/10/2026", FontGridCell, BDark, 570, 130);
        g.DrawString("Khoảng giá:", FontSmallB, BDark, 720, 110);
        g.DrawString("100.000 ₫ - 200.000 ₫", FontGridCell, BDark, 720, 130);
        g.FillRectangle(BNavy, 920, 115, 100, 34);
        g.DrawString("TÌM (F5)", FontBtn, BWhite, new RectangleF(920, 115, 100, 34), SfCenter);

        // Left: Trips list (w=440)
        g.FillRectangle(BWhite, 254, 174, 440, 626);
        g.DrawRectangle(PBorder, 254, 174, 440, 626);
        g.DrawString("Chuyến xe phù hợp (4 chuyến)", FontCardT, BNavy, 270, 188);
        g.DrawLine(PBorder, 270, 214, 674, 214);

        var trips = new[]
        {
            (Time: "07:00 → 09:00", Route: "Hà Nội - Hải Phòng", Price: "120.000 ₫", Bus: "29B-196.88 (40 chỗ)", Empty: "28 ghế trống", Sel: true),
            (Time: "09:30 → 11:30", Route: "Hà Nội - Hải Phòng", Price: "120.000 ₫", Bus: "29B-205.12 (29 chỗ)", Empty: "14 ghế trống", Sel: false),
            (Time: "13:00 → 15:00", Route: "Hà Nội - Hải Phòng", Price: "120.000 ₫", Bus: "29B-311.05 (40 chỗ)", Empty: "35 ghế trống", Sel: false),
            (Time: "16:30 → 18:30", Route: "Hà Nội - Hải Phòng", Price: "120.000 ₫", Bus: "29B-402.19 (40 chỗ)", Empty: "39 ghế trống", Sel: false)
        };
        int ty = 226;
        foreach (var t in trips)
        {
            using var bgTrip = new SolidBrush(ColorTranslator.FromHtml(t.Sel ? "#EFF6FF" : "#F8FAFC"));
            g.FillRectangle(bgTrip, 268, ty, 412, 76);
            if (t.Sel)
            {
                using var pTrip = new Pen(Navy, 1.5f);
                g.DrawRectangle(pTrip, 268, ty, 412, 76);
            }
            else
            {
                g.DrawRectangle(PBorder, 268, ty, 412, 76);
            }
            g.DrawString(t.Time, FontGridHdr, BNavy, 280, ty + 10);
            using var bPrice = new SolidBrush(ColorTranslator.FromHtml("#D97706"));
            g.DrawString(t.Price, FontCardT, bPrice, 570, ty + 8);
            g.DrawString(t.Bus, FontSmall, BDark, 280, ty + 32);
            using var bGr = new SolidBrush(Green);
            g.DrawString(t.Empty, FontSmallB, bGr, 280, ty + 52);
            ty += 88;
        }

        // Middle: Seatmap (w=340)
        g.FillRectangle(BWhite, 706, 174, 340, 626);
        g.DrawRectangle(PBorder, 706, 174, 340, 626);
        g.DrawString("Sơ đồ ghế: Xe 40 chỗ (29B-196.88)", FontCardT, BDark, 722, 188);

        using (var b = new SolidBrush(Green)) g.FillRectangle(b, 724, 218, 14, 14);
        g.DrawString("Trống", FontSmall, BDark, 742, 218);
        using (var b = new SolidBrush(Red)) g.FillRectangle(b, 790, 218, 14, 14);
        g.DrawString("Đã đặt", FontSmall, BDark, 808, 218);
        using (var b = new SolidBrush(Blue)) g.FillRectangle(b, 860, 218, 14, 14);
        g.DrawString("Đang chọn", FontSmall, BDark, 878, 218);
        g.DrawLine(PBorder, 722, 242, 1030, 242);

        g.DrawString("\uE806", FontSymbolSm, BMuted, 724, 254);
        g.DrawString("Vị trí tài xế", FontSmallB, BMuted, 742, 252);

        var bookedSeats = new[] { 1, 2, 6, 8, 11, 12, 15, 20, 22, 28, 30, 33 };
        int sy = 280;
        for (int row = 0; row < 10; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int seatNum = (row * 4) + col + 1;
                int sx = (col < 2) ? 730 + (col * 52) : 730 + (col * 52) + 38;

                Color sColor = Green;
                if (Array.IndexOf(bookedSeats, seatNum) >= 0) sColor = Red;
                if (seatNum == 5) sColor = Blue;

                using var bSeat = new SolidBrush(sColor);
                g.FillRectangle(bSeat, sx, sy, 44, 38);
                g.DrawRectangle(PBorder, sx, sy, 44, 38);
                var numStr = (seatNum < 10) ? $"0{seatNum}" : $"{seatNum}";
                g.DrawString(numStr, FontSmallB, BWhite, new RectangleF(sx, sy, 44, 38), SfCenter);
            }
            sy += 46;
        }

        // Right: Customer info & booking (w=278)
        g.FillRectangle(BWhite, 1058, 174, 278, 626);
        g.DrawRectangle(PBorder, 1058, 174, 278, 626);
        g.DrawString("Thông tin đặt vé", FontCardT, BNavy, 1074, 188);
        g.DrawLine(PBorder, 1074, 214, 1320, 214);

        var sellFields = new[]
        {
            (L: "Ghế đang chọn:", V: "Ghế số 05 (Tầng 1)"),
            (L: "Giá vé:", V: "120.000 ₫"),
            (L: "Số điện thoại (*):", V: "0912345678"),
            (L: "Họ và tên (*):", V: "Nguyễn Văn An"),
            (L: "CCCD/CMND:", V: "001200001234")
        };
        int ry = 226;
        foreach (var fld in sellFields)
        {
            g.DrawString(fld.L, FontSmallB, BDark, 1074, ry);
            ry += 18;
            using (var b = new SolidBrush(ColorTranslator.FromHtml("#F8FAFC")))
                g.FillRectangle(b, 1074, ry, 246, 32);
            g.DrawRectangle(PBorder, 1074, ry, 246, 32);
            g.DrawString(fld.V, FontGridCellB, BNavy, 1084, ry + 7);
            ry += 42;
        }

        using (var b = new SolidBrush(GreenBg))
            g.FillRectangle(b, 1074, ry, 246, 28);
        using (var p = new Pen(GreenBorder, 1f))
            g.DrawRectangle(p, 1074, ry, 246, 28);
        using (var b = new SolidBrush(Green))
        {
            g.DrawString("\uE73E", FontSymbolSm, b, 1082, ry + 7);
            g.DrawString("Khách hàng thân thiết (3 vé đã mua)", FontSmallB, b, 1098, ry + 6);
        }

        g.FillRectangle(BNavy, 1074, 580, 246, 44);
        g.DrawString("ĐẶT VÉ (Ctrl+S)", FontBtn, BWhite, new RectangleF(1074, 580, 246, 44), SfCenter);

        SaveCanvas(bmp, g, outDir, "03-sell-seatmap");
    }

    // --- 04 SELL PAYMENT DIALOG ---
    private static void Gen04SellPayment(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 1, "Bán vé");

        // Dim overlay
        using (var bDim = new SolidBrush(Color.FromArgb(120, 15, 23, 42)))
            g.FillRectangle(bDim, 0, 32, 1360, 788);

        // Modal Dialog
        int mx = 420, my = 210, mw = 520, mh = 380;
        g.FillRectangle(BWhite, mx, my, mw, mh);
        using (var pModal = new Pen(Navy, 2f))
            g.DrawRectangle(pModal, mx, my, mw, mh);

        g.FillRectangle(BNavy, mx, my, mw, 50);
        g.DrawString("Thanh toán vé xe", FontCardT, BWhite, mx + 20, my + 16);
        g.DrawString("\uE8BB", FontSymbolSm, BWhite, mx + mw - 30, my + 16);

        g.DrawString("Mã vé: TK-20261019-001  |  Ghế: 05", FontSmallB, BMuted, mx + 28, my + 70);
        g.DrawString("Khách hàng: Nguyễn Văn An (0912345678)", FontSmall, BDark, mx + 28, my + 92);
        g.DrawString("Tuyến: Hà Nội → Hải Phòng (07:00 19/10/2026)", FontSmall, BDark, mx + 28, my + 112);
        g.DrawLine(PBorder, mx + 28, my + 140, mx + mw - 28, my + 140);

        g.DrawString("Số tiền cần thanh toán:", FontMenu, BDark, mx + 28, my + 155);
        using (var fBigPrice = new Font("Segoe UI", 22f, FontStyle.Bold))
        using (var bPrice = new SolidBrush(ColorTranslator.FromHtml("#D97706")))
            g.DrawString("120.000 ₫", fBigPrice, bPrice, mx + 28, my + 178);

        g.DrawString("Phương thức thanh toán:", FontSmallB, BDark, mx + 28, my + 230);

        // Radios
        using (var pRadio = new Pen(Navy, 2f))
            g.DrawEllipse(pRadio, mx + 30, my + 260, 16, 16);
        g.FillEllipse(BNavy, mx + 34, my + 264, 8, 8);
        g.DrawString("Tiền mặt (CASH)", FontGridCellB, BDark, mx + 54, my + 258);

        g.DrawEllipse(PBorder, mx + 190, my + 260, 16, 16);
        g.DrawString("Chuyển khoản (TRANSFER)", FontGridCell, BDark, mx + 214, my + 258);

        g.DrawEllipse(PBorder, mx + 390, my + 260, 16, 16);
        g.DrawString("Thẻ (CARD)", FontGridCell, BDark, mx + 414, my + 258);

        // Buttons
        g.FillRectangle(BNavy, mx + 28, my + 310, 220, 42);
        g.DrawString("ĐỒNG Ý THANH TOÁN", FontBtn, BWhite, new RectangleF(mx + 28, my + 310, 220, 42), SfCenter);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0")))
            g.FillRectangle(b, mx + 268, my + 310, 220, 42);
        g.DrawString("HỦY BỎ (ĐỂ THANH TOÁN SAU)", FontSmallB, BDark, new RectangleF(mx + 268, my + 310, 220, 42), SfCenter);

        SaveCanvas(bmp, g, outDir, "04-sell-payment");
    }

    // --- 05 TICKETS ---
    private static void Gen05Tickets(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 2, "Quản lý vé");

        // Toolbar
        g.FillRectangle(BWhite, 254, 102, 1082, 54);
        g.DrawRectangle(PBorder, 254, 102, 1082, 54);
        g.DrawString("\uE721", FontSymbolSm, BDark, 268, 112);
        g.DrawString("Tìm mã vé, SĐT, tên khách:", FontSmallB, BDark, 286, 110);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#F8FAFC")))
            g.FillRectangle(b, 268, 128, 220, 24);
        g.DrawRectangle(PBorder, 268, 128, 220, 24);
        g.DrawString("0912345678", FontSmall, BDark, 276, 132);

        g.DrawString("Trạng thái:", FontSmallB, BDark, 510, 110);
        g.DrawString("Tất cả", FontSmall, BDark, 510, 130);
        g.DrawString("Từ ngày:", FontSmallB, BDark, 620, 110);
        g.DrawString("15/10/2026", FontSmall, BDark, 620, 130);
        g.DrawString("Đến ngày:", FontSmallB, BDark, 730, 110);
        g.DrawString("20/10/2026", FontSmall, BDark, 730, 130);

        g.FillRectangle(BNavy, 860, 112, 100, 34);
        g.DrawString("THANH TOÁN", FontSmallB, BWhite, new RectangleF(860, 112, 100, 34), SfCenter);

        using (var b = new SolidBrush(Red))
            g.FillRectangle(b, 970, 112, 80, 34);
        g.DrawString("HỦY VÉ", FontSmallB, BWhite, new RectangleF(970, 112, 80, 34), SfCenter);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#059669")))
            g.FillRectangle(b, 1060, 112, 120, 34);
        g.DrawString("\uE749", FontSymbolSm, BWhite, 1070, 122);
        g.DrawString("IN VÉ (Ctrl+P)", FontSmallB, BWhite, 1088, 121);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#16A34A")))
            g.FillRectangle(b, 1190, 112, 130, 34);
        g.DrawString("\uE9D2", FontSymbolSm, BWhite, 1200, 122);
        g.DrawString("XUẤT EXCEL", FontSmallB, BWhite, 1218, 121);

        var tCols = new[] { "Mã vé", "Tuyến xe", "Khởi hành", "Ghế", "Khách hàng", "Số điện thoại", "Giá vé", "Trạng thái", "Người bán" };
        var tWidths = new[] { 120, 180, 140, 60, 160, 120, 110, 130, 110 };
        var tRows = new[]
        {
            new[] { "TK-00101", "Hà Nội - Hải Phòng", "19/10 07:00", "05", "Nguyễn Văn An", "0912345678", "120.000 ₫", "Đã thanh toán", "operator01" },
            new[] { "TK-00102", "Hà Nội - Hải Phòng", "19/10 07:00", "06", "Trần Thị Bình", "0987654321", "120.000 ₫", "Đã thanh toán", "seller01" },
            new[] { "TK-00103", "Hà Nội - Hải Phòng", "19/10 07:00", "08", "Lê Hoàng Cường", "0903112233", "120.000 ₫", "Chờ thanh toán", "seller01" },
            new[] { "TK-00098", "Hà Nội - Nam Định", "18/10 09:00", "12", "Phạm Minh Đức", "0915667788", "100.000 ₫", "Đã hủy", "seller02" },
            new[] { "TK-00095", "Hà Nội - Đà Nẵng", "18/10 11:00", "02", "Hoàng Kim Dung", "0944556677", "450.000 ₫", "Đã thanh toán", "operator01" },
            new[] { "TK-00092", "Hà Nội - Quảng Ninh", "18/10 09:45", "15", "Vũ Hải Nam", "0977889900", "160.000 ₫", "Đã thanh toán", "seller01" }
        };
        DrawGridTable(g, 254, 168, 1082, 630, tCols, tWidths, tRows, 0);

        SaveCanvas(bmp, g, outDir, "05-tickets");
    }

    // --- 06 PASSENGERS ---
    private static void Gen06Passengers(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 3, "Quản lý hành khách");

        var pCols = new[] { "Mã KH", "Họ và tên", "Số điện thoại", "CCCD / CMND", "Tổng vé đã đặt" };
        var pWidths = new[] { 80, 200, 140, 160, 140 };
        var pRows = new[]
        {
            new[] { "KH001", "Nguyễn Văn An", "0912345678", "001200001234", "3 vé" },
            new[] { "KH002", "Trần Thị Bình", "0987654321", "001201005678", "2 vé" },
            new[] { "KH003", "Lê Hoàng Cường", "0903112233", "001202009876", "1 vé" },
            new[] { "KH004", "Phạm Minh Đức", "0915667788", "001203004321", "4 vé" },
            new[] { "KH005", "Hoàng Kim Dung", "0944556677", "001204001122", "2 vé" }
        };
        DrawGridTable(g, 254, 102, 750, 360, pCols, pWidths, pRows, 0);

        var pFields = new[]
        {
            (Label: "Họ và tên (*)", Value: "Nguyễn Văn An", Disabled: false),
            (Label: "Số điện thoại (*)", Value: "0912345678", Disabled: false),
            (Label: "CCCD / CMND", Value: "001200001234", Disabled: false),
            (Label: "Ghi chú", Value: "Khách đặt thường xuyên tuyến Hải Phòng", Disabled: false)
        };
        DrawFormPanel(g, 1020, 102, 316, 696, "Thông tin hành khách", pFields);

        // Lower history panel
        g.FillRectangle(BWhite, 254, 478, 750, 320);
        g.DrawRectangle(PBorder, 254, 478, 750, 320);
        g.DrawString("Lịch sử đặt vé của khách hàng: Nguyễn Văn An (0912345678)", FontCardT, BNavy, 270, 492);
        var phCols = new[] { "Mã vé", "Tuyến xe", "Khởi hành", "Ghế", "Giá vé", "Trạng thái" };
        var phWidths = new[] { 110, 200, 140, 60, 110, 110 };
        var phRows = new[]
        {
            new[] { "TK-00101", "Hà Nội - Hải Phòng", "19/10/2026 07:00", "05", "120.000 ₫", "PAID (Đã thanh toán)" },
            new[] { "TK-00045", "Hà Nội - Hải Phòng", "05/10/2026 08:30", "12", "120.000 ₫", "PAID (Đã thanh toán)" },
            new[] { "TK-00012", "Hà Nội - Nam Định", "22/09/2026 09:00", "03", "100.000 ₫", "PAID (Đã thanh toán)" }
        };
        DrawGridTable(g, 270, 522, 718, 260, phCols, phWidths, phRows, -1);

        SaveCanvas(bmp, g, outDir, "06-passengers");
    }

    // --- 07 ROUTES ---
    private static void Gen07Routes(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 4, "Quản lý tuyến xe");

        var rCols = new[] { "Mã tuyến", "Tên tuyến xe", "Điểm đi", "Điểm đến", "Cự ly", "Thời gian", "Giá cơ bản" };
        var rWidths = new[] { 80, 180, 100, 100, 80, 90, 110 };
        var rRows = new[]
        {
            new[] { "RT01", "Hà Nội - Hải Phòng", "Hà Nội", "Hải Phòng", "105 km", "120 phút", "120.000 ₫" },
            new[] { "RT02", "Hà Nội - Quảng Ninh", "Hà Nội", "Quảng Ninh", "155 km", "180 phút", "160.000 ₫" },
            new[] { "RT03", "Hà Nội - Nam Định", "Hà Nội", "Nam Định", "90 km", "90 phút", "100.000 ₫" },
            new[] { "RT04", "Hà Nội - Đà Nẵng", "Hà Nội", "Đà Nẵng", "760 km", "840 phút", "450.000 ₫" },
            new[] { "RT05", "Hà Nội - Sapa", "Hà Nội", "Lào Cai", "315 km", "360 phút", "280.000 ₫" },
            new[] { "RT06", "Hải Phòng - Hà Nội", "Hải Phòng", "Hà Nội", "105 km", "120 phút", "120.000 ₫" }
        };
        DrawGridTable(g, 254, 102, 750, 696, rCols, rWidths, rRows, 0);

        var rFields = new[]
        {
            (Label: "Tên tuyến (*)", Value: "Hà Nội - Hải Phòng", Disabled: false),
            (Label: "Điểm đi (*)", Value: "Hà Nội", Disabled: false),
            (Label: "Điểm đến (*)", Value: "Hải Phòng", Disabled: false),
            (Label: "Quãng đường (km)", Value: "105", Disabled: false),
            (Label: "Thời gian dự kiến (phút)", Value: "120", Disabled: false),
            (Label: "Giá vé cơ bản (₫) (*)", Value: "120000", Disabled: false)
        };
        DrawFormPanel(g, 1020, 102, 316, 696, "Thông tin tuyến xe", rFields);

        SaveCanvas(bmp, g, outDir, "07-routes");
    }

    // --- 08 VEHICLES ---
    private static void Gen08Vehicles(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 5, "Quản lý phương tiện");

        var vCols = new[] { "Mã xe", "Biển số xe", "Loại phương tiện", "Số ghế", "Trạng thái" };
        var vWidths = new[] { 80, 150, 220, 100, 180 };
        var vRows = new[]
        {
            new[] { "VH01", "29B-196.88", "Giường nằm 40 chỗ", "40", "ACTIVE (Hoạt động)" },
            new[] { "VH02", "29B-205.12", "Ghế ngồi 29 chỗ", "29", "ACTIVE (Hoạt động)" },
            new[] { "VH03", "29B-311.05", "Giường nằm 40 chỗ", "40", "ACTIVE (Hoạt động)" },
            new[] { "VH04", "29B-402.19", "Limousine 34 phòng", "34", "ACTIVE (Hoạt động)" },
            new[] { "VH05", "30A-999.01", "Limousine 16 chỗ", "16", "MAINTENANCE (Bảo trì)" },
            new[] { "VH06", "29B-108.45", "Ghế ngồi 45 chỗ", "45", "RETIRED (Ngừng dùng)" }
        };
        DrawGridTable(g, 254, 102, 750, 696, vCols, vWidths, vRows, 0);

        var vFields = new[]
        {
            (Label: "Biển số xe (*)", Value: "29B-196.88", Disabled: false),
            (Label: "Loại phương tiện (*)", Value: "Giường nằm 40 chỗ", Disabled: false),
            (Label: "Số ghế ngồi (1 - 60)", Value: "40", Disabled: false),
            (Label: "Trạng thái", Value: "ACTIVE (Hoạt động)", Disabled: false)
        };
        DrawFormPanel(g, 1020, 102, 316, 696, "Thông tin phương tiện", vFields);

        SaveCanvas(bmp, g, outDir, "08-vehicles");
    }

    // --- 09 DRIVERS ---
    private static void Gen09Drivers(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 6, "Quản lý tài xế");

        // Yellow warning banner
        using (var b = new SolidBrush(YellowBg))
            g.FillRectangle(b, 254, 102, 1082, 40);
        using (var p = new Pen(YellowBorder, 1.5f))
            g.DrawRectangle(p, 254, 102, 1082, 40);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#92400E")))
        {
            g.DrawString("\uE7BA", FontSymbolSm, b, 268, 116);
            g.DrawString("Chú ý: Có 1 tài xế sắp hết hạn bằng lái trong 30 ngày tới (Đỗ Minh Quân - Hạng E - Hạn: 28/10/2026). Vui lòng yêu cầu gia hạn bằng.", FontSmallB, b, 288, 114);
        }

        var dCols = new[] { "Mã TX", "Họ và tên", "Số điện thoại", "CCCD", "Hạng bằng", "Hạn bằng lái", "Tình trạng bằng" };
        var dWidths = new[] { 70, 160, 110, 120, 80, 100, 100 };
        var dRows = new[]
        {
            new[] { "DR01", "Nguyễn Văn Hùng", "0912111222", "001085001234", "Hạng E", "15/08/2028", "Còn hạn" },
            new[] { "DR02", "Đỗ Minh Quân", "0988222333", "001086005678", "Hạng E", "28/10/2026", "Sắp hết hạn" },
            new[] { "DR03", "Trần Đình Trọng", "0903333444", "001087009876", "Hạng D", "10/12/2027", "Còn hạn" },
            new[] { "DR04", "Lê Văn Thắng", "0915444555", "001088004321", "Hạng D2", "04/05/2029", "Còn hạn" },
            new[] { "DR05", "Vũ Quốc Việt", "0944555666", "001089001122", "Hạng E", "12/09/2026", "Hết hạn" }
        };
        DrawGridTable(g, 254, 154, 750, 644, dCols, dWidths, dRows, 1);

        var dFields = new[]
        {
            (Label: "Họ và tên (*)", Value: "Đỗ Minh Quân", Disabled: false),
            (Label: "Số điện thoại (*)", Value: "0988222333", Disabled: false),
            (Label: "Số CCCD (*)", Value: "001086005678", Disabled: false),
            (Label: "Số giấy phép lái xe", Value: "GPLX-790123456", Disabled: false),
            (Label: "Hạng bằng lái", Value: "Hạng E (Xe khách > 30 chỗ)", Disabled: false),
            (Label: "Ngày hết hạn bằng (*)", Value: "28/10/2026", Disabled: false)
        };
        DrawFormPanel(g, 1020, 154, 316, 644, "Thông tin tài xế", dFields);

        SaveCanvas(bmp, g, outDir, "09-drivers");
    }

    // --- 10 SCHEDULES ---
    private static void Gen10Schedules(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 7, "Quản lý lịch chạy");

        // Filter toolbar
        g.FillRectangle(BWhite, 254, 102, 1082, 54);
        g.DrawRectangle(PBorder, 254, 102, 1082, 54);
        g.DrawString("Từ ngày:", FontSmallB, BDark, 268, 110);
        g.DrawString("18/10/2026", FontSmall, BDark, 268, 130);
        g.DrawString("Đến ngày:", FontSmallB, BDark, 390, 110);
        g.DrawString("25/10/2026", FontSmall, BDark, 390, 130);
        g.DrawString("Tuyến xe:", FontSmallB, BDark, 520, 110);
        g.DrawString("Tất cả tuyến", FontSmall, BDark, 520, 130);

        g.FillRectangle(BNavy, 660, 114, 90, 32);
        g.DrawString("LỌC", FontBtn, BWhite, new RectangleF(660, 114, 90, 32), SfCenter);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#059669")))
            g.FillRectangle(b, 760, 114, 130, 32);
        g.DrawString("BẮT ĐẦU CHẠY", FontBtn, BWhite, new RectangleF(760, 114, 130, 32), SfCenter);

        using (var b = new SolidBrush(Red))
            g.FillRectangle(b, 900, 114, 110, 32);
        g.DrawString("HỦY CHUYẾN", FontBtn, BWhite, new RectangleF(900, 114, 110, 32), SfCenter);

        var sCols = new[] { "Mã", "Tuyến xe", "Khởi hành", "Dự kiến đến", "Biển số xe", "Tài xế", "Đã bán / Ghế", "Trạng thái" };
        var sWidths = new[] { 50, 160, 100, 100, 90, 110, 80, 80 };
        var sRows = new[]
        {
            new[] { "SCH01", "Hà Nội - Hải Phòng", "19/10 07:00", "19/10 09:00", "29B-196.88", "Nguyễn Văn Hùng", "12 / 40", "ACTIVE" },
            new[] { "SCH02", "Hà Nội - Hải Phòng", "19/10 09:30", "19/10 11:30", "29B-205.12", "Trần Đình Trọng", "15 / 29", "ACTIVE" },
            new[] { "SCH03", "Hà Nội - Quảng Ninh", "19/10 13:00", "19/10 16:00", "29B-311.05", "Lê Văn Thắng", "6 / 40", "ACTIVE" },
            new[] { "SCH04", "Hà Nội - Đà Nẵng", "19/10 18:00", "20/10 08:00", "29B-402.19", "Nguyễn Văn Hùng", "34 / 34", "ACTIVE" },
            new[] { "SCH05", "Hà Nội - Nam Định", "18/10 14:00", "18/10 15:30", "29B-205.12", "Trần Đình Trọng", "20 / 29", "COMPLETED" }
        };
        DrawGridTable(g, 254, 168, 750, 630, sCols, sWidths, sRows, 0);

        var sFields = new[]
        {
            (Label: "Tuyến xe (*)", Value: "Hà Nội - Hải Phòng", Disabled: false),
            (Label: "Giá vé tự điền (₫)", Value: "120.000", Disabled: true),
            (Label: "Phương tiện (ACTIVE)", Value: "29B-196.88 (40 chỗ)", Disabled: false),
            (Label: "Tài xế (còn hạn bằng)", Value: "Nguyễn Văn Hùng (Hạng E)", Disabled: false),
            (Label: "Thời điểm khởi hành (*)", Value: "19/10/2026 07:00", Disabled: false),
            (Label: "Thời điểm đến dự kiến", Value: "19/10/2026 09:00", Disabled: false)
        };
        DrawFormPanel(g, 1020, 168, 316, 630, "Tạo lịch chạy mới", sFields);

        SaveCanvas(bmp, g, outDir, "10-schedules");
    }

    // --- 11 STATISTICS ---
    private static void Gen11Statistics(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 8, "Thống kê doanh thu & chuyến xe");

        var stCards = new[]
        {
            (L: "Tổng số chuyến", V: "64 chuyến", C: "#0B3D91"),
            (L: "Vé đã bán", V: "1.840 vé", C: "#10B981"),
            (L: "Doanh thu thực", V: "312.450.000 ₫", C: "#D97706"),
            (L: "Tỷ lệ lấp đầy TB", V: "82.4%", C: "#6366F1"),
            (L: "Vé đã hủy", V: "18 vé (hoàn tiền)", C: "#EF4444")
        };
        for (int i = 0; i < 5; i++)
        {
            int cx = 254 + (i * 218);
            var sc = stCards[i];
            g.FillRectangle(BWhite, cx, 102, 206, 80);
            g.DrawRectangle(PBorder, cx, 102, 206, 80);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(sc.C)))
                g.FillRectangle(b, cx, 102, 4, 80);
            g.DrawString(sc.L, FontSmall, BMuted, cx + 12, 112);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(sc.C)))
                g.DrawString(sc.V, FontCardT, b, cx + 12, 136);
        }

        // Chart
        g.FillRectangle(BWhite, 254, 194, 1082, 260);
        g.DrawRectangle(PBorder, 254, 194, 1082, 260);
        g.DrawString("Biểu đồ doanh thu thực theo tuyến xe (Tháng 10/2026)", FontCardT, BNavy, 274, 208);

        var routesChart = new[]
        {
            (N: "Hà Nội - Đà Nẵng", V: 126000, L: "126.0tr"),
            (N: "Hà Nội - Hải Phòng", V: 84500, L: "84.5tr"),
            (N: "Hà Nội - Quảng Ninh", V: 58200, L: "58.2tr"),
            (N: "Hà Nội - Sapa", V: 26400, L: "26.4tr"),
            (N: "Hà Nội - Nam Định", V: 17350, L: "17.4tr")
        };
        int cy = 246;
        foreach (var rc in routesChart)
        {
            g.DrawString(rc.N, FontSmallB, BDark, 274, cy + 6);
            int barW = (int)((rc.V / 130000.0) * 650);
            g.FillRectangle(BNavy, 440, cy + 4, barW, 22);
            using var bRev = new SolidBrush(ColorTranslator.FromHtml("#D97706"));
            g.DrawString(rc.L, FontSmallB, bRev, 450 + barW, cy + 6);
            cy += 38;
        }

        // Table V_TRIP_STATISTICS
        var vCols = new[] { "Mã chuyến", "Tuyến xe", "Khởi hành", "Xe", "Tài xế", "Giá vé", "Vé đã bán", "Vé hủy", "Doanh thu thực", "Tỷ lệ lấp đầy" };
        var vWidths = new[] { 70, 160, 110, 90, 120, 90, 80, 70, 120, 90 };
        var vRows = new[]
        {
            new[] { "SCH01", "Hà Nội - Hải Phòng", "18/10 07:00", "29B-196.88", "Nguyễn Văn Hùng", "120.000 ₫", "38", "1", "4.440.000 ₫", "95.0%" },
            new[] { "SCH02", "Hà Nội - Hải Phòng", "18/10 09:30", "29B-205.12", "Trần Đình Trọng", "120.000 ₫", "26", "0", "3.120.000 ₫", "89.7%" },
            new[] { "SCH03", "Hà Nội - Quảng Ninh", "18/10 13:00", "29B-311.05", "Lê Văn Thắng", "160.000 ₫", "34", "2", "5.120.000 ₫", "85.0%" },
            new[] { "SCH04", "Hà Nội - Đà Nẵng", "18/10 18:00", "29B-402.19", "Nguyễn Văn Hùng", "450.000 ₫", "34", "0", "15.300.000 ₫", "100.0%" },
            new[] { "SCH05", "Hà Nội - Nam Định", "18/10 14:00", "29B-205.12", "Trần Đình Trọng", "100.000 ₫", "20", "0", "2.000.000 ₫", "69.0%" }
        };
        DrawGridTable(g, 254, 466, 1082, 332, vCols, vWidths, vRows, -1);

        SaveCanvas(bmp, g, outDir, "11-statistics");
    }

    // --- 12 STAFF ---
    private static void Gen12Staff(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 9, "Quản lý nhân viên");

        var stCols = new[] { "Mã NV", "Tên đăng nhập", "Họ và tên", "Số điện thoại", "Vai trò Oracle", "Trạng thái tài khoản" };
        var stWidths = new[] { 70, 130, 160, 110, 140, 140 };
        var stRows = new[]
        {
            new[] { "ST01", "operator01", "Nguyễn Văn Điều Hành", "0911000111", "ROLE_OPERATOR", "ACTIVE (Hoạt động)" },
            new[] { "ST02", "seller01", "Trần Bán Vé", "0922000222", "ROLE_TICKET_SELLER", "ACTIVE (Hoạt động)" },
            new[] { "ST03", "seller02", "Lê Thu Ngân", "0933000333", "ROLE_TICKET_SELLER", "ACTIVE (Hoạt động)" },
            new[] { "ST04", "seller03", "Phạm Văn Quầy", "0944000444", "ROLE_TICKET_SELLER", "LOCKED (Đã khóa)" }
        };
        DrawGridTable(g, 254, 102, 750, 696, stCols, stWidths, stRows, 1);

        var stFields = new[]
        {
            (Label: "Tên đăng nhập (Oracle User) (*)", Value: "seller01", Disabled: true),
            (Label: "Họ và tên (*)", Value: "Trần Bán Vé", Disabled: false),
            (Label: "Số điện thoại (*)", Value: "0922000222", Disabled: false),
            (Label: "Vai trò", Value: "ROLE_TICKET_SELLER (Bán vé)", Disabled: false),
            (Label: "Trạng thái", Value: "ACTIVE (Đang hoạt động)", Disabled: false)
        };
        DrawFormPanel(g, 1020, 102, 316, 696, "Thông tin nhân viên", stFields);

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#D97706")))
            g.FillRectangle(b, 1036, 440, 130, 32);
        g.DrawString("ĐẶT LẠI MK", FontSmallB, BWhite, new RectangleF(1036, 440, 130, 32), SfCenter);

        using (var b = new SolidBrush(Red))
            g.FillRectangle(b, 1180, 440, 130, 32);
        g.DrawString("KHÓA USER", FontSmallB, BWhite, new RectangleF(1180, 440, 130, 32), SfCenter);

        SaveCanvas(bmp, g, outDir, "12-staff");
    }

    // --- 13 BACKUP ---
    private static void Gen13Backup(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 10, "Sao lưu và phục hồi CSDL");

        // Green Success Bar
        using (var b = new SolidBrush(GreenBg))
            g.FillRectangle(b, 254, 102, 1082, 42);
        using (var p = new Pen(GreenBorder, 1.5f))
            g.DrawRectangle(p, 254, 102, 1082, 42);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#166534")))
        {
            g.DrawString("\uE73E", FontSymbolSm, b, 268, 116);
            g.DrawString("Đã sao lưu dữ liệu vận hành thành công lúc 14:32:15. Đã lưu snapshot 64 chuyến, 1.840 vé, 1.840 thanh toán vào bảng *_BK.", FontSmallB, b, 288, 115);
        }

        // Left Col
        g.FillRectangle(BWhite, 254, 156, 530, 642);
        g.DrawRectangle(PBorder, 254, 156, 530, 642);
        g.DrawString("Sao lưu dữ liệu vận hành (PL/SQL)", FontCardT, BNavy, 270, 170);
        g.DrawString("Chép toàn bộ dữ liệu lịch chạy, vé, thanh toán sang các bảng `_BK` (giữ 7 bản).", FontSmall, BMuted, 270, 194);

        g.FillRectangle(BNavy, 270, 222, 160, 38);
        g.DrawString("\uE7C0", FontSymbolSm, BWhite, 282, 234);
        g.DrawString("SAO LƯU NGAY", FontBtn, BWhite, 298, 231);

        var bkCols = new[] { "Mã BK", "Thời điểm sao lưu", "Người thực hiện", "Số bản ghi", "Trạng thái" };
        var bkW = new[] { 60, 130, 110, 90, 90 };
        var bkRows = new[]
        {
            new[] { "BK-07", "18/10 14:32:15", "operator01", "3.744 dòng", "Thành công" },
            new[] { "BK-06", "18/10 02:00:00", "JOB_NIGHTLY", "3.680 dòng", "Thành công" },
            new[] { "BK-05", "17/10 02:00:00", "JOB_NIGHTLY", "3.520 dòng", "Thành công" },
            new[] { "BK-04", "16/10 02:00:00", "JOB_NIGHTLY", "3.410 dòng", "Thành công" },
            new[] { "BK-03", "15/10 02:00:00", "JOB_NIGHTLY", "3.280 dòng", "Thành công" }
        };
        DrawGridTable(g, 270, 274, 498, 508, bkCols, bkW, bkRows, 0);

        // Right Col Upper: Job status
        g.FillRectangle(BWhite, 800, 156, 536, 310);
        g.DrawRectangle(PBorder, 800, 156, 536, 310);
        g.DrawString("Job tự động sao lưu hàng đêm (DBMS_SCHEDULER)", FontCardT, BNavy, 816, 170);
        g.DrawLine(PBorder, 816, 196, 1320, 196);

        g.DrawString("Tên Job: JOB_NIGHTLY_BACKUP", FontGridHdr, BDark, 816, 208);
        using (var b = new SolidBrush(Green))
            g.DrawString("Trạng thái: SCHEDULED (Đang hoạt động)", FontGridHdr, b, 816, 230);
        g.DrawString("Lịch chạy: Hàng ngày lúc 02:00:00 sáng", FontSmall, BDark, 816, 252);
        g.DrawString("Lần chạy gần nhất: 18/10/2026 02:00:00 (Thành công - Thời gian chạy: 1.2s)", FontSmall, BDark, 816, 274);
        g.DrawString("Lần chạy tiếp theo: 19/10/2026 02:00:00", FontSmall, BNavy, 816, 296);
        using (var b = new SolidBrush(Green))
            g.DrawString("Số lần gặp lỗi: 0 lần", FontSmall, b, 816, 318);

        // Right Col Lower: Data Pump
        g.FillRectangle(BWhite, 800, 480, 536, 318);
        g.DrawRectangle(PBorder, 800, 480, 536, 318);
        g.DrawString("Sao lưu vật lý toàn bộ (Oracle Data Pump)", FontCardT, BNavy, 816, 494);
        g.DrawLine(PBorder, 816, 520, 1320, 520);

        g.DrawString(@"Thư mục lưu trữ: D:\OracleBackup (Directory BACKUP_DIR)", FontSmallB, BDark, 816, 532);
        g.DrawString("1. Chạy file batch sao lưu toàn bộ schema TRANSPORT:", FontSmall, BDark, 816, 556);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#1E293B")))
            g.FillRectangle(b, 816, 576, 504, 30);
        using var fCode = new Font("Consolas", 9.5f);
        g.DrawString(@"database\backup.bat", fCode, BWhite, 826, 582);

        g.DrawString("2. Phục hồi CSDL từ file dump gần nhất:", FontSmall, BDark, 816, 616);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#1E293B")))
            g.FillRectangle(b, 816, 636, 504, 30);
        g.DrawString(@"database\restore.bat transport_20261018_1432.dmp", fCode, BWhite, 826, 642);

        SaveCanvas(bmp, g, outDir, "13-backup");
    }

    // --- 14 ENGLISH ---
    private static void Gen14English(string outDir)
    {
        var (bmp, g) = NewCanvas(1360, 820);
        DrawWindowBase(g, 1360, 820, 0, "Operations Overview", true, "Nguyen Van Dieu Hanh", "Dispatcher (ROLE_OPERATOR)");

        var enCards = new[]
        {
            (L: "Today's Trips", V: "8 trips", Sub: "4 trips open for booking", Color: "#0B3D91"),
            (L: "Tickets Sold Today", V: "142 tickets", Sub: "+12% compared to yesterday", Color: "#10B981"),
            (L: "Net Revenue Today", V: "24,650,000 ₫", Sub: "0 ₫ refunded", Color: "#F59E0B"),
            (L: "Avg Occupancy Rate", V: "78.5%", Sub: "Avg 32/40 seats per trip", Color: "#6366F1")
        };
        for (int i = 0; i < 4; i++)
        {
            int cx = 254 + (i * 272);
            var cd = enCards[i];
            g.FillRectangle(BWhite, cx, 106, 256, 92);
            g.DrawRectangle(PBorder, cx, 106, 256, 92);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(cd.Color)))
                g.FillRectangle(b, cx, 106, 4, 92);
            g.DrawString(cd.L, FontSmall, BMuted, cx + 16, 118);
            g.DrawString(cd.V, FontCardVal, BDark, cx + 16, 136);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(cd.Color)))
                g.DrawString(cd.Sub, FontSmall, b, cx + 16, 170);
        }

        // Chart Card
        g.FillRectangle(BWhite, 254, 214, 610, 330);
        g.DrawRectangle(PBorder, 254, 214, 610, 330);
        g.DrawString("Net Revenue Last 7 Days (k VND)", FontCardT, BDark, 274, 228);
        g.DrawLine(PBorder, 274, 254, 844, 254);
        var days = new[] { "12/10", "13/10", "14/10", "15/10", "16/10", "17/10", "18/10" };
        var revs = new[] { 18500, 21200, 19800, 26400, 24100, 29800, 24650 };
        for (int d = 0; d < 7; d++)
        {
            int bx = 295 + (d * 78);
            int bh = (int)((revs[d] / 35000f) * 190);
            int by = 500 - bh;
            Brush bBrush = (d == 6) ? BGold : BNavy;
            g.FillRectangle(bBrush, bx, by, 44, bh);
            g.DrawString($"{Math.Round(revs[d] / 1000.0, 1)}k", FontSmallB, BDark, bx + 6, by - 18);
            g.DrawString(days[d], FontSmall, BMuted, bx + 8, 506);
        }

        // Upcoming
        g.FillRectangle(BWhite, 880, 214, 456, 330);
        g.DrawRectangle(PBorder, 880, 214, 456, 330);
        g.DrawString("Upcoming Departures Today", FontCardT, BDark, 900, 228);
        g.DrawLine(PBorder, 900, 254, 1316, 254);
        var enNextTrips = new[]
        {
            (R: "Hanoi → Haiphong", I: "08:30 | 29B-196.88", S: "38/40", St: "Ready", C: Green),
            (R: "Hanoi → Nam Dinh", I: "09:00 | 29B-205.12", S: "26/29", St: "Ready", C: Green),
            (R: "Hanoi → Quang Ninh", I: "09:45 | 29B-311.05", S: "34/40", St: "Boarding", C: Green),
            (R: "Hanoi → Da Nang", I: "11:00 | 29B-402.19", S: "40/40", St: "Sold Out", C: Red)
        };
        int ty = 268;
        foreach (var tr in enNextTrips)
        {
            using (var b = new SolidBrush(ColorTranslator.FromHtml("#F8FAFC")))
                g.FillRectangle(b, 896, ty, 424, 56);
            g.DrawRectangle(PBorder, 896, ty, 424, 56);
            g.DrawString(tr.R, FontGridHdr, BNavy, 908, ty + 8);
            g.DrawString(tr.I, FontSmall, BMuted, 908, ty + 30);
            using (var b = new SolidBrush(tr.C))
                g.DrawString($"Seats: {tr.S}", FontGridHdr, b, 1220, ty + 8);
            g.DrawString(tr.St, FontSmall, BMuted, 1220, ty + 30);
            ty += 64;
        }

        // Bottom Table
        g.FillRectangle(BWhite, 254, 560, 1082, 236);
        g.DrawRectangle(PBorder, 254, 560, 1082, 236);
        g.DrawString("Key Routes Status", FontCardT, BDark, 274, 574);
        var enCols = new[] { "Route Name", "Distance", "Base Fare", "Daily Trips", "Tickets Sold", "Net Revenue", "Occupancy Rate" };
        var dashW = new[] { 200, 130, 130, 140, 130, 180, 140 };
        var dashRows = new[]
        {
            new[] { "Hanoi - Haiphong", "105 km", "120.000 ₫", "4 trips", "74 tix", "8.880.000 ₫", "92.5%" },
            new[] { "Hanoi - Quang Ninh", "155 km", "160.000 ₫", "3 trips", "48 tix", "7.680.000 ₫", "80.0%" },
            new[] { "Hanoi - Nam Dinh", "90 km", "100.000 ₫", "2 trips", "20 tix", "2.000.000 ₫", "69.0%" },
            new[] { "Hanoi - Da Nang", "760 km", "450.000 ₫", "1 trip", "40 tix", "18.000.000 ₫", "100.0%" },
            new[] { "Hanoi - Sapa", "315 km", "280.000 ₫", "2 trips", "32 tix", "8.960.000 ₫", "80.0%" }
        };
        DrawGridTable(g, 274, 602, 1042, 180, enCols, dashW, dashRows, 0);

        SaveCanvas(bmp, g, outDir, "14-english");
    }

    // --- 15 EXCEL ---
    private static void Gen15Excel(string outDir)
    {
        var (bmp, g) = NewCanvas(1280, 720);

        // Excel Chrome
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#107C41")))
            g.FillRectangle(b, 0, 0, 1280, 42);
        g.DrawString("BaoCao_ThongKe_20261018.xlsx - Excel", FontMenuB, BWhite, 16, 12);
        g.DrawString("\uE921", FontSymbolSm, BWhite, 1200, 14);
        g.DrawString("\uE922", FontSymbolSm, BWhite, 1225, 14);
        g.DrawString("\uE8BB", FontSymbolSm, BWhite, 1250, 14);

        // Ribbon
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#F3F4F6")))
            g.FillRectangle(b, 0, 42, 1280, 58);
        g.DrawLine(PBorder, 0, 100, 1280, 100);
        g.DrawString("File    Home    Insert    Page Layout    Formulas    Data    Review    View", FontGridCell, BDark, 16, 52);
        g.DrawString("\uE77F", FontSymbolSm, BMuted, 16, 80);
        g.DrawString("Paste  |  B  I  U  |  Font: Segoe UI  11  |  Align  |  Format: #,##0 ₫  |  AutoSum  |  Sort & Filter", FontSmall, BMuted, 34, 78);

        // Formula Bar
        g.FillRectangle(BWhite, 0, 100, 1280, 28);
        g.DrawLine(PBorder, 0, 128, 1280, 128);
        g.DrawString("A1", FontSmallB, BNavy, 16, 106);
        g.DrawLine(PBorder, 60, 100, 60, 128);
        g.DrawString("fx  BÁO CÁO THỐNG KÊ DOANH THU & CHUYẾN XE", FontSmall, BDark, 70, 106);

        // Sheet grid
        g.FillRectangle(BWhite, 0, 128, 1280, 592);

        // Title row
        g.FillRectangle(BNavy, 40, 140, 1200, 36);
        using var fExTitle = new Font("Segoe UI", 13f, FontStyle.Bold);
        g.DrawString("BÁO CÁO THỐNG KÊ DOANH THU & CHUYẾN XE", fExTitle, BWhite, 50, 148);

        // Metadata
        g.DrawString("Thời điểm xuất: 18/10/2026 14:30:00  |  Người xuất: operator01 (Nguyễn Văn Điều Hành)  |  Đơn vị: Đề tài 11 - HaUI", FontSmallB, BMuted, 50, 184);

        // Table
        var exHeaders = new[] { "Mã chuyến", "Tuyến xe", "Khởi hành", "Thời điểm đến", "Biển số xe", "Tài xế", "Giá vé", "Vé đã bán", "Doanh thu thực", "Tỷ lệ lấp đầy" };
        var exW = new[] { 90, 160, 120, 120, 100, 130, 100, 90, 140, 110 };
        int exY = 210;

        using (var b = new SolidBrush(ColorTranslator.FromHtml("#1E3A8A")))
            g.FillRectangle(b, 40, exY, 1200, 32);

        int curExX = 50;
        for (int c = 0; c < exHeaders.Length; c++)
        {
            g.DrawString(exHeaders[c] + " ▼", FontSmallB, BWhite, curExX, exY + 8);
            curExX += exW[c];
        }

        var exRows = new[]
        {
            new[] { "SCH001", "Hà Nội - Hải Phòng", "18/10/2026 07:00", "18/10/2026 09:00", "29B-196.88", "Nguyễn Văn Hùng", "120.000 ₫", "38", "4.440.000 ₫", "95.0%" },
            new[] { "SCH002", "Hà Nội - Hải Phòng", "18/10/2026 09:30", "18/10/2026 11:30", "29B-205.12", "Trần Đình Trọng", "120.000 ₫", "26", "3.120.000 ₫", "89.7%" },
            new[] { "SCH003", "Hà Nội - Quảng Ninh", "18/10/2026 13:00", "18/10/2026 16:00", "29B-311.05", "Lê Văn Thắng", "160.000 ₫", "34", "5.120.000 ₫", "85.0%" },
            new[] { "SCH004", "Hà Nội - Đà Nẵng", "18/10/2026 18:00", "19/10/2026 08:00", "29B-402.19", "Nguyễn Văn Hùng", "450.000 ₫", "34", "15.300.000 ₫", "100.0%" },
            new[] { "SCH005", "Hà Nội - Nam Định", "18/10/2026 14:00", "18/10/2026 15:30", "29B-205.12", "Trần Đình Trọng", "100.000 ₫", "20", "2.000.000 ₫", "69.0%" },
            new[] { "SCH006", "Hà Nội - Sapa", "18/10/2026 21:00", "19/10/2026 03:00", "29B-311.05", "Đỗ Minh Quân", "280.000 ₫", "32", "8.960.000 ₫", "80.0%" },
            new[] { "SCH007", "Hà Nội - Hải Phòng", "18/10/2026 16:30", "18/10/2026 18:30", "29B-196.88", "Nguyễn Văn Hùng", "120.000 ₫", "36", "4.320.000 ₫", "90.0%" }
        };

        int rY = exY + 32;
        for (int r = 0; r < exRows.Length; r++)
        {
            using var rowBg = (r % 2 == 0) ? new SolidBrush(ColorTranslator.FromHtml("#F8FAFC")) : new SolidBrush(Color.White);
            g.FillRectangle(rowBg, 40, rY, 1200, 26);
            g.DrawLine(PBorder, 40, rY + 26, 1240, rY + 26);

            curExX = 50;
            var rData = exRows[r];
            for (int c = 0; c < rData.Length; c++)
            {
                g.DrawString(rData[c], FontSmall, BDark, curExX, rY + 6);
                curExX += exW[c];
            }
            rY += 26;
        }

        // Total row
        using (var b = new SolidBrush(YellowBg))
            g.FillRectangle(b, 40, rY, 1200, 28);
        using (var p = new Pen(Gold, 2f))
        {
            g.DrawLine(p, 40, rY, 1240, rY);
            g.DrawLine(p, 40, rY + 28, 1240, rY + 28);
        }
        g.DrawString("TỔNG CỘNG:", FontSmallB, BDark, 50, rY + 6);
        g.DrawString("220 vé", FontSmallB, BNavy, 800, rY + 6);
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#D97706")))
            g.DrawString("43.260.000 ₫", FontSmallB, b, 890, rY + 6);
        using (var b = new SolidBrush(Green))
            g.DrawString("88.4%", FontSmallB, b, 1030, rY + 6);

        SaveCanvas(bmp, g, outDir, "15-excel");
    }
}
