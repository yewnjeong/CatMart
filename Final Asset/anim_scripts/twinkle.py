# Star / lamp twinkle: emission strength *= lo + (1-lo) * (0.5 + 0.5*sin(frame*speed + random*2pi))
# Each object gets its own phase from Object Info > Random. Re-running is safe.
import bpy, math

TARGETS = {
    # material: (speed rad/frame, lowest brightness factor)
    "URPLab_Baked_Star": (0.11, 0.45),
    "URPLab_Baked_Star1": (0.13, 0.4),
    "URPLab_Baked_Star1.002": (0.13, 0.4),
    "M_StarNightLight.001": (0.19, 0.3),
}


def twinkle(mat, speed, lo):
    nt = mat.node_tree
    N = nt.nodes
    bsdf = next(n for n in N if n.type == 'BSDF_PRINCIPLED')
    sock = bsdf.inputs["Emission Strength"]
    old = N.get("TW_mul")
    if old is not None and old.inputs[0].is_linked:      # restore the pre-twinkle source
        orig = old.inputs[0].links[0].from_socket
        if orig.node.name == "TW_base":
            sock.default_value = orig.node.outputs[0].default_value
        else:
            nt.links.new(orig, sock)
    for n in [n for n in N if n.name.startswith("TW_")]:
        N.remove(n)
    if sock.is_linked:
        src = sock.links[0].from_socket
    else:
        v = N.new("ShaderNodeValue"); v.name = "TW_base"
        v.outputs[0].default_value = sock.default_value
        src = v.outputs[0]
    fr = N.new("ShaderNodeValue"); fr.name = "TW_frame"
    fcu = fr.outputs[0].driver_add("default_value")
    fcu.driver.type = 'SCRIPTED'
    fcu.driver.expression = "frame"
    oi = N.new("ShaderNodeObjectInfo"); oi.name = "TW_obj"

    def math_node(op, a, b, name):
        m = N.new("ShaderNodeMath"); m.operation = op; m.name = name
        for i, x in enumerate((a, b)):
            if x is None:
                continue
            if isinstance(x, (int, float)):
                m.inputs[i].default_value = x
            else:
                nt.links.new(x, m.inputs[i])
        return m
    t = math_node('MULTIPLY', fr.outputs[0], speed, "TW_t")
    ph = math_node('MULTIPLY', oi.outputs["Random"], math.tau, "TW_phase")
    a = math_node('ADD', t.outputs[0], ph.outputs[0], "TW_angle")
    s = math_node('SINE', a.outputs[0], None, "TW_sin")
    k = math_node('MULTIPLY_ADD', s.outputs[0], (1 - lo) / 2, "TW_fac")
    k.inputs[2].default_value = (1 + lo) / 2
    out = math_node('MULTIPLY', src, k.outputs[0], "TW_mul")
    nt.links.new(out.outputs[0], sock)
    x0 = bsdf.location.x - 900
    for i, n in enumerate([fr, oi, t, ph, a, s, k, out]):
        n.location = (x0 + 110 * i, bsdf.location.y - 500)


done = []
for name, (sp, lo) in TARGETS.items():
    m = bpy.data.materials.get(name)
    if m and m.use_nodes:
        twinkle(m, sp, lo)
        done.append(name)
RESULT = {"twinkle": done}
