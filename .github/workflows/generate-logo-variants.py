# This is a simple script to generate the various logo variants used in the docs and README. It is run automatically by the GitHub Actions workflow on push to main, but can also be run manually if needed.

from PIL import Image

src = Image.open('.github/docs/MosaicShell.png').convert('RGBA')
out = '.github/docs/logo-variants'

for size in [512, 256, 128, 64, 32]:
    src.resize((size, size), Image.LANCZOS).save(f'{out}/compact-{size}.png')

for size in [24, 16]:
    src.resize((size, size), Image.LANCZOS).save(f'{out}/micro-{size}.png')

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
    monochrome(resized, (255, 255, 255)).save(f'{out}/monochrome-white-{size}.png')
    monochrome(resized, (11, 16, 32)).save(f'{out}/monochrome-dark-{size}.png')

ico_imgs = [src.resize((s, s), Image.LANCZOS).convert('RGBA') for s in [16, 32]]
ico_imgs[0].save(f'{out}/favicon.ico', format='ICO', sizes=[(16, 16), (32, 32)], append_images=[ico_imgs[1]])

print('Done')
