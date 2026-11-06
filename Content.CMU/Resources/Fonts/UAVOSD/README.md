# CMU OSD Cyrillic

This extends the bundled UAV OSD Sans Mono with Russian glyphs drawn in
its angular OSD style. It does not substitute another typeface or
transliterate Cyrillic text.

- All original character outlines and advances are preserved.
- All 66 Russian letters, including Ё/ё and Й/й, are covered.
- Lowercase characters retain the original font's all-capitals presentation.
- Added punctuation includes «», №, –, —, … and typographic quotes.
- New letters retain the 3586-unit advance and the original vertical metrics.
  Diacritics fit inside the cap height; Д, Ц and Щ use the existing descender.

The CRT stack in `StyleNanoCrt` selects this file first. Remaining Noto
entries are for symbols and languages outside the extension. Keeping the
`/Fonts/UAVOSD/` path also preserves the existing UI font preference and
its OSD-to-standard size conversion. Readable chat keeps its independent font.

Install `fonttools`, then run from the repository root:

```sh
python Tools/cmu/fonts/extend_uav_osd.py
```

See `ATTRIBUTION-CMU-OSD.txt` for the original designer and usage statement.
