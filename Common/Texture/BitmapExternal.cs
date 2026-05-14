using Cairo;
using SkiaSharp;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Util;


namespace Vintagestory.API.Common
{
    public static class SurfaceDrawImage
    {
        public static void Image(this ImageSurface surface, BitmapRef bmp, int xPos, int yPos, int width, int height)
        {
            surface.Image(((BitmapExternal)bmp).bmp, xPos, yPos, width, height);
        }
    }

    public class BitmapExternal : BitmapRef
    {

        private SKBitmap _bmp;

        /// <summary>_bmp.IsNull backing store.</summary>
        private bool _bmpIsNull = true;

        /// <summary>_bmp.Info backing store.</summary>
        private SKImageInfo _imageInfo;

        /// <summary>Assign internal SKBitmap reference and immediately retrieve frequently accessed information.</summary>
        private void SetBmpInternal(SKBitmap newBmp)
        {
            _bmp = newBmp ?? throw new NullReferenceException("newBmp");
            _bmpIsNull = _bmp.IsNull;
            _imageInfo = _bmpIsNull ? new SKImageInfo() : _bmp.Info;
        }

        /// <summary>Assigns a new default 1x1 orange image as the internal SKBitmap reference.</summary>
        private void SetPlaceholderBmpInternal()
        {
            SetBmpInternal(new SKBitmap(1, 1));
            if (!_bmpIsNull)
            {
                _bmp.SetPixel(0, 0, SKColors.Orange);
            }
        }

        /// <returns>Reference to the internally managed SKBitmap object.</returns>
        /// <remarks>This operation can return an SKBitmap whose wrapped native object pointer is either null, or will become null.
        /// The caller must prove !SKBitmap.IsNull when using SKBitmap features that rely on native SK API calls.</remarks>
        public SKBitmap UnsafeGetInternalSkBitmap() => _bmp;

        [Obsolete("Use UnsafeGetInternalSkBitmap() to make it clear that this operation has non-trivial hazards.")]
        public SKBitmap bmp => _bmp;

        public override int Height => _imageInfo.Height;

        public override int Width => _imageInfo.Width;

        public override int[] Pixels => _bmpIsNull ? Array.Empty<int>() : Array.ConvertAll(_bmp.Pixels, p => (int)(uint)p);

        public IntPtr PixelsPtrAndLock => _bmpIsNull ? 0 : _bmp.GetPixels();

        public BitmapExternal(SKBitmap bmp)
        {
            SetBmpInternal(bmp);
        }

        public BitmapExternal(int width, int height)
        {
            SetBmpInternal(new SKBitmap(width, height));
        }

        public BitmapExternal(MemoryStream ms, ILogger logger, AssetLocation? loc = null)
        {
            try
            {
                SetBmpInternal(Decode(ms.ToArray()));
            }
            catch (Exception e)
            {
                if (loc != null)
                {
                    logger.Error("Failed loading bitmap from png file {0}. Will default to an empty 1x1 bitmap.", loc);
                    logger.Error(e);
                }
                else
                {
                    logger.Error("Failed loading bitmap. Will default to an empty 1x1 bitmap.");
                    logger.Error(e);
                }
                SetPlaceholderBmpInternal();
            }
        }

        ///// <summary>
        ///// Create a BitmapExternal from a path to an existing image file.  Calling code should check that the file exists
        ///// </summary>
        public BitmapExternal(string filePath, ILogger? logger = null)
        {
            try
            {
                SetBmpInternal(Decode(File.ReadAllBytes(filePath)));
            }
            catch (Exception ex)
            {
                if (logger != null) {
                    logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
                    logger.Error(ex);
                }

                SetPlaceholderBmpInternal();
            }
        }

        ///// <summary>
        ///// Create a BitmapExternal from a stream.  Calling code should close the stream.  The stream must have the Length property.
        ///// </summary>
        public BitmapExternal(Stream stream, ILogger? logger = null)
        {
            try
            {
                var buffer = new byte[stream.Length];
                stream.ReadExactly(buffer);
                SetBmpInternal(Decode(buffer));
            }
            catch (Exception ex)
            {
                if (logger != null) {
                    logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
                    logger.Error(ex);
                }

                SetPlaceholderBmpInternal();
            }
        }

        /// <summary>
        /// Create a BitmapExternal from a byte array
        /// </summary>
        public BitmapExternal(byte[] data, int dataLength, ILogger logger)
        {
            try
            {
                SetBmpInternal(Decode(data.AsSpan()[..dataLength]));
            }
            catch (Exception ex)
            {
                logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
                logger.Error(ex);
                SetPlaceholderBmpInternal();
            }
        }

        // Copypasted from SkBitmap.cs because the simplified Decode() changes AlphaType.Unpremul into AlphaType.Premul. wtf.
        public unsafe static SKBitmap Decode(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* ptr = buffer)
            {
                using SKData data = SKData.Create((IntPtr)ptr, buffer.Length);
                using SKCodec codec = SKCodec.Create(data);

                SKImageInfo bitmapInfo = codec.Info;
                bitmapInfo.AlphaType = SKAlphaType.Unpremul;
                // needs to be set so on MacOS so we load the pixel in the correct color format for the GPU upload, else we get R / B swaped channels
                bitmapInfo.ColorType = SKColorType.Bgra8888;
                return SKBitmap.Decode(codec, bitmapInfo);
            }
        }

