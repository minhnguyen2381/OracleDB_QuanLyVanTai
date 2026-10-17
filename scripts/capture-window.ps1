# Chụp cửa sổ chính của một tiến trình và lưu vào docs/images/<Name>.png
# Dùng: powershell -ExecutionPolicy Bypass -File scripts/capture-window.ps1 -Name 02-dashboard
param(
    [Parameter(Mandatory = $true)] [string] $Name,
    [string] $Process = "Transport.Presentation.Wpf"
)

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);

    public static IntPtr FindMainWindow(uint pid) {
        IntPtr found = IntPtr.Zero;
        EnumWindows((h, l) => {
            uint p;
            GetWindowThreadProcessId(h, out p);
            if (p == pid && IsWindowVisible(h)) {
                RECT r;
                GetWindowRect(h, out r);
                int w = r.Right - r.Left;
                int ht = r.Bottom - r.Top;
                if (w > 100 && ht > 100) {
                    found = h;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
'@

# Lấy toạ độ thật (không bị Windows phóng to theo DPI)
[Win32Capture]::SetProcessDPIAware() | Out-Null

$target = Get-Process -Name $Process -ErrorAction Stop | Select-Object -First 1
if (-not $target) { throw "No process found for '$Process'." }

$hWnd = [Win32Capture]::FindMainWindow($target.Id)
if ($hWnd -eq [IntPtr]::Zero) {
    $hWnd = $target.MainWindowHandle
}
if ($hWnd -eq [IntPtr]::Zero) { throw "No window found for process '$Process'." }

[Win32Capture]::SetForegroundWindow($hWnd) | Out-Null
Start-Sleep -Milliseconds 500

$rect = New-Object Win32Capture+RECT
[Win32Capture]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top

if ($width -le 0 -or $height -le 0) {
    throw "Invalid window size: $width x $height"
}

$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)

$folder = Join-Path $PSScriptRoot "..\docs\images"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$output = Join-Path $folder "$Name.png"
$bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bitmap.Dispose()
"Saved $output ($width x $height)"
