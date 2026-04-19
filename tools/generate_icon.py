"""Generate app icon for HonestTimeTracker."""
import os
import math
from PIL import Image, ImageDraw, ImageFont

SIZES = [16, 32, 48, 64, 128, 256]
COLOR_GRAD_START = (251, 199, 7)   # #fbc707
COLOR_GRAD_END   = (253, 221, 4)   # #fddd04
COLOR_TEXT       = (20, 17, 17)    # #141111 — same as logo

FONT_NAMES = [
    "E:/OneDrive/Honest IT/Dokumenty/Księga CI/Exo/Exo-ExtraBold.ttf",
    "E:/OneDrive/Honest IT/Dokumenty/Księga CI/Exo/Exo-Bold.ttf",
    "C:/Windows/Fonts/Exo2-Bold.ttf",
    "C:/Windows/Fonts/Exo-Bold.ttf",
]
FALLBACK_FONTS = [
    "C:/Windows/Fonts/arialbd.ttf",
    "C:/Windows/Fonts/calibrib.ttf",
    "C:/Windows/Fonts/segoeui.ttf",
]


def find_font():
    for path in FONT_NAMES + FALLBACK_FONTS:
        if os.path.exists(path):
            return path
    return None


def gradient_background(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    for x in range(size):
        t = x / max(size - 1, 1)
        r = int(COLOR_GRAD_START[0] + t * (COLOR_GRAD_END[0] - COLOR_GRAD_START[0]))
        g = int(COLOR_GRAD_START[1] + t * (COLOR_GRAD_END[1] - COLOR_GRAD_START[1]))
        b = int(COLOR_GRAD_START[2] + t * (COLOR_GRAD_END[2] - COLOR_GRAD_START[2]))
        draw.line([(x, 0), (x, size - 1)], fill=(r, g, b, 255))
    return img


def rounded_mask(size: int, radius_ratio: float = 0.18) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    r = int(size * radius_ratio)
    draw.rounded_rectangle([0, 0, size - 1, size - 1], radius=r, fill=255)
    return mask


def draw_text_centered(draw: ImageDraw.ImageDraw, text: str, size: int, font):
    bbox = draw.textbbox((0, 0), text, font=font)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    x = (size - tw) // 2 - bbox[0]
    y = (size - th) // 2 - bbox[1]
    draw.text((x, y), text, fill=COLOR_TEXT, font=font)


def make_icon_frame(size: int, font_path: str | None) -> Image.Image:
    bg = gradient_background(size)
    mask = rounded_mask(size)

    frame = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    frame.paste(bg, mask=mask)

    draw = ImageDraw.Draw(frame)

    if size >= 32 and font_path:
        font_size = int(size * 0.52)
        try:
            font = ImageFont.truetype(font_path, font_size)
        except Exception:
            font = ImageFont.load_default()
        draw_text_centered(draw, "HT", size, font)
    elif size >= 16 and font_path:
        font_size = max(int(size * 0.55), 8)
        try:
            font = ImageFont.truetype(font_path, font_size)
        except Exception:
            font = ImageFont.load_default()
        draw_text_centered(draw, "H", size, font)
    else:
        # tiny: just fill with brand color, no text
        pass

    return frame


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    out_path = os.path.join(script_dir, "..", "src", "HonestTimeTracker.Desktop", "app.ico")
    out_path = os.path.normpath(out_path)

    font_path = find_font()
    if font_path:
        print(f"Using font: {font_path}")
    else:
        print("No Exo/Arial font found, using default")

    frames = []
    for size in SIZES:
        frame = make_icon_frame(size, font_path)
        frames.append(frame.convert("RGBA"))

    base = frames[0]
    rest = frames[1:]
    base.save(out_path, format="ICO", sizes=[(s, s) for s in SIZES], append_images=rest)
    print(f"Saved: {out_path}")


if __name__ == "__main__":
    main()