        public override void Dispose()
        {
            _bmp.Dispose();
            _bmpIsNull = true;
            _imageInfo = new SKImageInfo();
        }

        public override void Save(string filename)
        {
            if (_bmpIsNull)
            {
                throw new NullReferenceException("Cannot save: _bmpIsNull");
            }
            _bmp.Save(filename);
        }

        /// <summary>
        /// Retrives the ARGB value from given coordinate
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override SKColor GetPixel(int x, int y)
        {
            return _bmpIsNull ? new SKColor() : _bmp.GetPixel(x, y);
        }

        /// <summary>
        /// Retrives the ARGB value from given coordinate using normalized coordinates (0..1)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override SKColor GetPixelRel(float x, float y)
        {
            return _bmpIsNull ? new SKColor() : _bmp.GetPixel((int)Math.Min(_imageInfo.Width - 1, x * _imageInfo.Width), (int)Math.Min(_imageInfo.Height - 1, (y * _imageInfo.Height)));
        }

        public override unsafe void MulAlpha(int alpha = 255)
        {
            if (_bmpIsNull)
            {
                return;
            }
            var len = Width * Height;
            var af = alpha / 255f;
            var colp = (byte*)_bmp.GetPixels().ToPointer();
            for (var i = 0; i < len; i++)
            {
                int a = colp[3];
                colp[0] = (byte)(colp[0] * af);
                colp[1] = (byte)(colp[1] * af);
                colp[2] = (byte)(colp[2] * af);

                colp[3] = (byte)(a * af);
                colp += 4;
            }
        }

        public override int[] GetPixelsTransformed(int rot = 0, int mulAlpha = 255)
        {
            if (_bmpIsNull)
            {
                return Array.Empty<int>();
            }
            int[] bmpPixels = new int[Width * Height];
            int width = _imageInfo.Width;
            int height = _imageInfo.Height;
            FastBitmap fastBitmap = new FastBitmap();
            fastBitmap.bmp = _bmp;
            int stride = fastBitmap.Stride;
            switch (rot)
            {
                case 0:
                {
                    for (int y = 0; y < height; y++)
                    {
                        fastBitmap.GetPixelRow(width, y * stride, bmpPixels, y * width);
                    }

                    break;
                }
                case 90:
                {
                    for (int x = 0; x < width; x++)
                    {
                        int baseY = x * width;
                        for (int y = 0; y < height; y++)
                        {
                            bmpPixels[y + baseY] = fastBitmap.GetPixel(width - x - 1, y * stride);
                        }
                    }

                    break;
                }
                case 180:
                {
                    for (int y = 0; y < height; y++)
                    {
                        int baseX = y * width;
                        int yStride = (height - y - 1) * stride;
                        for (int x = 0; x < width; x++)
                        {
                            bmpPixels[x + baseX] = fastBitmap.GetPixel(width - x - 1, yStride);
                        }
                    }

                    break;
                }
                case 270:
                {
                    for (int x = 0; x < width; x++)
                    {
                        int baseY = x * width;
                        for (int y = 0; y < height; y++)
                        {
                            bmpPixels[y + baseY] = fastBitmap.GetPixel(x, (height - y - 1) * stride);
                        }
                    }

                    break;
                }
            }

            if (mulAlpha != 255)
            {
                var alpaP = mulAlpha / 255f;
                int clearAlpha = ~(0xff << 24);

                for (int i = 0; i < bmpPixels.Length; i++)
                {
                    var col = bmpPixels[i];
                    var curAlpha = (uint)col >> 24;
                    col &= clearAlpha;

                    bmpPixels[i] = col | ((int)(curAlpha * alpaP) << 24);
                }
            }

            return bmpPixels;
        }

        public override BitmapExternal CropTo(int newSize)
        {
            if (_bmpIsNull)
            {
                return new BitmapExternal(0, 0);
            }
            SKBitmap bmpPixels = new(newSize,newSize);
            int width = _imageInfo.Width;
            int height = _imageInfo.Height;
            int centerOffsetX = (width - newSize);
            int centerOffsetY = 0;
            int brown = (29 << 16) + (11 << 8) + 1;
            for (int x = 0; x < newSize; x++)
            {
                for (int y = 0; y < newSize; y++)
                {
                    var color = _bmp.GetPixel(x + centerOffsetX, y + centerOffsetY);
                    int alpha = (int)((uint)color >> 24);
                    if (alpha < 255)
                    {
                        int rgb = (int)((uint)color & 0xFFFFFFu);
                        color = (uint)MathTools.ColorUtil.ColorOverlay(brown, rgb, alpha / 255f) | 0xFF000000u;
                    }
                    bmpPixels.SetPixel(x, y, color);
                }
            }

            return new BitmapExternal(bmpPixels);
        }
    }
}
