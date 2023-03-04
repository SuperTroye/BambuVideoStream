using System;
using System.Drawing;

namespace BambuVideoStream
{
    static class ImageProccesingTools
    {
        public static Bitmap CropUnwantedBackground(Bitmap bmpOriginal)
        {
            // Getting The Background Color by checking Corners of Original Image
            Color baseColor = bmpOriginal.GetPixel(0, 0);
            int SameCorners = 0;
            bool hasBaseColor = false;

            Point[] Corners = new Point[]{new Point(0,0),
                new Point(0, bmpOriginal.Height-1),
                new Point(bmpOriginal.Width-1, 0),
                new Point(bmpOriginal.Width-1, bmpOriginal.Height-1)};

            for (int i = 0; i < 4; i++)
            {
                SameCorners = 0;
                baseColor = bmpOriginal.GetPixel(Corners[i].X, Corners[i].Y);
                for (int j = 0; j < 4; j++)
                {
                    Color CornerColor = bmpOriginal.GetPixel(Corners[j].X, Corners[j].Y);
                    if ((CornerColor.R <= baseColor.R * 1.1 && CornerColor.R >= baseColor.R * 0.9) &&
                        (CornerColor.G <= baseColor.G * 1.1 && CornerColor.G >= baseColor.G * 0.9) &&
                        (CornerColor.B <= baseColor.B * 1.1 && CornerColor.B >= baseColor.B * 0.9))
                    {
                        SameCorners++;
                    }
                }
                if (SameCorners > 2)
                {
                    hasBaseColor = true;
                    break;
                }
            }

            //--------------------------------------------------------------------
            // Finding the Bounds of Crop Area bu using Unsafe Code and Image Proccesing
            // keep in mind in BitmapData Pixel Format is BGR NOT RGB !!!!!!

            if (hasBaseColor)
            {
                int width = bmpOriginal.Width, height = bmpOriginal.Height;
                bool UpperLeftPointFounded = false;
                Point[] bounds = new Point[2];
                Color c;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        c = bmpOriginal.GetPixel(x, y);
                        bool sameAsBackColor = ((c.R <= baseColor.R * 1.1 && c.R >= baseColor.R * 0.9) &&
                                                (c.G <= baseColor.G * 1.1 && c.G >= baseColor.G * 0.9) &&
                                                (c.B <= baseColor.B * 1.1 && c.B >= baseColor.B * 0.9));
                        if (!sameAsBackColor)
                        {
                            if (!UpperLeftPointFounded)
                            {
                                bounds[0] = new Point(x, y);
                                bounds[1] = new Point(x, y);
                                UpperLeftPointFounded = true;
                            }
                            else
                            {
                                if (x > bounds[1].X)
                                    bounds[1].X = x;
                                else if (x < bounds[0].X)
                                    bounds[0].X = x;
                                if (y >= bounds[1].Y)
                                    bounds[1].Y = y;
                            }
                        }
                    }
                }
                Bitmap result = new Bitmap(bounds[1].X - bounds[0].X + 1, bounds[1].Y - bounds[0].Y + 1);
                Graphics g = Graphics.FromImage(result);
                g.DrawImage(bmpOriginal, new Rectangle(0, 0, result.Width, result.Height), new Rectangle(bounds[0].X, bounds[0].Y, bounds[1].X - bounds[0].X + 1, bounds[1].Y - bounds[0].Y + 1), GraphicsUnit.Pixel);
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
