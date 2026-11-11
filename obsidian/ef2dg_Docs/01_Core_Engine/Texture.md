# Texture

🌐 **Русский** · [English](../../ef2dg_Docs_en/01_Core_Engine/Texture.md)

`Texture` — RAII-обёртка над OpenGL texture handle. Она загружает файл через StbImageSharp либо создаёт текстуру из массива байтов, хранит ширину и высоту и умеет привязываться к слоту `TextureUnit`.

Текстуры игровых ресурсов следует получать через [ResourceManager](ResourceManager.md), а не создавать вручную в компонентах. Исключение — временная обёртка текстуры framebuffer, которую `Engine` использует при композиции кадра.
