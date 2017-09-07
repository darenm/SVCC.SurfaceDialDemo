using Windows.Graphics.Effects;
using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace SVCC.SurfaceDialDemo
{
    public static class EffectFactory
    {
        public static ICanvasImage CreateContrastEffect(IGraphicsEffectSource loadedImage, Slider valueSlider)
        {
            valueSlider.Minimum = -1;
            valueSlider.Maximum = 1;
            valueSlider.StepFrequency = 0.1;

            var brightnessEffect = new ContrastEffect
            {
                Source = loadedImage,
                Contrast = 0
            };

            return brightnessEffect;
        }

        public static ICanvasImage CreateExposureEffect(IGraphicsEffectSource loadedImage, Slider valueSlider)
        {
            valueSlider.Minimum = -2;
            valueSlider.Maximum = 2;
            valueSlider.StepFrequency = 0.2;


            var brightnessEffect = new ExposureEffect
            {
                Source = loadedImage,
                Exposure = 0
            };

            return brightnessEffect;
        }
    }
}