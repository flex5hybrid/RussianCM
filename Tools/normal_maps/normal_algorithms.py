"""Deterministic, frame-local tangent-space normal estimation for SS14 pixel art.

This is an *approximation*: RGB alone cannot uniquely recover 3D surface normals.
All processing stays inside a single RSI animation/direction frame.
"""
from __future__ import annotations

from dataclasses import dataclass
import numpy as np
from PIL import Image, ImageFilter

MODES = ('flat', 'bevel', 'height', 'hybrid')
PROFILES = ('auto', 'generic', 'equipment', 'character', 'floor')


@dataclass(frozen=True)
class NormalOptions:
    mode: str = 'hybrid'
    profile: str = 'auto'
    bevel: float = 1.25
    strength: float = 5.0
    detail_strength: float = 2.2
    detail_radius: float = 2.4
    edge_strength: float = 0.16

    def __post_init__(self) -> None:
        if self.mode not in MODES or self.profile not in PROFILES:
            raise ValueError('Invalid normal generation mode/profile')
        if not (0 <= self.bevel <= 16 and 0 <= self.strength <= 100 and
                0 <= self.detail_strength <= 20 and 0.5 <= self.detail_radius <= 16 and
                0 <= self.edge_strength <= 2):
            raise ValueError('Normal generation options outside allowed range')


def profile_for_path(path: str) -> str:
    parts = {part.lower() for part in path.replace('\\', '/').split('/')}
    if parts & {'tiles', 'floors', 'floor', 'turf', 'terrain'}:
        return 'floor'
    if parts & {'mobs', 'human', 'humans', 'characters', 'xenonids', 'xenos', 'clothing'}:
        return 'character'
    if parts & {'structures', 'machines', 'objects', 'items', 'doors', 'vehicles', 'equipment', 'furniture'}:
        return 'equipment'
    return 'generic'


def _blur(channel: np.ndarray, radius: float) -> np.ndarray:
    if radius <= 0:
        return channel.astype(np.float32, copy=False)
    data = np.uint8(np.rint(np.clip(channel * 255.0, 0, 255)))
    return np.asarray(Image.fromarray(data, 'L').filter(ImageFilter.GaussianBlur(radius)),
                      dtype=np.float32) / 255.0


