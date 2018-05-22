using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipboardToPNG
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main()
        {
            var data = Clipboard.GetDataObject() as DataObject;

            string path = Environment.ExpandEnvironmentVariables(@"%TEMP%\clip.png");
            //foreach (var i in data.GetFormats())
            //{
            //    Debug.Print(i);
            //}

            // if we have a png, just write it to the file and be done
            if (data.GetDataPresent("PNG"))
            {
                Debug.Print("got data");
                var png = data.GetData("PNG") as MemoryStream;
                if (png != null)
                {
                    using (FileStream file = new FileStream(path, FileMode.Create, System.IO.FileAccess.Write))
                        png.CopyTo(file);
                    Debug.Print("saved png");
                    return 0;
                }
            }

            // if we have DIB that's 32 bit (ARGB), convert to an image and then save
            if (data.GetDataPresent(DataFormats.Dib))
            {
                Debug.Print("dib got");
                var dib = (data.GetData(DataFormats.Dib) as MemoryStream).ToArray();
                if (dib != null)
                {
                    var w = BitConverter.ToInt32(dib, 4);
                    var h = BitConverter.ToInt32(dib, 8);
                    var bpp = BitConverter.ToInt16(dib, 14);
                    var offset = 40;
                    if (bpp == 32) // if 32 bit (has transparency)
                    {
                        var test = BitConverter.ToInt64(dib, offset);
                        var test2 = BitConverter.ToInt32(dib, offset + 8);
                        // some applications include this bitmask of some sorts while copying that is 12 bytes in size. 
                        // the bmp header would let us get the offset to the pixel array directly, but we only have access to the dib header
                        // so the best i could come up with here was to check for this bitmask
                        // this means that there's a chance that an image with pixels that happen to hit these values could end up getting offset instead
                        // but this should be relatively rare - it would require a hit on:
                        // (A, R, G, B) = {(0, 0, 255, 0), (0, 255, 0, 0), (255, 0, 0, 0)}, which would be
                        // a sequence of a transparent green pixel, a transparent red pixel, and of an opaque black pixel.
                        if (test == 0x0000ff0000ff0000 && test2 == 0x000000ff)
                        {
                            offset += 12;
                        }

                        var gch = GCHandle.Alloc(dib, GCHandleType.Pinned);
                        Bitmap bmp = null;
                        try
                        {
                            var ptr = new IntPtr((long)gch.AddrOfPinnedObject() + offset);
                            bmp = new Bitmap(w, h, w * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr);
                            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
                            bmp.SetResolution(72, 72);
                            bmp.Save(path, ImageFormat.Png);
                            Debug.Print("saved dib");
                            return 0;
                        }
                        finally
                        {
                            gch.Free();
                            if (bmp != null) bmp.Dispose();
                        }
                    }
                }
            }

            // if that all fails, paste as normal (avoids messing with dpi and such)
            return 1;
        }
    }
}
