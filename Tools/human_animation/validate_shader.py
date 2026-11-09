"""Compile the engine-generated animation shader in a hidden SDL/OpenGL context.

Requires generated/native-shader.json from Robust's shader parser and a built client.
Does not start a game or create a visible window. This is a compiler check, not a runtime art test.
"""
import ctypes as ct
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Tools/human_animation/generated"


def main():
    parsed = json.loads((OUT / "native-shader.json").read_text(encoding="utf-8-sig"))
    sdl = ct.CDLL(str(ROOT / "bin/Content.Client/runtimes/win-x64/native/SDL3.dll"))

    def api(name, result, *args):
        fn = getattr(sdl, name)
        fn.restype, fn.argtypes = result, list(args)
        return fn

    error = api("SDL_GetError", ct.c_char_p)
    init = api("SDL_Init", ct.c_bool, ct.c_uint32)
    quit_sdl = api("SDL_Quit", None)
    attribute = api("SDL_GL_SetAttribute", ct.c_bool, ct.c_int, ct.c_int)
    create_window = api("SDL_CreateWindow", ct.c_void_p, ct.c_char_p, ct.c_int, ct.c_int, ct.c_uint64)
    destroy_window = api("SDL_DestroyWindow", None, ct.c_void_p)
    create_context = api("SDL_GL_CreateContext", ct.c_void_p, ct.c_void_p)
    destroy_context = api("SDL_GL_DestroyContext", ct.c_bool, ct.c_void_p)
    proc = api("SDL_GL_GetProcAddress", ct.c_void_p, ct.c_char_p)
    if not init(0x20):
        raise RuntimeError(error().decode())
    window = context = None
    try:
        for key, value in ((17, 3), (18, 3), (21, 1)):
            if not attribute(key, value):
                raise RuntimeError(error().decode())
        window = create_window(b"Human animation validation", 16, 16, 0x2 | 0x8)
        if not window:
            raise RuntimeError(error().decode())
        context = create_context(window)
        if not context:
            raise RuntimeError(error().decode())

        def gl(name, result, *args):
            address = proc(name.encode())
            if not address:
                raise RuntimeError(f"Unavailable GL function: {name}")
            return ct.WINFUNCTYPE(result, *args)(address)

        create_shader = gl("glCreateShader", ct.c_uint, ct.c_uint)
        source = gl("glShaderSource", None, ct.c_uint, ct.c_int, ct.POINTER(ct.c_char_p), ct.POINTER(ct.c_int))
        compile_shader = gl("glCompileShader", None, ct.c_uint)
        shader_status = gl("glGetShaderiv", None, ct.c_uint, ct.c_uint, ct.POINTER(ct.c_int))
        shader_log = gl("glGetShaderInfoLog", None, ct.c_uint, ct.c_int, ct.POINTER(ct.c_int), ct.c_void_p)
        delete_shader = gl("glDeleteShader", None, ct.c_uint)
        create_program = gl("glCreateProgram", ct.c_uint)
        attach = gl("glAttachShader", None, ct.c_uint, ct.c_uint)
        link = gl("glLinkProgram", None, ct.c_uint)
        program_status = gl("glGetProgramiv", None, ct.c_uint, ct.c_uint, ct.POINTER(ct.c_int))
        program_log = gl("glGetProgramInfoLog", None, ct.c_uint, ct.c_int, ct.POINTER(ct.c_int), ct.c_void_p)
        delete_program = gl("glDeleteProgram", None, ct.c_uint)

        def check(handle, status, log, flag):
            success, length = ct.c_int(), ct.c_int()
            status(handle, flag, ct.byref(success))
            status(handle, 0x8B84, ct.byref(length))
            message = ct.create_string_buffer(max(1, length.value))
            log(handle, len(message), None, message)
            if not success.value:
                raise RuntimeError(message.value.decode())

        results = []
        for srgb, buffers in ((False, False), (True, False), (False, True), (True, True)):
            header = "#version 140\n#define HAS_MOD\n#define HAS_DFDX\n#define HAS_FLOAT_TEXTURES\n"
            if srgb:
                header += "#define HAS_SRGB\n"
            if buffers:
                header += "#define HAS_UNIFORM_BUFFERS\n"
            shaders, program = [], 0
            try:
                for name, kind, define in (("vertex", 0x8B31, "VERTEX_SHADER"), ("fragment", 0x8B30, "FRAGMENT_SHADER")):
                    handle = create_shader(kind)
                    shaders.append(handle)
                    code = ct.c_char_p((header + f"#define {define}\n" + parsed["library"] + parsed[name]).encode())
                    source(handle, 1, ct.byref(code), None)
                    compile_shader(handle)
                    check(handle, shader_status, shader_log, 0x8B81)
                program = create_program()
                for handle in shaders:
                    attach(program, handle)
                link(program)
                check(program, program_status, program_log, 0x8B82)
                results.append({"nativeSrgb": srgb, "uniformBuffers": buffers, "compiledAndLinked": True})
            finally:
                if program:
                    delete_program(program)
                for handle in shaders:
                    delete_shader(handle)
        (OUT / "shader-validation.json").write_text(json.dumps(results, indent=2))
        print(f"Compiled and linked {len(results)} engine-generated GLSL variants.")
    finally:
        if context:
            destroy_context(context)
        if window:
            destroy_window(window)
        quit_sdl()


if __name__ == "__main__":
    main()