def _gradient(channel: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    # Central difference in pixel coordinates; replicate border values rather than
    # sampling neighbouring frames in an RSI spritesheet.
    padded = np.pad(channel, ((1, 1), (1, 1)), mode='edge')
    dx = (padded[1:-1, 2:] - padded[1:-1, :-2]) * 0.5
    dy = (padded[2:, 1:-1] - padded[:-2, 1:-1]) * 0.5
    return dx, dy


def _frame_normals(frame: Image.Image, opts: NormalOptions, profile: str) -> Image.Image:
    src = np.asarray(frame.convert('RGBA'), dtype=np.uint8)
    h, w = src.shape[:2]
    alpha = src[:, :, 3].astype(np.float32) / 255.0
    normal_x = np.zeros((h, w), dtype=np.float32)
    normal_y = np.zeros((h, w), dtype=np.float32)

    # Preserve the old bevel for translucent object contours but avoid treating
    # the sheet's outer edge as an implicit contour of adjacent frames.
    if opts.mode in ('bevel', 'hybrid') and opts.bevel > 0 and profile != 'floor':
        edge = _blur(alpha, opts.bevel)
        dx, dy = _gradient(edge)
        normal_x -= dx * opts.strength
        normal_y += dy * opts.strength

    if opts.mode in ('height', 'hybrid') and opts.detail_strength > 0:
        rgb = src[:, :, :3].astype(np.float32) / 255.0
        # Lightness as a *weak shape cue*; subtract low frequencies to avoid
        # interpreting broad hand-painted illumination as object height.
        luminance = rgb[:, :, 0] * 0.2126 + rgb[:, :, 1] * 0.7152 + rgb[:, :, 2] * 0.0722
        smooth = _blur(luminance, 0.55)
        broad = _blur(smooth, opts.detail_radius)
        height = np.clip((smooth - broad) * 1.8, -0.35, 0.35)
        # Discard tiny gradients/noise; pixel-art panels and seams remain.
        height = np.sign(height) * np.maximum(np.abs(height) - 0.018, 0.0)
        dx, dy = _gradient(height)
        mult = {'floor': 0.28, 'character': 0.7, 'generic': 0.85,
                'equipment': 1.0}[profile]
        normal_x -= dx * opts.detail_strength * mult
        normal_y += dy * opts.detail_strength * mult

        if opts.edge_strength > 0:
            # Add *subtle* inset joints along strong interior colour edges,
            # including iso-luminant stripes that RGB->gray alone would miss.
            # Saturation-normalized chroma reduces responses to light/dark shading.
            sm = np.empty_like(rgb)
            for c in range(3):
                sm[:, :, c] = _blur(rgb[:, :, c], 0.6)
            chroma = sm / np.maximum(sm.max(axis=2, keepdims=True), 0.12)
            energy = np.zeros((h, w), dtype=np.float32)
            for c in range(3):
                gx, gy = _gradient(chroma[:, :, c])
                energy += gx * gx + gy * gy
            magnitude = np.sqrt(energy)
            seam = -np.clip((magnitude - 0.07) * 1.8, 0.0, 1.0)
            seam = _blur((seam + 1.0), 0.65) - 1.0
            sx, sy = _gradient(seam)
            normal_x -= sx * opts.edge_strength * mult
            normal_y += sy * opts.edge_strength * mult

    # Normalize per pixel. Outside geometry alpha is exactly zero.
    denom = np.sqrt(normal_x * normal_x + normal_y * normal_y + 1.0)
    normals = np.stack((normal_x / denom, normal_y / denom, 1.0 / denom), axis=2)
    encoded = np.uint8(np.rint(np.clip((normals * 0.5 + 0.5) * 255.0, 0, 255)))
    final = np.empty_like(src)
    final[:, :, :3] = encoded
    final[:, :, 3] = src[:, :, 3]
    final[alpha == 0, :3] = 0
    return Image.fromarray(final, 'RGBA')


def normal_sheet(source: Image.Image, frame_size: tuple[int, int],
                 opts: NormalOptions, *, resource_path: str = '') -> Image.Image:
    w, h = source.size
    fw, fh = frame_size
    if fw <= 0 or fh <= 0 or w % fw or h % fh:
        raise ValueError(f'Image {source.size} is not a multiple of RSI frame size {frame_size}')
    profile = profile_for_path(resource_path) if opts.profile == 'auto' else opts.profile
    result = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    for y in range(0, h, fh):
        for x in range(0, w, fw):
            crop = source.crop((x, y, x + fw, y + fh))
            result.paste(_frame_normals(crop, opts, profile), (x, y))
    return result


def light_preview(source: Image.Image, normal: Image.Image,
                  frame_size: tuple[int, int], scale: int = 4) -> Image.Image:
    """Diagnostic 3-panel image: original | normal | re-lit approximation.

    Preview lighting is *not* the game's lighting shader.
    """
    w, h = frame_size
    src = source.convert('RGBA').crop((0, 0, w, h))
    nrm = normal.convert('RGBA').crop((0, 0, w, h))
    base = np.asarray(src, dtype=np.float32)
    normal_rgb = np.asarray(nrm, dtype=np.float32)[:, :, :3] / 255.0 * 2.0 - 1.0
    lighting = np.maximum(0.0, normal_rgb[:, :, 0] * 0.50 +
                          normal_rgb[:, :, 1] * 0.40 + normal_rgb[:, :, 2] * 0.77)
    shaded = np.uint8(np.rint(np.clip(base[:, :, :3] * (0.25 + lighting[:, :, None] * 0.85), 0, 255)))
    relit = Image.fromarray(np.dstack((shaded, base[:, :, 3].astype(np.uint8))), 'RGBA')
    padding = max(6, scale * 2)
    sheet = Image.new('RGBA', (w * 3 * scale + padding * 4, h * scale + padding * 2), (26, 30, 39, 255))
    for i, image in enumerate((src, nrm, relit)):
        sheet.alpha_composite(image.resize((w * scale, h * scale), Image.Resampling.NEAREST),
                              (padding * (i + 1) + i * w * scale, padding))
    return sheet
