param([string]$OutDir)
Add-Type -AssemblyName PresentationCore, WindowsBase
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$W = [System.Windows.Media.Brushes]::White
$B = [System.Windows.Media.Brushes]::Black
function P($x, $y) { New-Object System.Windows.Point $x, $y }
function Pen($brush, $w) {
    $p = New-Object System.Windows.Media.Pen $brush, $w
    $p.StartLineCap = 'Round'; $p.EndLineCap = 'Round'; $p.LineJoin = 'Round'; $p
}

# Design space is 1024 x 1024.
$cx = 655; $cy = 630; $r = 185       # lens centre and radius
$frame = New-Object System.Windows.Rect 140, 160, 620, 480

function Draw-Picture($dc, [bool]$detail) {
    # Frame outline
    $dc.DrawRoundedRectangle($null, (Pen $W 58), $frame, 70, 70)
    # Mountains + sun, clipped to the inside of the frame
    $inner = New-Object System.Windows.Media.RectangleGeometry ([System.Windows.Rect]::Inflate($frame, -29, -29)), 42, 42
    $dc.PushClip($inner)
    $m = New-Object System.Windows.Media.StreamGeometry
    $c = $m.Open()
    $c.BeginFigure((P 215 660), $true, $true)
    $c.PolyLineTo([System.Windows.Point[]]@((P 360 400), (P 620 700)), $true, $true)
    $c.Close()
    $dc.DrawGeometry($W, $null, $m)
    if ($detail) { $dc.DrawEllipse($W, $null, (P 300 310), 52, 52) }
    $dc.Pop()
}

function Render([int]$size) {
    $v = New-Object System.Windows.Media.DrawingVisual
    $dc = $v.RenderOpen()
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 1024), ($size / 1024)))
    $detail = $size -ge 32

    # Black rounded tile
    $dc.DrawRoundedRectangle($B, $null, (New-Object System.Windows.Rect 32, 32, 960, 960), 210, 210)
    Draw-Picture $dc $detail

    # Black gap around the lens and handle, so they read as sitting on top of the picture
    $hs = P ($cx + $r * 0.7071) ($cy + $r * 0.7071)
    $he = P 850 825
    $dc.DrawLine((Pen $B 180), $hs, $he)
    $dc.DrawEllipse($B, $null, (P $cx $cy), ($r + 58), ($r + 58))

    # Lens ring and handle
    $dc.DrawEllipse($null, (Pen $W 58), (P $cx $cy), $r, $r)
    # Flat cap where the handle meets the ring, so it doesn't bulge into the lens
    $hp = Pen $W 100; $hp.StartLineCap = 'Flat'
    $dc.DrawLine($hp, $hs, $he)

    # Glint: a short arc inside the upper-left of the lens
    if ($detail) {
        $gr = $r - 72
        $a1 = [math]::PI * 1.08; $a2 = [math]::PI * 1.38
        $g = New-Object System.Windows.Media.StreamGeometry
        $c = $g.Open()
        $c.BeginFigure((P ($cx + $gr * [math]::Cos($a1)) ($cy + $gr * [math]::Sin($a1))), $false, $false)
        $c.ArcTo((P ($cx + $gr * [math]::Cos($a2)) ($cy + $gr * [math]::Sin($a2))), (New-Object System.Windows.Size $gr, $gr), 0, $false, 'Clockwise', $true, $true)
        $c.Close()
        $dc.DrawGeometry($null, (Pen $W 34), $g)
    }

    $dc.Pop(); $dc.Close()
    $bmp = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bmp.Render($v)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
    $ms = New-Object System.IO.MemoryStream
    $enc.Save($ms)
    $bytes = $ms.ToArray()
    [System.IO.File]::WriteAllBytes((Join-Path $OutDir "icon-$size.png"), $bytes)
    $bytes
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = @{}
foreach ($s in $sizes) { $pngs[$s] = [byte[]](Render $s) }
[void](Render 1024)

# Pack PNG frames into an .ico (PNG-compressed entries are supported since Vista).
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $ms
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
foreach ($s in $sizes) {
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$pngs[$s].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$s].Length
}
foreach ($s in $sizes) { $bw.Write($pngs[$s]) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $OutDir 'app.ico'), $ms.ToArray())
"wrote app.ico ($($ms.Length) bytes, sizes: $($sizes -join ', '))"
