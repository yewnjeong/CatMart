# PNG sequence -> H.264 mp4 with Blender's sequencer (no ffmpeg install needed).
# blender -b --factory-startup --python encode_mp4.py -- <png_dir> <out.mp4> [fps]
import bpy, os, sys

argv = sys.argv[sys.argv.index("--") + 1:]
src, out = os.path.abspath(argv[0]), os.path.abspath(argv[1])   # no blend file: relative paths crash
fps = int(argv[2]) if len(argv) > 2 else 30
files = sorted(f for f in os.listdir(src) if f.lower().endswith(".png"))
if not files:
    raise SystemExit("no PNGs in " + src)

sc = bpy.context.scene
sc.sequence_editor_create()
seq = sc.sequence_editor
strips = seq.strips if hasattr(seq, "strips") else seq.sequences
st = strips.new_image("frames", os.path.join(src, files[0]), channel=1, frame_start=1)
for f in files[1:]:
    st.elements.append(f)
r = sc.render
r.fps = fps
r.resolution_x, r.resolution_y, r.resolution_percentage = 1920, 1080, 100
sc.frame_start, sc.frame_end = 1, len(files)
im = r.image_settings
im.media_type = 'VIDEO'
im.file_format = 'FFMPEG'
r.ffmpeg.format = 'MPEG4'
r.ffmpeg.codec = 'H264'
r.ffmpeg.constant_rate_factor = 'HIGH'
r.ffmpeg.ffmpeg_preset = 'GOOD'
r.ffmpeg.audio_codec = 'NONE'
sc.view_settings.view_transform = 'Standard'   # frames are already graded; don't tone-map twice
r.filepath = out
bpy.ops.render.render(animation=True)
print("ENCODED", out, len(files), "frames")
