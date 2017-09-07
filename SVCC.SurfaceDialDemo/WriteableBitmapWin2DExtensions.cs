using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;

namespace SVCC.SurfaceDialDemo
{
    public static class WriteableBitmapWin2DExtensions
    {
        public static async Task<CanvasBitmap> ReadFromWriteableBitmapAsync(this CanvasBitmap canvasBitmap,
            WriteableBitmap writeableBitmap, ICanvasResourceCreator canvasResourceCreator)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                await writeableBitmap.ToStream(stream, BitmapEncoder.PngEncoderId);
                stream.Seek(0);
                return await CanvasBitmap.LoadAsync(canvasResourceCreator, stream);
            }
        }

        public static async Task<WriteableBitmap> WriteToWriteableBitmapAsync(this ICanvasImage canvasImage,
            ICanvasResourceCreator canvasResourceCreator, Size size)
        {
            return await canvasImage.WriteToWriteableBitmapAsync(canvasResourceCreator, size, 96);
        }

        public static async Task<WriteableBitmap> WriteToWriteableBitmapAsync(this ICanvasImage canvasImage,
            ICanvasResourceCreator canvasResourceCreator, Size size, float dpi)
        {
            // Initialize the in-memory stream where data will be stored.
            using (var stream = new InMemoryRandomAccessStream())
            {
                await CanvasImage.SaveAsync(canvasImage, new Rect(new Point(0, 0), size), dpi, canvasResourceCreator,
                    stream,
                    CanvasBitmapFileFormat.Png);

                stream.Seek(0);
                return await BitmapFactory.FromStream(stream);
            }
        }
    }
}