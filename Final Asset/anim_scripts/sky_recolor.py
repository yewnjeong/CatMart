# Recolor the Unity dusk panorama into a Serenity / Rose Quartz pastel sky, keeping the clouds.
# Each row gets a target colour by elevation; cloud detail = luminance ratio to the row's mean.
# Writes Final Asset/HDRI/Sky_Serenity_RoseQuartz.png. Run inside Blender.
import bpy, os, numpy as np

HDRI = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "HDRI")
SRC = os.path.join(HDRI, "Sky_Anime_02_dusk_a.png")
DST = os.path.join(HDRI, "Sky_Serenity_RoseQuartz.png")

# (elevation deg, sRGB colour)
STOPS = [(-90, (236, 214, 224)), (-10, (244, 214, 218)), (0, (250, 220, 218)), (8, (247, 202, 201)),   # Rose Quartz
         (18, (214, 190, 214)), (28, (168, 178, 216)), (45, (146, 168, 209)),                         # Serenity
         (90, (128, 152, 204))]
CLOUD = np.array([255, 250, 248]) / 255.0


def lin(x): return np.where(x <= 0.04045, x / 12.92, ((x + 0.055) / 1.055) ** 2.4)
def enc(x): x = np.clip(x, 0, 1); return np.where(x <= 0.0031308, 12.92 * x, 1.055 * x ** (1 / 2.4) - 0.055)


def run():
    img = bpy.data.images.load(SRC, check_existing=True)
    W, H = img.size
    s = np.array(img.pixels[:], dtype=np.float32).reshape(H, W, 4)[:, :, :3]   # sRGB values
    L = lin(s) @ np.array([0.2126, 0.7152, 0.0722])                              # linear luminance
    # row "sky" luminance = median (clouds are a minority of each row)
    base = np.median(L, axis=1, keepdims=True) + 1e-4
    ratio = L / base
    elev = (np.arange(H) + 0.5) / H * 180.0 - 90.0                               # row 0 = bottom
    e = np.array([p[0] for p in STOPS], float)
    target = np.stack([np.interp(elev, e, [p[1][i] / 255.0 for p in STOPS]) for i in range(3)], axis=1)
    T = lin(target)[:, None, :]                                                  # H,1,3 linear
    # clouds brighter than the sky drift toward warm white, darker cloud undersides keep the sky hue
    k = np.clip((ratio - 1.0) / 1.5, 0, 1)[..., None]
    out = T * np.clip(ratio, 0.82, 1.0)[..., None] + (lin(CLOUD) - T) * k
    out = enc(out)
    rgba = np.concatenate([out, np.ones((H, W, 1), np.float32)], axis=2).astype(np.float32)
    o = bpy.data.images.get("Sky_Serenity_RoseQuartz.png") or bpy.data.images.new("Sky_Serenity_RoseQuartz.png", W, H)
    if tuple(o.size) != (W, H):
        o.scale(W, H)
    o.pixels.foreach_set(rgba.ravel())
    o.filepath_raw = DST
    o.file_format = 'PNG'
    o.save()
    o.reload()
    return DST


RESULT = run()
