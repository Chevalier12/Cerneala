using System.Globalization;

namespace Cerneala.Drawing.Paths;

internal static class SvgPathFlattener
{
    public static IReadOnlyList<DrawPoint[]> Flatten(string data, float tolerance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        if (!float.IsFinite(tolerance) || tolerance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance));
        }

        return new Parser(data, tolerance).Parse();
    }

    private sealed class Parser
    {
        private readonly string data;
        private readonly float tolerance;
        private readonly List<List<DrawPoint>> contours = [];
        private List<DrawPoint>? contour;
        private DrawPoint current;
        private DrawPoint start;
        private DrawPoint lastCubicControl;
        private DrawPoint lastQuadraticControl;
        private char previousSegment;
        private int index;

        public Parser(string data, float tolerance)
        {
            this.data = data;
            this.tolerance = tolerance;
        }

        public IReadOnlyList<DrawPoint[]> Parse()
        {
            char command = '\0';
            while (true)
            {
                SkipSeparators();
                if (index >= data.Length)
                {
                    break;
                }

                if (char.IsLetter(data[index]))
                {
                    command = data[index++];
                }
                else if (command == '\0')
                {
                    throw InvalidPath("Expected an SVG path command.");
                }

                bool relative = char.IsLower(command);
                switch (char.ToUpperInvariant(command))
                {
                    case 'M':
                        MoveTo(ReadPoint(relative));
                        previousSegment = 'M';
                        while (HasNumber())
                        {
                            LineTo(ReadPoint(relative));
                            previousSegment = 'L';
                        }
                        command = relative ? 'l' : 'L';
                        break;

                    case 'L':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            LineTo(ReadPoint(relative));
                            previousSegment = 'L';
                        }
                        break;

                    case 'H':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            float x = ReadNumber();
                            LineTo(new DrawPoint(relative ? current.X + x : x, current.Y));
                            previousSegment = 'H';
                        }
                        break;

                    case 'V':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            float y = ReadNumber();
                            LineTo(new DrawPoint(current.X, relative ? current.Y + y : y));
                            previousSegment = 'V';
                        }
                        break;

                    case 'C':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            DrawPoint first = ReadPoint(relative);
                            DrawPoint second = ReadPoint(relative);
                            DrawPoint end = ReadPoint(relative);
                            FlattenCubic(current, first, second, end, 0);
                            current = end;
                            lastCubicControl = second;
                            previousSegment = 'C';
                        }
                        break;

                    case 'S':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            DrawPoint first = previousSegment is 'C' or 'S'
                                ? Reflect(lastCubicControl, current)
                                : current;
                            DrawPoint second = ReadPoint(relative);
                            DrawPoint end = ReadPoint(relative);
                            FlattenCubic(current, first, second, end, 0);
                            current = end;
                            lastCubicControl = second;
                            previousSegment = 'S';
                        }
                        break;

                    case 'Q':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            DrawPoint control = ReadPoint(relative);
                            DrawPoint end = ReadPoint(relative);
                            FlattenQuadratic(current, control, end, 0);
                            current = end;
                            lastQuadraticControl = control;
                            previousSegment = 'Q';
                        }
                        break;

                    case 'T':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            DrawPoint control = previousSegment is 'Q' or 'T'
                                ? Reflect(lastQuadraticControl, current)
                                : current;
                            DrawPoint end = ReadPoint(relative);
                            FlattenQuadratic(current, control, end, 0);
                            current = end;
                            lastQuadraticControl = control;
                            previousSegment = 'T';
                        }
                        break;

                    case 'A':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            float radiusX = ReadNumber();
                            float radiusY = ReadNumber();
                            float rotation = ReadNumber();
                            bool largeArc = ReadFlag();
                            bool sweep = ReadFlag();
                            DrawPoint end = ReadPoint(relative);
                            FlattenArc(current, end, radiusX, radiusY, rotation, largeArc, sweep);
                            current = end;
                            previousSegment = 'A';
                        }
                        break;

                    case 'Z':
                        CloseContour();
                        previousSegment = 'Z';
                        command = '\0';
                        break;

                    default:
                        throw InvalidPath($"Unsupported SVG path command '{command}'.");
                }
            }

            return contours
                .Select(points => RemoveClosingDuplicate(points).ToArray())
                .Where(points => points.Length >= 3)
                .ToArray();
        }

        private void MoveTo(DrawPoint point)
        {
            contour = [];
            contours.Add(contour);
            current = point;
            start = point;
            contour.Add(point);
        }

        private void LineTo(DrawPoint point)
        {
            EnsureContour();
            AddPoint(point);
            current = point;
        }

        private void CloseContour()
        {
            EnsureContour();
            current = start;
        }

        private void FlattenCubic(DrawPoint first, DrawPoint control1, DrawPoint control2, DrawPoint end, int depth)
        {
            if (depth >= 12 ||
                (DistanceToLineSquared(control1, first, end) <= tolerance * tolerance &&
                 DistanceToLineSquared(control2, first, end) <= tolerance * tolerance))
            {
                AddPoint(end);
                return;
            }

            DrawPoint a = Midpoint(first, control1);
            DrawPoint b = Midpoint(control1, control2);
            DrawPoint c = Midpoint(control2, end);
            DrawPoint d = Midpoint(a, b);
            DrawPoint e = Midpoint(b, c);
            DrawPoint middle = Midpoint(d, e);
            FlattenCubic(first, a, d, middle, depth + 1);
            FlattenCubic(middle, e, c, end, depth + 1);
        }

        private void FlattenQuadratic(DrawPoint first, DrawPoint control, DrawPoint end, int depth)
        {
            if (depth >= 12 || DistanceToLineSquared(control, first, end) <= tolerance * tolerance)
            {
                AddPoint(end);
                return;
            }

            DrawPoint a = Midpoint(first, control);
            DrawPoint b = Midpoint(control, end);
            DrawPoint middle = Midpoint(a, b);
            FlattenQuadratic(first, a, middle, depth + 1);
            FlattenQuadratic(middle, b, end, depth + 1);
        }

        private void FlattenArc(
            DrawPoint first,
            DrawPoint end,
            float radiusX,
            float radiusY,
            float rotationDegrees,
            bool largeArc,
            bool sweep)
        {
            double rx = Math.Abs(radiusX);
            double ry = Math.Abs(radiusY);
            if (rx <= double.Epsilon || ry <= double.Epsilon || first == end)
            {
                AddPoint(end);
                return;
            }

            double phi = rotationDegrees * Math.PI / 180;
            double cosine = Math.Cos(phi);
            double sine = Math.Sin(phi);
            double halfX = (first.X - end.X) / 2d;
            double halfY = (first.Y - end.Y) / 2d;
            double transformedX = (cosine * halfX) + (sine * halfY);
            double transformedY = (-sine * halfX) + (cosine * halfY);
            double radiiScale = (transformedX * transformedX / (rx * rx)) +
                (transformedY * transformedY / (ry * ry));
            if (radiiScale > 1)
            {
                double scale = Math.Sqrt(radiiScale);
                rx *= scale;
                ry *= scale;
            }

            double numerator = Math.Max(0,
                ((rx * rx * ry * ry) - (rx * rx * transformedY * transformedY) -
                 (ry * ry * transformedX * transformedX)) /
                ((rx * rx * transformedY * transformedY) + (ry * ry * transformedX * transformedX)));
            double sign = largeArc == sweep ? -1 : 1;
            double factor = sign * Math.Sqrt(numerator);
            double centerXPrime = factor * (rx * transformedY / ry);
            double centerYPrime = factor * (-ry * transformedX / rx);
            double centerX = (cosine * centerXPrime) - (sine * centerYPrime) + ((first.X + end.X) / 2d);
            double centerY = (sine * centerXPrime) + (cosine * centerYPrime) + ((first.Y + end.Y) / 2d);

            double startAngle = VectorAngle(1, 0, (transformedX - centerXPrime) / rx, (transformedY - centerYPrime) / ry);
            double deltaAngle = VectorAngle(
                (transformedX - centerXPrime) / rx,
                (transformedY - centerYPrime) / ry,
                (-transformedX - centerXPrime) / rx,
                (-transformedY - centerYPrime) / ry);
            if (!sweep && deltaAngle > 0)
            {
                deltaAngle -= Math.PI * 2;
            }
            else if (sweep && deltaAngle < 0)
            {
                deltaAngle += Math.PI * 2;
            }

            double maximumRadius = Math.Max(rx, ry);
            double step = 2 * Math.Acos(Math.Clamp(1 - (tolerance / maximumRadius), -1, 1));
            if (!double.IsFinite(step) || step <= 0)
            {
                step = Math.PI / 16;
            }
            int segments = Math.Clamp((int)Math.Ceiling(Math.Abs(deltaAngle) / step), 1, 2048);
            for (int segment = 1; segment <= segments; segment++)
            {
                double angle = startAngle + (deltaAngle * segment / segments);
                double x = centerX + (cosine * rx * Math.Cos(angle)) - (sine * ry * Math.Sin(angle));
                double y = centerY + (sine * rx * Math.Cos(angle)) + (cosine * ry * Math.Sin(angle));
                AddPoint(segment == segments ? end : new DrawPoint((float)x, (float)y));
            }
        }

        private DrawPoint ReadPoint(bool relative)
        {
            float x = ReadNumber();
            float y = ReadNumber();
            return relative ? new DrawPoint(current.X + x, current.Y + y) : new DrawPoint(x, y);
        }

        private float ReadNumber()
        {
            SkipSeparators();
            int startIndex = index;
            if (index < data.Length && data[index] is '+' or '-')
            {
                index++;
            }

            bool hasDigits = false;
            while (index < data.Length && char.IsDigit(data[index]))
            {
                hasDigits = true;
                index++;
            }
            if (index < data.Length && data[index] == '.')
            {
                index++;
                while (index < data.Length && char.IsDigit(data[index]))
                {
                    hasDigits = true;
                    index++;
                }
            }
            if (!hasDigits)
            {
                throw InvalidPath("Expected a number.");
            }
            if (index < data.Length && data[index] is 'e' or 'E')
            {
                int exponent = index++;
                if (index < data.Length && data[index] is '+' or '-')
                {
                    index++;
                }
                int exponentDigits = index;
                while (index < data.Length && char.IsDigit(data[index]))
                {
                    index++;
                }
                if (exponentDigits == index)
                {
                    index = exponent;
                }
            }

            return float.Parse(data.AsSpan(startIndex, index - startIndex), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private bool ReadFlag()
        {
            SkipSeparators();
            if (index >= data.Length || data[index] is not ('0' or '1'))
            {
                throw InvalidPath("Expected an SVG arc flag.");
            }

            return data[index++] == '1';
        }

        private bool HasNumber()
        {
            SkipSeparators();
            return index < data.Length && !char.IsLetter(data[index]);
        }

        private void RequireNumbers(char command)
        {
            if (!HasNumber())
            {
                throw InvalidPath($"Command '{command}' requires parameters.");
            }
        }

        private void SkipSeparators()
        {
            while (index < data.Length && (char.IsWhiteSpace(data[index]) || data[index] == ','))
            {
                index++;
            }
        }

        private void EnsureContour()
        {
            if (contour is null)
            {
                throw InvalidPath("Path data must begin with a move command.");
            }
        }

        private void AddPoint(DrawPoint point)
        {
            EnsureContour();
            if (contour!.Count == 0 || contour[^1] != point)
            {
                contour.Add(point);
            }
        }

        private FormatException InvalidPath(string message)
        {
            return new FormatException($"{message} Path offset: {index}.");
        }

        private static IReadOnlyList<DrawPoint> RemoveClosingDuplicate(List<DrawPoint> points)
        {
            if (points.Count > 1 && points[0] == points[^1])
            {
                return points.GetRange(0, points.Count - 1);
            }
            return points;
        }

        private static DrawPoint Reflect(DrawPoint control, DrawPoint around)
        {
            return new DrawPoint((2 * around.X) - control.X, (2 * around.Y) - control.Y);
        }

        private static DrawPoint Midpoint(DrawPoint first, DrawPoint second)
        {
            return new DrawPoint((first.X + second.X) / 2, (first.Y + second.Y) / 2);
        }

        private static float DistanceToLineSquared(DrawPoint point, DrawPoint start, DrawPoint end)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float lengthSquared = (dx * dx) + (dy * dy);
            if (lengthSquared <= float.Epsilon)
            {
                float x = point.X - start.X;
                float y = point.Y - start.Y;
                return (x * x) + (y * y);
            }

            float cross = ((point.X - start.X) * dy) - ((point.Y - start.Y) * dx);
            return cross * cross / lengthSquared;
        }

        private static double VectorAngle(double ux, double uy, double vx, double vy)
        {
            double dot = (ux * vx) + (uy * vy);
            double length = Math.Sqrt(((ux * ux) + (uy * uy)) * ((vx * vx) + (vy * vy)));
            double angle = Math.Acos(Math.Clamp(dot / length, -1, 1));
            return ((ux * vy) - (uy * vx)) < 0 ? -angle : angle;
        }
    }
}
