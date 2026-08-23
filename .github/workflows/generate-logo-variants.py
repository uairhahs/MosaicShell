# Generate logo variants from .github/res/MosaicShell.png (source of truth).
# Run from repo root: python .github/workflows/generate-logo-variants.py

from pathlib import Path
from PIL import Image
import shutil

repo = Path(__file__).resolve().parents[2]
src = Image.open(repo / '.github/res/MosaicShell.png').convert('RGBA')
out = repo / '.github/res/logo-variants'

for size in [512, 256, 128, 64, 32]:
    src.resize((size, size), Image.LANCZOS).save(out / f'compact-{size}.png')

for size in [24, 16]:
    src.resize((size, size), Image.LANCZOS).save(out / f'micro-{size}.png')

def monochrome(img, color):
    r, g, b = color
    result = Image.new('RGBA', img.size)
    px_in = img.load()
    px_out = result.load()
    for y in range(img.height):
        for x in range(img.width):
            a = px_in[x, y][3]
            px_out[x, y] = (r, g, b, a)
    return result

for size in [512, 256, 128, 64]:
    resized = src.resize((size, size), Image.LANCZOS)
    monochrome(resized, (255, 255, 255)).save(out / f'monochrome-white-{size}.png')
    monochrome(resized, (11, 16, 32)).save(out / f'monochrome-dark-{size}.png')

ico_sizes = [256, 128, 64, 48, 32, 24, 16]
ico_imgs = [src.resize((s, s), Image.LANCZOS).convert('RGBA') for s in ico_sizes]
favicon = out / 'favicon.ico'
ico_imgs[0].save(
    favicon,
    format='ICO',
    sizes=[(s, s) for s in ico_sizes],
    append_images=ico_imgs[1:],
)

host_ico = repo / 'host/MosaicShell.Host/Assets/mosaicshell.ico'
host_ico.parent.mkdir(parents=True, exist_ok=True)
shutil.copy(favicon, host_ico)

print(f'Wrote variants under {out}')
print(f'Synced Host icon to {host_ico}')
