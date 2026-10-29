# Report frames where a cat's footprint overlaps a prop footprint (oriented bbox in XY).
import bpy, math
from mathutils import Vector

OBSTACLES = ["Cart_A_Body", "Cart_B_Body", "Bench_A_Low.001", "Bench_A_Low.002",
             "Red Box_Low.001", "Yellow Box_Low.001", "Blue Box_Low.001", "Green Box_Low.002", "Brown Box_Low.001",
             "Star Box_Low.001", "Mail Box_Low.001", "Broom_Body", "Apple_F_Low.001", "Lollipop_Low.001",
             "texture_mapping.001", "texture_mapping.002", "Star1_Low.006", "Star_Low", "Garden_Low.004",
             "Bakery_Low.002", "Shopping Basket_Low.001", "Mint Bag_Low", "Fountain_Low.001",
             "Wooden Box_Low.003", "Wooden Box_Low.004", "Wooden Box_Low.005", "Pudding_Low_FBX",
             "Tree_Low.001", "Tree_Low.003", "Tree_Low.004", "Tree_F_Low.003"]
TRUNK = {"Tree_Low.001": 0.35, "Tree_Low.003": 0.25, "Tree_Low.004": 0.4, "Tree_F_Low.003": 0.35,
         "Lollipop_Low.001": 0.15, "texture_mapping.001": 0.2, "texture_mapping.002": 0.2, "Fountain_Low.001": 1.75}


def fcurves(obj):
    out = {}
    act = obj.animation_data.action if obj.animation_data else None
    if not act:
        return out
    for l in act.layers:
        for st in l.strips:
            for cb in st.channelbags:
                for fc in cb.fcurves:
                    out[(fc.data_path, fc.array_index)] = fc
    return out


def check(scene_name, tag, end, cat_r=0.2, step=2, ignore=(), moving_carts=False):
    sc = bpy.data.scenes[scene_name]
    bpy.context.window.scene = sc
    M = {o.get("src", o.name): o for o in sc.objects}
    obs = []
    for n in OBSTACLES:
        o = M.get(n)
        if o is None or n in ignore:
            continue
        obs.append(o)

    def footprint(o):
        if o.get("src", o.name) in TRUNK:
            c = sum((o.matrix_world @ Vector(v) for v in o.bound_box), Vector()) / 8
            return ("circle", c, TRUNK[o.get("src", o.name)])
        mi = o.matrix_world.inverted()
        bb = o.bound_box
        lo = Vector((min(v[0] for v in bb), min(v[1] for v in bb), min(v[2] for v in bb)))
        hi = Vector((max(v[0] for v in bb), max(v[1] for v in bb), max(v[2] for v in bb)))
        top = max((o.matrix_world @ Vector(v)).z for v in bb)
        return ("box", mi, (lo, hi), top, o.matrix_world.to_scale())

    fps = {o.name: footprint(o) for o in obs}
    cats = {}
    for w in ("Black", "White", "Rainbow"):
        mv = bpy.data.objects.get("%s_Mover_%s" % (tag, w))
        if mv:
            cats[w] = fcurves(mv)
    hits = []
    for f in range(1, end + 1, step):
        if moving_carts:
            sc.frame_set(f)
            for o in obs:
                if "Cart" in o.name:
                    fps[o.name] = footprint(o)
        for w, F in cats.items():
            x = F[("location", 0)].evaluate(f); y = F[("location", 1)].evaluate(f); z = F[("location", 2)].evaluate(f)
            if abs(x) > 20:
                continue
            for o in obs:
                fp = fps[o.name]
                if fp[0] == "circle":
                    if math.hypot(x - fp[1].x, y - fp[1].y) < fp[2] + cat_r:
                        hits.append((f, w, o.get("src", o.name)))
                else:
                    _, mi, (lo, hi), top, s = fp
                    if z > top - 0.05:      # standing on top of it
                        continue
                    r = cat_r / max(1e-6, min(abs(s.x), abs(s.y), abs(s.z)))
                    inside = False
                    for zz in (z + 0.1, z + 0.3, z + 0.5):
                        p = mi @ Vector((x, y, zz))
                        if all(lo[i] - r < p[i] < hi[i] + r for i in range(3)):
                            inside = True
                            break
                    if inside:
                        hits.append((f, w, o.get("src", o.name)))
    comp = []
    for f, w, n in hits:
        if comp and comp[-1][1] == w and comp[-1][2] == n and f - comp[-1][3] <= step * 3:
            comp[-1][3] = f
        else:
            comp.append([f, w, n, f])
    return comp
