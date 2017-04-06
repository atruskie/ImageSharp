﻿// <copyright file="BlankProvider.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Xunit.Abstractions;

    public interface ITestImageProvider
    {
        PixelTypes PixelType { get; }
        ImagingTestCaseUtility Utility { get; }
    }

    public abstract partial class TestImageProvider<TColor> : ITestImageProvider
        where TColor : struct, IPixel<TColor>
    {

        /// <summary>
        /// A test image provider that produces test patterns.
        /// </summary>
        /// <typeparam name="TColor"></typeparam>
        private class TestPatternProvider : BlankProvider
        {
            static Dictionary<string, Image<TColor>> testImages = new Dictionary<string, Image<TColor>>();

            public TestPatternProvider(int width, int height)
                : base(width, height)
            {
            }

            public TestPatternProvider()
                : base()
            {
            }

            public override string SourceFileOrDescription => $"TestPattern{this.Width}x{this.Height}";

            public override Image<TColor> GetImage()
            {
                lock (testImages)
                {
                    if (!testImages.ContainsKey(this.SourceFileOrDescription))
                    {
                        Image<TColor> image = new Image<TColor>(this.Width, this.Height);
                        DrawTestPattern(image);
                        testImages.Add(this.SourceFileOrDescription, image);
                    }
                }

                return new Image<TColor>(testImages[this.SourceFileOrDescription]);
            }

            /// <summary>
            /// Draws the test pattern on an image by drawing 4 other patterns in the for quadrants of the image.
            /// </summary>
            /// <param name="image"></param>
            private static void DrawTestPattern(Image<TColor> image)
            {
                // first lets split the image into 4 quadrants
                using (PixelAccessor<TColor> pixels = image.Lock())
                {
                    BlackWhiteChecker(pixels); // top left
                    VirticalBars(pixels); // top right
                    TransparentGradients(pixels); // bottom left
                    Rainbow(pixels); // bottom right 
                }
            }
            /// <summary>
            /// Fills the top right quadrant with alternating solid vertical bars.
            /// </summary>
            /// <param name="pixels"></param>
            private static void VirticalBars(PixelAccessor<TColor> pixels)
            {
                // topLeft 
                int left = pixels.Width / 2;
                int right = pixels.Width;
                int top = 0;
                int bottom = pixels.Height / 2;
                int stride = pixels.Width / 12;
                if (stride == 0)
                {
                    stride = 1;
                }
                TColor[] c = {
                        NamedColors<TColor>.HotPink,
                        NamedColors<TColor>.Blue
                    };
                int p = 0;
                for (int y = top; y < bottom; y++)
                {
                    for (int x = left; x < right; x++)
                    {
                        if (x % stride == 0)
                        {
                            p++;
                            p = p % c.Length;
                        }
                        pixels[x, y] = c[p];
                    }
                }
            }

            /// <summary>
            /// fills the top left quadrant with a black and white checker board.
            /// </summary>
            /// <param name="pixels"></param>
            private static void BlackWhiteChecker(PixelAccessor<TColor> pixels)
            {
                // topLeft 
                int left = 0;
                int right = pixels.Width / 2;
                int top = 0;
                int bottom = pixels.Height / 2;
                int stride = pixels.Width / 6;
                if (stride == 0)
                {
                    stride = 1;
                }
                TColor[] c = {
                        NamedColors<TColor>.Black,
                        NamedColors<TColor>.White
                    };

                int p = 0;
                for (int y = top; y < bottom; y++)
                {
                    if (y % stride == 0)
                    {
                        p++;
                        p = p % c.Length;
                    }
                    int pstart = p;
                    for (int x = left; x < right; x++)
                    {
                        if (x % stride == 0)
                        {
                            p++;
                            p = p % c.Length;
                        }
                        pixels[x, y] = c[p];
                    }
                    p = pstart;
                }
            }

            /// <summary>
            /// Fills the bottom left quadrent with 3 horizental bars in Red, Green and Blue with a alpha gradient from left (transparent) to right (solid).
            /// </summary>
            /// <param name="pixels"></param>
            private static void TransparentGradients(PixelAccessor<TColor> pixels)
            {
                // topLeft 
                int left = 0;
                int right = pixels.Width / 2;
                int top = pixels.Height / 2;
                int bottom = pixels.Height;

                int height = (top - bottom);
                int width = (left - right);
                if (height == 0 || width == 0)
                {
                    return;
                }

                int bandHeight = (int)Math.Ceiling(pixels.Height / 6f);

                Vector4 red = Color.Red.ToVector4(); // use real color so we can see har it translates in the test pattern
                Vector4 green = Color.Green.ToVector4(); // use real color so we can see har it translates in the test pattern
                Vector4 blue = Color.Blue.ToVector4(); // use real color so we can see har it translates in the test pattern

                TColor c = default(TColor);

                for (int x = left; x < right; x++)
                {
                    blue.W = red.W = green.W = (float)x / (float)right;

                    c.PackFromVector4(red);
                    int topBand = top;
                    for (int y = topBand; y < top + bandHeight && y < pixels.Height; y++)
                    {
                        pixels[x, y] = c;
                    }
                    topBand = topBand + bandHeight;

                    c.PackFromVector4(green);
                    for (int y = topBand; y < topBand + bandHeight && y < pixels.Height; y++)
                    {
                        pixels[x, y] = c;
                    }
                    topBand = topBand + bandHeight;
                    c.PackFromVector4(blue);
                    for (int y = topBand; y < bottom && y < pixels.Height; y++)
                    {
                        pixels[x, y] = c;
                    }
                }
            }

            /// <summary>
            /// Fills the bottom right quadrant with all the colors producable by converting itterating over a uint and unpacking it.
            /// A better algorithm could be used but it works
            /// </summary>
            /// <param name="pixels"></param>
            private static void Rainbow(PixelAccessor<TColor> pixels)
            {
                int left = pixels.Width / 2;
                int right = pixels.Width;
                int top = pixels.Height / 2;
                int bottom = pixels.Height;

                int height = (top - bottom);
                int width = (left - right);
                if (height == 0 || width == 0)
                {
                    return;
                }
                int pixelCount = width * height;
                uint stepsPerPixel = (uint)(uint.MaxValue / pixelCount);
                TColor c = default(TColor);
                Color t = new Color(0);

                for (int x = left; x < right; x++)
                    for (int y = top; y < bottom; y++)
                    {
                        t.PackedValue += stepsPerPixel;
                        Vector4 v = t.ToVector4();
                        //v.W = (x - left) / (float)left;
                        c.PackFromVector4(v);
                        pixels[x, y] = c;
                    }
            }
        }
    }
}