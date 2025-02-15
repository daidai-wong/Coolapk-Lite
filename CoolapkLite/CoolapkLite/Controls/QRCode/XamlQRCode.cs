﻿using QRCoder;
using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace CoolapkLite.Controls
{
    public class XamlQRCode : AbstractQRCode, IDisposable
    {
        private readonly bool _disposed;

        /// <summary>
        /// Constructor without params to be used in COM Objects connections
        /// </summary>
        public XamlQRCode(bool dispose = true) { _disposed = dispose; }

        public XamlQRCode(QRCodeData data, bool dispose = true) : base(data) { _disposed = dispose; }

        public GeometryGroup GetGraphic(Size viewBox, bool hasRadius = true)
        {
            int drawableModulesCount = GetDrawableModulesCount();
            double unitsPerModule = GetUnitsPerModule(viewBox, drawableModulesCount);
            const int offsetModules = 4;

            GeometryGroup group = new GeometryGroup();
            double x = 0d;
            for (int xi = offsetModules; xi < drawableModulesCount + offsetModules; xi++)
            {
                double y = 0d;
                for (int yi = offsetModules; yi < drawableModulesCount + offsetModules; yi++)
                {
                    if (hasRadius)
                    {
                        if (QrCodeData.ModuleMatrix[yi][xi])
                        {
                            RadiusFilterKind filterKind = GetRadiusFilterKind(xi, yi);
                            group.Children.Add(
                                new PathGeometry
                                {
                                    Figures = new PathFigureCollection
                                    {
                                        CreateRoundedRectanglePath(x, y, unitsPerModule, filterKind)
                                    }
                                });
                        }
                        else
                        {
                            RadiusFilterKind filterKind = GetAntiRadiusFilterKind(xi, yi);
                            group.Children.Add(
                                new PathGeometry
                                {
                                    Figures = CreateEntiRoundedRectanglePath(x, y, unitsPerModule, filterKind)
                                });
                        }
                    }
                    else if (QrCodeData.ModuleMatrix[yi][xi])
                    {
                        group.Children.Add(
                        new RectangleGeometry
                        {
                            Rect = new Rect(x, y, unitsPerModule, unitsPerModule)
                        });
                    }
                    y += unitsPerModule;
                }
                x += unitsPerModule;
            }
            return group;
        }

        private int GetDrawableModulesCount()
        {
            return QrCodeData.ModuleMatrix.Count - 8;
        }

        private static double GetUnitsPerModule(Size viewBox, int drawableModulesCount)
        {
            double qrSize = Math.Min(viewBox.Width, viewBox.Height);
            return qrSize / drawableModulesCount;
        }

        private static PathFigure CreateRoundedRectanglePath(double x, double y, double unitsPerModule, RadiusFilterKind filterKind)
        {
            double x_middle = x + (unitsPerModule / 2);
            double x_end = x + unitsPerModule;
            double y_middle = y + (unitsPerModule / 2);
            double y_end = y + unitsPerModule;
            Size arcSize = new Size(unitsPerModule / 2, unitsPerModule / 2);

            PathFigure PathFigure = new PathFigure
            {
                IsClosed = true,
                StartPoint = new Point(x_middle, y),
                Segments = new PathSegmentCollection()
            };

            if (filterKind.HasFlag(RadiusFilterKind.TopLeft))
            {
                PathFigure.Segments.Add(
                    new ArcSegment
                    {
                        Point = new Point(x, y_middle),
                        Size = arcSize
                    });
            }
            else
            {
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x, y)
                    });
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x, y_middle)
                    });
            }

            if (filterKind.HasFlag(RadiusFilterKind.BottomLeft))
            {
                PathFigure.Segments.Add(
                    new ArcSegment
                    {
                        Point = new Point(x_middle, y_end),
                        Size = arcSize
                    });
            }
            else
            {
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x, y_end)
                    });
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x_middle, y_end)
                    });
            }

            if (filterKind.HasFlag(RadiusFilterKind.BottomRight))
            {
                PathFigure.Segments.Add(
                    new ArcSegment
                    {
                        Point = new Point(x_end, y_middle),
                        Size = arcSize
                    });
            }
            else
            {
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x_end, y_end)
                    });
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x_end, y_middle)
                    });
            }

            if (filterKind.HasFlag(RadiusFilterKind.TopRight))
            {
                PathFigure.Segments.Add(
                    new ArcSegment
                    {
                        Point = new Point(x_middle, y),
                        Size = arcSize
                    });
            }
            else
            {
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x_end, y)
                    });
                PathFigure.Segments.Add(
                    new LineSegment
                    {
                        Point = new Point(x_middle, y)
                    });
            }

            return PathFigure;
        }

        private static PathFigureCollection CreateEntiRoundedRectanglePath(double x, double y, double unitsPerModule, RadiusFilterKind filterKind)
        {
            double x_middle = x + (unitsPerModule / 2);
            double x_end = x + unitsPerModule;
            double y_middle = y + (unitsPerModule / 2);
            double y_end = y + unitsPerModule;
            Size arcSize = new Size(unitsPerModule / 2, unitsPerModule / 2);

            PathFigureCollection pathFigures = new PathFigureCollection();

            if (filterKind.HasFlag(RadiusFilterKind.TopLeft))
            {
                pathFigures.Add(
                    new PathFigure
                    {
                        IsClosed = true,
                        StartPoint = new Point(x_middle, y),
                        Segments = new PathSegmentCollection
                        {
                            new ArcSegment
                            {
                                Point = new Point(x, y_middle),
                                Size = arcSize
                            },
                            new LineSegment
                            {
                                Point = new Point(x, y)
                            },
                            new LineSegment
                            {
                                Point = new Point(x_middle, y)
                            }
                        }
                    });
            }

            if (filterKind.HasFlag(RadiusFilterKind.BottomLeft))
            {
                pathFigures.Add(
                    new PathFigure
                    {
                        IsClosed = true,
                        StartPoint = new Point(x, y_middle),
                        Segments = new PathSegmentCollection
                        {
                            new ArcSegment
                            {
                                Point = new Point(x_middle, y_end),
                                Size = arcSize
                            },
                            new LineSegment
                            {
                                Point = new Point(x, y_end)
                            },
                            new LineSegment
                            {
                                Point = new Point(x, y_middle)
                            }
                        }
                    });
            }

            if (filterKind.HasFlag(RadiusFilterKind.BottomRight))
            {
                pathFigures.Add(
                    new PathFigure
                    {
                        IsClosed = true,
                        StartPoint = new Point(x_middle, y_end),
                        Segments = new PathSegmentCollection
                        {
                            new ArcSegment
                            {
                                Point = new Point(x_end, y_middle),
                                Size = arcSize
                            },
                            new LineSegment
                            {
                                Point = new Point(x_end, y_end)
                            },
                            new LineSegment
                            {
                                Point = new Point(x_middle, y_end)
                            }
                        }
                    });
            }

            if (filterKind.HasFlag(RadiusFilterKind.TopRight))
            {
                pathFigures.Add(
                    new PathFigure
                    {
                        IsClosed = true,
                        StartPoint = new Point(x_end, y_middle),
                        Segments = new PathSegmentCollection
                        {
                            new ArcSegment
                            {
                                Point = new Point(x_middle, y),
                                Size = arcSize
                            },
                            new LineSegment
                            {
                                Point = new Point(x_end, y)
                            },
                            new LineSegment
                            {
                                Point = new Point(x_end, y_middle)
                            }
                        }
                    });
            }

            return pathFigures;
        }

        private RadiusFilterKind GetRadiusFilterKind(int xi, int yi)
        {
            RadiusFilterKind radiusFilterKind = RadiusFilterKind.None;
            if (!QrCodeData.ModuleMatrix[yi - 1][xi])
            {
                if (!QrCodeData.ModuleMatrix[yi][xi - 1])
                {
                    radiusFilterKind |= RadiusFilterKind.TopLeft;
                }
                if (!QrCodeData.ModuleMatrix[yi][xi + 1])
                {
                    radiusFilterKind |= RadiusFilterKind.TopRight;
                }
            }
            if (!QrCodeData.ModuleMatrix[yi + 1][xi])
            {
                if (!QrCodeData.ModuleMatrix[yi][xi - 1])
                {
                    radiusFilterKind |= RadiusFilterKind.BottomLeft;
                }
                if (!QrCodeData.ModuleMatrix[yi][xi + 1])
                {
                    radiusFilterKind |= RadiusFilterKind.BottomRight;
                }
            }
            return radiusFilterKind;
        }

        private RadiusFilterKind GetAntiRadiusFilterKind(int xi, int yi)
        {
            RadiusFilterKind radiusFilterKind = RadiusFilterKind.None;
            if (QrCodeData.ModuleMatrix[yi - 1][xi])
            {
                if (QrCodeData.ModuleMatrix[yi][xi - 1] && QrCodeData.ModuleMatrix[yi - 1][xi - 1])
                {
                    radiusFilterKind |= RadiusFilterKind.TopLeft;
                }
                if (QrCodeData.ModuleMatrix[yi][xi + 1] && QrCodeData.ModuleMatrix[yi - 1][xi + 1])
                {
                    radiusFilterKind |= RadiusFilterKind.TopRight;
                }
            }
            if (QrCodeData.ModuleMatrix[yi + 1][xi])
            {
                if (QrCodeData.ModuleMatrix[yi][xi - 1] && QrCodeData.ModuleMatrix[yi + 1][xi - 1])
                {
                    radiusFilterKind |= RadiusFilterKind.BottomLeft;
                }
                if (QrCodeData.ModuleMatrix[yi][xi + 1] && QrCodeData.ModuleMatrix[yi + 1][xi + 1])
                {
                    radiusFilterKind |= RadiusFilterKind.BottomRight;
                }
            }
            return radiusFilterKind;
        }

        public new void Dispose()
        {
            Dispose(_disposed);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                QrCodeData?.Dispose();
            }
            QrCodeData = null;
        }
    }

    [Flags]
    public enum RadiusFilterKind : byte
    {
        None = 0x00,
        TopLeft = 0x01,
        TopRight = 0x02,
        BottomLeft = 0x04,
        BottomRight = 0x08,
    }
}
