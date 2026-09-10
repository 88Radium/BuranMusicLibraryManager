#!/usr/bin/env python3
"""Render Buran Library-tab UI concepts as PNG mockups."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent
LOGO = Path(__file__).resolve().parents[1] / "BuranUI" / "Assets" / "Buran.png"
FONT_DIR = Path("/usr/share/fonts/google-noto")

W, H = 1600, 920
CAP = 36
APP_Y = CAP
APP_H = H - CAP

BG = (11, 18, 32, 255)
HEADER = (7, 12, 22, 255)
SIDEBAR = (7, 12, 22, 240)
WORK = (18, 26, 44, 230)
BORDER = (42, 59, 88, 255)
TEXT = (232, 238, 248, 255)
MUTED = (154, 168, 188, 255)
ACCENT = (94, 200, 240, 255)
ACCENT_DIM = (94, 200, 240, 40)
PLAYING = (26, 58, 74, 255)
SELECTED = (36, 54, 86, 255)
ROW_ALT = (18, 26, 44, 180)
CHIP = (24, 38, 58, 255)
DANGER = (255, 120, 120, 255)
BTN = (18, 26, 44, 255)
BTN_BORDER = BORDER
EDIT_ON = (42, 120, 110, 255)

TRACKS = [
    dict(play=True,  sel=False, title="Lose Yourself", artists="Eminem", album="8 Mile", year="2002",
         dur="5:26", br="320", sr="44.1 kHz", depth="16", genre="Hip-Hop", mood="Aggressive",
         file="Eminem - Lose Yourself.mp3", fmt="MP3"),
    dict(play=False, sel=True,  title="Stan", artists="Eminem feat. Dido", album="The Marshall Mathers LP", year="2000",
         dur="6:44", br="320", sr="44.1 kHz", depth="16", genre="Hip-Hop", mood="Dark",
         file="Eminem - Stan (feat. Dido).mp3", fmt="MP3"),
    dict(play=False, sel=False, title="Purple Pills", artists="D12", album="Devil's Night", year="2001",
         dur="5:05", br="256", sr="44.1 kHz", depth="16", genre="Rap", mood="Playful",
         file="D12 - Purple Pills.mp3", fmt="MP3"),
    dict(play=False, sel=False, title="The Way I Am", artists="Eminem", album="The Marshall Mathers LP", year="2000",
         dur="4:50", br="FLAC", sr="192 kHz", depth="24", genre="Rap", mood="Intense",
         file="Eminem - The Way I Am.flac", fmt="FLAC"),
    dict(play=False, sel=False, title="Without Me", artists="Eminem", album="The Eminem Show", year="2002",
         dur="4:50", br="320", sr="44.1 kHz", depth="16", genre="Hip-Hop", mood="Cocky",
         file="Eminem - Without Me.mp3", fmt="MP3"),
    dict(play=False, sel=False, title="My Name Is", artists="Eminem", album="The Slim Shady LP", year="1999",
         dur="4:28", br="192", sr="44.1 kHz", depth="16", genre="Rap", mood="Humorous",
         file="Eminem - My Name Is.mp3", fmt="MP3"),
    dict(play=False, sel=False, title="In da Club", artists="50 Cent", album="Get Rich or Die Tryin'", year="2003",
         dur="3:13", br="320", sr="48 kHz", depth="16", genre="Rap", mood="Party",
         file="50 Cent - In da Club.mp3", fmt="MP3"),
    dict(play=False, sel=False, title="Forgot About Dre", artists="Dr. Dre feat. Eminem", album="2001", year="1999",
         dur="3:42", br="320", sr="44.1 kHz", depth="16", genre="Rap", mood="Aggressive",
         file="Dr. Dre - Forgot About Dre (feat. Eminem).mp3", fmt="MP3"),
]


def font(size, weight="Regular"):
    return ImageFont.truetype(str(FONT_DIR / f"NotoSans-{weight}.ttf"), size)


def fit(draw, text, fnt, max_w):
    if draw.textlength(text, font=fnt) <= max_w:
        return text
    ell = "…"
    while text and draw.textlength(text + ell, font=fnt) > max_w:
        text = text[:-1]
    return text + ell


def rr(draw, xy, r, fill=None, outline=None, width=1):
    draw.rounded_rectangle(xy, radius=r, fill=fill, outline=outline, width=width)


def text(draw, xy, s, fnt, fill=TEXT, anchor="lt"):
    draw.text(xy, s, font=fnt, fill=fill, anchor=anchor)


def line(draw, a, b, fill=BORDER, width=1):
    draw.line([a, b], fill=fill, width=width)


def load_logo(size=28):
    im = Image.open(LOGO).convert("RGBA")
    im.thumbnail((size, size), Image.Resampling.LANCZOS)
    return im


def spectrogram(w, h, playhead=0.32):
    img = Image.new("RGBA", (w, h), (7, 12, 22, 255))
    px = img.load()
    stops = [
        (0.00, (16, 16, 40)),
        (0.25, (80, 80, 255)),
        (0.45, (80, 255, 255)),
        (0.62, (80, 255, 80)),
        (0.80, (255, 255, 100)),
        (1.00, (255, 80, 80)),
    ]

    def col(t):
        t = max(0, min(1, t))
        for i in range(len(stops) - 1):
            a, ca = stops[i]
            b, cb = stops[i + 1]
            if a <= t <= b:
                u = 0 if b == a else (t - a) / (b - a)
                return tuple(int(ca[k] + (cb[k] - ca[k]) * u) for k in range(3))
        return stops[-1][1]

    import math, random
    rng = random.Random(7)
    for x in range(w):
        nx = x / max(1, w - 1)
        energy = 0.25 + 0.55 * abs(math.sin(nx * 14.0)) * (0.4 + 0.6 * abs(math.sin(nx * 3.1)))
        energy *= 0.65 + 0.35 * rng.random()
        for y in range(h):
            # log-ish: more detail in lower half of image (high freq at top)
            fy = 1 - (y / max(1, h - 1))
            band = energy * (0.35 + 0.65 * fy ** 1.4)
            noise = 0.08 * rng.random()
            t = min(1.0, band + noise)
            c = col(t)
            fade = 0.25 + 0.75 * t
            px[x, y] = (int(c[0] * fade), int(c[1] * fade), int(c[2] * fade), 255)
    d = ImageDraw.Draw(img)
    xh = int(w * playhead)
    d.line([(xh, 0), (xh, h)], fill=(232, 244, 255, 230), width=2)
    return img


def draw_caption(img, title):
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, CAP], fill=(6, 10, 18, 255))
    text(d, (16, CAP / 2), title, font(13, "Medium"), MUTED, "lm")


def draw_chrome(img, tab="library"):
    d = ImageDraw.Draw(img)
    y0 = APP_Y
    d.rectangle([0, y0, W, H], fill=BG)
    # header
    d.rectangle([0, y0, W, y0 + 56], fill=HEADER)
    line(d, (0, y0 + 56), (W, y0 + 56))
    logo = load_logo(28)
    img.paste(logo, (16, y0 + 14), logo)
    text(d, (52, y0 + 16), "BURAN", font(16, "SemiBold"), TEXT)
    text(d, (52, y0 + 36), "Music library manager", font(11), MUTED)
    rr(d, (W - 118, y0 + 14, W - 16, y0 + 42), 6, fill=BTN, outline=BTN_BORDER)
    text(d, (W - 67, y0 + 28), "Settings", font(12), MUTED, "mm")

    # sidebar
    sw = 260
    d.rectangle([0, y0 + 56, sw, H], fill=SIDEBAR)
    line(d, (sw, y0 + 56), (sw, H))
    text(d, (16, y0 + 72), "LIBRARY", font(11, "SemiBold"), MUTED)
    text(d, (16, y0 + 94), "/Music/Hip-Hop", font(11), MUTED)
    rr(d, (16, y0 + 116, 168, y0 + 142), 6, fill=(20, 90, 110, 255), outline=ACCENT)
    text(d, (92, y0 + 129), "Choose folder", font(11, "Medium"), TEXT, "mm")
    rr(d, (176, y0 + 116, 244, y0 + 142), 6, fill=BTN, outline=BTN_BORDER)
    text(d, (210, y0 + 129), "Refresh", font(11), MUTED, "mm")

    folders = [
        ("Music", 0, False),
        ("Hip-Hop", 1, True),
        ("Eminem", 2, True),
        ("D12", 2, True),
        ("50 Cent", 2, True),
        ("Electronic", 1, False),
        ("Classical", 1, False),
    ]
    fy = y0 + 160
    for name, depth, audio in folders:
        x = 16 + depth * 16
        if name == "Eminem":
            d.rectangle([8, fy - 4, sw - 8, fy + 18], fill=SELECTED)
        text(d, (x, fy), name, font(12, "Medium" if name == "Eminem" else "Regular"), TEXT)
        if audio:
            d.ellipse([sw - 22, fy + 2, sw - 12, fy + 12], fill=ACCENT)
        fy += 24
    text(d, (16, H - 22), "Eminem  ·  8 files", font(11), MUTED)

    # workspace tabs
    wx = sw + 1
    tabs_y = y0 + 56
    d.rectangle([wx, tabs_y, W, tabs_y + 40], fill=HEADER)
    line(d, (wx, tabs_y + 40), (W, tabs_y + 40))
    tabs = [("Library", "library"), ("Catalog", "catalog")]
    tx = wx + 16
    for label, key in tabs:
        active = key == tab
        tw = 88
        if active:
            d.rectangle([tx, tabs_y + 8, tx + tw, tabs_y + 40], fill=WORK)
            line(d, (tx, tabs_y + 39), (tx + tw, tabs_y + 39), fill=ACCENT, width=2)
            text(d, (tx + tw / 2, tabs_y + 24), label, font(13, "SemiBold"), TEXT, "mm")
        else:
            text(d, (tx + tw / 2, tabs_y + 24), label, font(13), MUTED, "mm")
        tx += tw + 8
    return wx, tabs_y + 40


def draw_toolbar(d, x, y, w, mode="listen", extra=None):
    h = 44
    d.rectangle([x, y, x + w, y + h], fill=WORK)
    line(d, (x, y + h), (x + w, y + h))
    # mode toggle
    edit_on = mode != "listen"
    rr(d, (x + 12, y + 8, x + 108, y + 36), 6,
       fill=EDIT_ON if edit_on else BTN, outline=ACCENT if edit_on else BTN_BORDER)
    text(d, (x + 60, y + 22), "Edit tags" if not edit_on else "Editing",
         font(12, "Medium"), TEXT, "mm")

    rr(d, (x + 118, y + 8, x + 210, y + 36), 6, fill=BTN, outline=BTN_BORDER)
    text(d, (x + 164, y + 22), "Columns ▾", font(12), MUTED, "mm")

    rr(d, (x + 220, y + 8, x + 360, y + 36), 6, fill=BTN, outline=BTN_BORDER)
    text(d, (x + 290, y + 22), "Queue: Folder ▾", font(12), MUTED, "mm")

    rr(d, (x + 370, y + 8, x + 508, y + 36), 6, fill=BTN, outline=BTN_BORDER)
    text(d, (x + 439, y + 22), "Playlist: Night ▾", font(12), MUTED, "mm")

    actions = ["Select all", "To playlist", "ID3 ↔ name"]
    if edit_on:
        actions = ["Select all", "Bulk artists", "Bulk genres", "Bulk moods", "Comments", "ID3 ↔ name"]
    ax = x + 520
    for a in actions:
        tw = int(d.textlength(a, font=font(12))) + 20
        accent_bulk = a.startswith("Bulk") or a == "Comments"
        rr(d, (ax, y + 8, ax + tw, y + 36), 6,
           fill=(20, 90, 110, 255) if accent_bulk else BTN,
           outline=ACCENT if accent_bulk else BTN_BORDER)
        text(d, (ax + tw / 2, y + 22), a, font(12), TEXT if accent_bulk else MUTED, "mm")
        ax += tw + 8

    if extra:
        extra(d, x, y, w, h)
    return y + h


def draw_column_menu(d, x, y):
    mx, my = x + 118, y + 40
    items = [
        ("Title", True), ("Artists", True), ("Album", True), ("Year", True),
        ("Duration", True), ("Bitrate", True), ("Sample rate", False),
        ("Bit depth", False), ("Genre", False), ("Mood", False),
        ("Filename", False), ("Format", False),
    ]
    h = 16 + 22 * len(items)
    rr(d, (mx, my, mx + 180, my + h), 8, fill=HEADER, outline=BORDER)
    yy = my + 10
    for label, on in items:
        box = [mx + 12, yy + 3, mx + 24, yy + 15]
        rr(d, box, 3, fill=ACCENT if on else BTN, outline=ACCENT if on else BORDER)
        if on:
            text(d, (mx + 18, yy + 9), "✓", font(10, "Bold"), HEADER, "mm")
        text(d, (mx + 32, yy + 2), label, font(12), TEXT)
        yy += 22


def draw_table(d, x, y, w, h, columns, multi_sel=False):
    d.rectangle([x, y, x + w, y + h], fill=WORK)
    header_h = 28
    d.rectangle([x, y, x + w, y + header_h], fill=HEADER)
    line(d, (x, y + header_h), (x + w, y + header_h))
    # column layout
    total = sum(c[1] for c in columns)
    gutter = 28
    xs = [x + gutter]
    for _, frac in columns:
        xs.append(xs[-1] + int((w - gutter - 8) * frac / total))
    xs[-1] = x + w - 8
    for i, (name, _) in enumerate(columns):
        text(d, (xs[i] + 6, y + 14), name, font(11, "SemiBold"), MUTED, "lm")
        if i:
            line(d, (xs[i], y + 4), (xs[i], y + header_h - 4), fill=(42, 59, 88, 120))

    row_h = 32
    visible = min(len(TRACKS), (h - header_h) // row_h)
    for i in range(visible):
        t = TRACKS[i]
        ry = y + header_h + i * row_h
        bg = PLAYING if t["play"] else (SELECTED if (t["sel"] or (multi_sel and i < 3)) else (ROW_ALT if i % 2 else WORK))
        d.rectangle([x, ry, x + w, ry + row_h], fill=bg)
        if t["play"]:
            d.polygon([(x + 10, ry + 10), (x + 10, ry + 22), (x + 20, ry + 16)], fill=ACCENT)
        elif t["sel"] or (multi_sel and i < 3):
            rr(d, (x + 10, ry + 11, x + 20, ry + 21), 2, fill=ACCENT, outline=ACCENT)
        else:
            rr(d, (x + 10, ry + 11, x + 20, ry + 21), 2, fill=BTN, outline=BORDER)
        values = {
            "Title": t["title"], "Artists": t["artists"], "Album": t["album"], "Year": t["year"],
            "Duration": t["dur"], "Bitrate": t["br"], "Sample rate": t["sr"], "Bit depth": t["depth"],
            "Genre": t["genre"], "Mood": t["mood"], "Filename": t["file"], "Format": t["fmt"],
            "": "",
        }
        for ci, (name, _) in enumerate(columns):
            cw = xs[ci + 1] - xs[ci] - 12
            if name == "":
                continue
            val = fit(d, values[name], font(12, "Medium" if t["play"] and name == "Title" else "Regular"), cw)
            fill = ACCENT if t["play"] and name == "Title" else TEXT
            text(d, (xs[ci] + 6, ry + 16), val, font(12, "Medium" if t["play"] and name == "Title" else "Regular"), fill, "lm")
        line(d, (x, ry + row_h), (x + w, ry + row_h), fill=(42, 59, 88, 80))


def draw_player(img, x, y, w, h, spectro=True):
    d = ImageDraw.Draw(img)
    d.rectangle([x, y, x + w, y + h], fill=HEADER)
    line(d, (x, y), (x + w, y))
    # transport
    bx = x + 16
    for i in range(4):
        fill = (20, 90, 110, 255) if i == 1 else BTN
        outline = ACCENT if i == 1 else BTN_BORDER
        rr(d, (bx, y + 10, bx + 36, y + 38), 6, fill=fill, outline=outline)
        cx, cy = bx + 18, y + 24
        if i == 0:  # previous
            d.polygon([(cx + 8, cy - 7), (cx + 8, cy + 7), (cx - 2, cy)], fill=TEXT)
            d.rectangle([cx - 8, cy - 7, cx - 5, cy + 7], fill=TEXT)
        elif i == 1:  # pause
            d.rectangle([cx - 6, cy - 7, cx - 2, cy + 7], fill=TEXT)
            d.rectangle([cx + 2, cy - 7, cx + 6, cy + 7], fill=TEXT)
        elif i == 2:  # stop
            d.rectangle([cx - 6, cy - 6, cx + 6, cy + 6], fill=TEXT)
        else:  # next
            d.polygon([(cx - 8, cy - 7), (cx - 8, cy + 7), (cx + 2, cy)], fill=TEXT)
            d.rectangle([cx + 5, cy - 7, cx + 8, cy + 7], fill=TEXT)
        bx += 42
    text(d, (bx + 8, y + 14), "Eminem  –  Lose Yourself", font(13, "SemiBold"), TEXT)
    text(d, (bx + 8, y + 32), "1:42  /  5:26", font(11), MUTED)
    text(d, (x + w - 210, y + 24), "Volume", font(11), MUTED, "lm")
    rr(d, (x + w - 150, y + 20, x + w - 24, y + 28), 4, fill=BTN, outline=BORDER)
    d.rectangle([x + w - 150, y + 20, x + w - 70, y + 28], fill=ACCENT)

    if spectro and h > 56:
        spec = spectrogram(w - 24, h - 52, 0.32)
        img.paste(spec, (x + 12, y + 46))
        d = ImageDraw.Draw(img)
        text(d, (x + 20, y + 52), "Hz", font(9), MUTED)
        text(d, (x + w - 40, y + 52), "dB", font(9), MUTED)


def draw_chip_row(d, x, y, w, label, chips, placeholder):
    text(d, (x, y), label, font(10, "SemiBold"), MUTED)
    cx, cy = x, y + 16
    for c in chips:
        tw = int(d.textlength(c, font=font(11))) + 28
        if cx + tw > x + w:
            cx, cy = x, cy + 28
        rr(d, (cx, cy, cx + tw, cy + 22), 11, fill=CHIP, outline=BORDER)
        text(d, (cx + 10, cy + 11), c, font(11), TEXT, "lm")
        text(d, (cx + tw - 10, cy + 11), "×", font(11), MUTED, "mm")
        cx += tw + 6
    add_y = cy + 28
    rr(d, (x, add_y, x + w - 36, add_y + 26), 6, fill=BTN, outline=BORDER)
    text(d, (x + 10, add_y + 13), placeholder, font(11), MUTED, "lm")
    rr(d, (x + w - 30, add_y, x + w, add_y + 26), 6, fill=(20, 90, 110, 255), outline=ACCENT)
    text(d, (x + w - 15, add_y + 13), "+", font(13, "Bold"), TEXT, "mm")
    return add_y + 34


def draw_inspector(d, x, y, w, h, multi=False):
    d.rectangle([x, y, x + w, y + h], fill=HEADER)
    line(d, (x, y), (x, y + h))
    pad = 14
    if multi:
        text(d, (x + pad, y + 16), "Focus in selection", font(11, "SemiBold"), MUTED)
        text(d, (x + pad, y + 38), "Stan", font(16, "SemiBold"), TEXT)
        text(d, (x + pad, y + 60), "1 of 3 selected", font(12), ACCENT)
    else:
        text(d, (x + pad, y + 16), "Selected track", font(11, "SemiBold"), MUTED)
        text(d, (x + pad, y + 38), "Stan", font(16, "SemiBold"), TEXT)
        text(d, (x + pad, y + 60), "Eminem feat. Dido", font(12), MUTED)

    def field(label, value, fx, fy, fw):
        text(d, (fx, fy), label, font(10, "SemiBold"), MUTED)
        rr(d, (fx, fy + 14, fx + fw, fy + 38), 6, fill=BTN, outline=BORDER)
        text(d, (fx + 10, fy + 26), value, font(12), TEXT, "lm")

    inner = w - 2 * pad
    field("Title", "Stan", x + pad, y + 80, inner)
    field("Album", "The Marshall Mathers LP", x + pad, y + 124, inner)
    field("Year", "2000", x + pad, y + 168, 90)
    field("Comment", "", x + pad + 100, y + 168, inner - 100)

    iy = draw_chip_row(d, x + pad, y + 220, inner, "Artists",
                       ["Eminem", "Dido"], "Add artist…")
    iy = draw_chip_row(d, x + pad, iy + 8, w - 2 * pad, "Genres",
                       ["Hip-Hop"], "Add genre…")
    iy = draw_chip_row(d, x + pad, iy + 8, w - 2 * pad, "Moods",
                       ["Dark"], "Add mood…")

    rr(d, (x + pad, y + h - 48, x + w / 2 - 6, y + h - 16), 6, fill=BTN, outline=BORDER)
    text(d, ((x + pad + x + w / 2 - 6) / 2, y + h - 32), "From filename", font(11), MUTED, "mm")
    rr(d, (x + w / 2 + 2, y + h - 48, x + w - pad, y + h - 16), 6, fill=BTN, outline=BORDER)
    text(d, ((x + w / 2 + 2 + x + w - pad) / 2, y + h - 32), "Reset", font(11), MUTED, "mm")


def draw_edit_card(d, x, y, w, h, track, selected=False):
    rr(d, (x, y, x + w, y + h), 8, fill=SELECTED if selected else HEADER, outline=ACCENT if selected else BORDER)
    # checkbox
    rr(d, (x + 10, y + 14, x + 26, y + 30), 3, fill=ACCENT if selected else BTN, outline=ACCENT if selected else BORDER)
    # artists column
    text(d, (x + 40, y + 10), "Artists", font(10, "SemiBold"), MUTED)
    ay = y + 28
    for a in track["artists"].split(" feat. "):
        tw = int(d.textlength(a, font=font(11))) + 10
        text(d, (x + 40, ay), a, font(11), TEXT)
        text(d, (x + 40 + tw + 8, ay), "×", font(11), MUTED)
        ay += 18
    rr(d, (x + 40, y + h - 28, x + 150, y + h - 10), 4, fill=BTN, outline=BORDER)
    text(d, (x + 48, y + h - 19), "+ artist", font(10), MUTED, "lm")

    def mini(label, value, fx, fy, fw):
        text(d, (fx, fy), label, font(10, "SemiBold"), MUTED)
        rr(d, (fx, fy + 14, fx + fw, fy + 36), 4, fill=BTN, outline=BORDER)
        text(d, (fx + 8, fy + 25), value, font(11), TEXT, "lm")

    mini("Title", track["title"], x + 170, y + 8, 220)
    mini("Release year", track["year"], x + 400, y + 8, 80)
    mini("Album", track["album"], x + 170, y + 50, 220)
    mini("Filename", track["file"], x + 170, y + 92, 310)

    text(d, (x + 500, y + 10), "Genre", font(10, "SemiBold"), MUTED)
    text(d, (x + 500, y + 28), track["genre"], font(11), TEXT)
    text(d, (x + 500, y + 50), "Mood", font(10, "SemiBold"), MUTED)
    text(d, (x + 500, y + 68), track["mood"], font(11), TEXT)

    rr(d, (x + w - 118, y + 12, x + w - 16, y + 36), 4, fill=BTN, outline=BORDER)
    text(d, (x + w - 67, y + 24), "ID3 → name", font(10), MUTED, "mm")
    rr(d, (x + w - 118, y + 42, x + w - 16, y + 66), 4, fill=BTN, outline=BORDER)
    text(d, (x + w - 67, y + 54), "name → ID3", font(10), MUTED, "mm")
    rr(d, (x + w - 118, y + 72, x + w - 16, y + 96), 4, fill=BTN, outline=BORDER)
    text(d, (x + w - 67, y + 84), "Reset", font(10), MUTED, "mm")


def draw_bulk_bar(d, x, y, w):
    h = 40
    d.rectangle([x, y, x + w, y + h], fill=(20, 40, 48, 255))
    line(d, (x, y + h), (x + w, y + h), fill=ACCENT)
    text(d, (x + 16, y + 20), "3 tracks selected", font(12, "SemiBold"), ACCENT, "lm")
    labels = ["Artists…", "Genres…", "Moods…", "Comments…", "Play", "To playlist"]
    ax = x + 180
    for lab in labels:
        tw = int(d.textlength(lab, font=font(11))) + 18
        rr(d, (ax, y + 8, ax + tw, y + 32), 6, fill=BTN, outline=BORDER)
        text(d, (ax + tw / 2, y + 20), lab, font(11), TEXT, "mm")
        ax += tw + 8
    return y + h


def save(img, name):
    path = ROOT / name
    img.save(path, "PNG")
    print("wrote", path)
    return path


def mock_listen():
    img = Image.new("RGBA", (W, H), (4, 8, 14, 255))
    draw_caption(img, "01   Hören  —  eine Trackliste, zuschaltbare Spalten, Player unten")
    wx, top = draw_chrome(img)
    d = ImageDraw.Draw(img)
    tool = draw_toolbar(d, wx, top, W - wx, "listen")
    player_h = 118
    table_h = H - tool - player_h
    cols = [("Title", 3), ("Artists", 3), ("Album", 3), ("Year", 1), ("Duration", 1.2), ("Bitrate", 1.2)]
    draw_table(d, wx, tool, W - wx, table_h, cols)
    draw_player(img, wx, H - player_h, W - wx, player_h, spectro=True)
    return save(img, "01-hoeren.png")


def mock_columns():
    img = Image.new("RGBA", (W, H), (4, 8, 14, 255))
    draw_caption(img, "02   Spalten  —  wie Explorer: Duration, Bitrate, Sample rate, Bit depth …")
    wx, top = draw_chrome(img)
    d = ImageDraw.Draw(img)
    tool = draw_toolbar(d, wx, top, W - wx, "listen")
    player_h = 118
    table_h = H - tool - player_h
    cols = [("Title", 2.4), ("Artists", 2.2), ("Year", 0.8), ("Duration", 1),
            ("Bitrate", 1), ("Sample rate", 1.3), ("Bit depth", 1), ("Format", 0.9)]
    draw_table(d, wx, tool, W - wx, table_h, cols)
    draw_player(img, wx, H - player_h, W - wx, player_h, spectro=True)
    draw_column_menu(d, wx, tool - 44)
    return save(img, "02-spalten.png")


def mock_cards():
    img = Image.new("RGBA", (W, H), (4, 8, 14, 255))
    draw_caption(img, "03   Deine Idee  —  Edit schaltet die Liste in ID3-Karten um")
    wx, top = draw_chrome(img)
    d = ImageDraw.Draw(img)
    tool = draw_toolbar(d, wx, top, W - wx, "edit")
    player_h = 52
    area_y = tool
    area_h = H - tool - player_h
    d.rectangle([wx, area_y, W, area_y + area_h], fill=WORK)
    cy = area_y + 10
    for i, t in enumerate(TRACKS[:3]):
        draw_edit_card(d, wx + 10, cy, W - wx - 20, 118, t, selected=(i == 1))
        cy += 126
    text(d, (wx + 16, cy + 8), "Weitere 5 Titel darunter …  —  Karten skalieren schlecht bei 200 Dateien.",
         font(12), MUTED)
    draw_player(img, wx, H - player_h, W - wx, player_h, spectro=False)
    return save(img, "03-bearbeiten-karten.png")


def mock_inspector():
    img = Image.new("RGBA", (W, H), (4, 8, 14, 255))
    draw_caption(img, "04   Edit, ein Track  —  Inspector mit Add-Feldern; Bulk in der Toolbar")
    wx, top = draw_chrome(img)
    d = ImageDraw.Draw(img)
    tool = draw_toolbar(d, wx, top, W - wx, "edit")
    player_h = 118
    insp_w = 340
    table_h = H - tool - player_h
    cols = [("Title", 3), ("Artists", 3), ("Album", 2.5), ("Year", 1), ("Duration", 1.1)]
    draw_table(d, wx, tool, W - wx - insp_w, table_h, cols)
    draw_inspector(d, W - insp_w, tool, insp_w, table_h, multi=False)
    draw_player(img, wx, H - player_h, W - wx, player_h, spectro=True)
    return save(img, "04-bearbeiten-inspektor.png")


def mock_inline():
    img = Image.new("RGBA", (W, H), (4, 8, 14, 255))
    draw_caption(img, "05   Edit, mehrere Tracks  —  Bulk-Leiste + Inspector (Add Artists/Genres/Moods bleibt)")
    wx, top = draw_chrome(img)
    d = ImageDraw.Draw(img)
    tool = draw_toolbar(d, wx, top, W - wx, "edit")
    bulk = draw_bulk_bar(d, wx, tool, W - wx)
    player_h = 52
    insp_w = 340
    table_h = H - bulk - player_h
    cols = [("Title", 3), ("Artists", 3), ("Album", 2.6), ("Year", 1), ("Duration", 1.1)]
    draw_table(d, wx, bulk, W - wx - insp_w, table_h, cols, multi_sel=True)
    draw_inspector(d, W - insp_w, bulk, insp_w, table_h, multi=True)
    draw_player(img, wx, H - player_h, W - wx, player_h, spectro=False)
    return save(img, "05-inline-bulk.png")


if __name__ == "__main__":
    mock_listen()
    mock_columns()
    mock_cards()
    mock_inspector()
    mock_inline()
