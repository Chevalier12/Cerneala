using System.Globalization;

namespace Cerneala.Drawing;

public static class DrawPathParser
{
    public static DrawPath ParseSvg(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data);
        return new SvgParser(data).Parse();
    }

    private sealed class SvgParser
    {
        private readonly string data;
        private readonly DrawPathBuilder builder = new();
        private DrawPoint current;
        private DrawPoint start;
        private DrawPoint lastCubicControl;
        private DrawPoint lastQuadraticControl;
        private char previousSegment;
        private bool hasContour;
        private bool contourClosed;
        private int index;

        public SvgParser(string data) => this.data = data;

        public DrawPath Parse()
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
                            EnsureOpenContour();
                            DrawPoint first = ReadPoint(relative);
                            DrawPoint second = ReadPoint(relative);
                            DrawPoint end = ReadPoint(relative);
                            builder.CubicTo(first, second, end);
                            current = end;
                            lastCubicControl = second;
                            previousSegment = 'C';
                        }
                        break;
                    case 'S':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            EnsureOpenContour();
                            DrawPoint first = previousSegment is 'C' or 'S'
                                ? Reflect(lastCubicControl, current)
                                : current;
                            DrawPoint second = ReadPoint(relative);
                            DrawPoint end = ReadPoint(relative);
                            builder.CubicTo(first, second, end);
                            current = end;
                            lastCubicControl = second;
                            previousSegment = 'S';
                        }
                        break;
                    case 'Q':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            EnsureOpenContour();
                            DrawPoint control = ReadPoint(relative);
                            DrawPoint end = ReadPoint(relative);
                            builder.QuadraticTo(control, end);
                            current = end;
                            lastQuadraticControl = control;
                            previousSegment = 'Q';
                        }
                        break;
                    case 'T':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            EnsureOpenContour();
                            DrawPoint control = previousSegment is 'Q' or 'T'
                                ? Reflect(lastQuadraticControl, current)
                                : current;
                            DrawPoint end = ReadPoint(relative);
                            builder.QuadraticTo(control, end);
                            current = end;
                            lastQuadraticControl = control;
                            previousSegment = 'T';
                        }
                        break;
                    case 'A':
                        RequireNumbers(command);
                        while (HasNumber())
                        {
                            EnsureOpenContour();
                            float radiusX = MathF.Abs(ReadNumber());
                            float radiusY = MathF.Abs(ReadNumber());
                            float rotation = ReadNumber();
                            bool largeArc = ReadFlag();
                            bool sweep = ReadFlag();
                            DrawPoint end = ReadPoint(relative);
                            if (radiusX == 0 || radiusY == 0)
                            {
                                builder.LineTo(end);
                            }
                            else
                            {
                                builder.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, end);
                            }
                            current = end;
                            previousSegment = 'A';
                        }
                        break;
                    case 'Z':
                        EnsureOpenContour();
                        builder.Close();
                        current = start;
                        contourClosed = true;
                        previousSegment = 'Z';
                        command = '\0';
                        break;
                    default:
                        throw InvalidPath($"Unsupported SVG path command '{command}'.");
                }
            }

            try
            {
                return builder.Build();
            }
            catch (InvalidOperationException exception)
            {
                throw InvalidPath(exception.Message);
            }
        }

        private void MoveTo(DrawPoint point)
        {
            builder.MoveTo(point);
            current = point;
            start = point;
            hasContour = true;
            contourClosed = false;
        }

        private void LineTo(DrawPoint point)
        {
            EnsureOpenContour();
            builder.LineTo(point);
            current = point;
        }

        private void EnsureOpenContour()
        {
            if (!hasContour)
            {
                throw InvalidPath("Path data must begin with a move command.");
            }
            if (contourClosed)
            {
                throw InvalidPath("A new move command is required after closing a contour.");
            }
        }

        private DrawPoint ReadPoint(bool relative)
        {
            float x = ReadNumber();
            float y = ReadNumber();
            return relative
                ? new DrawPoint(current.X + x, current.Y + y)
                : new DrawPoint(x, y);
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

            return float.Parse(
                data.AsSpan(startIndex, index - startIndex),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
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
            while (index < data.Length &&
                (char.IsWhiteSpace(data[index]) || data[index] == ','))
            {
                index++;
            }
        }

        private FormatException InvalidPath(string message) =>
            new($"{message} Path offset: {index}.");

        private static DrawPoint Reflect(DrawPoint control, DrawPoint around) =>
            new((2 * around.X) - control.X, (2 * around.Y) - control.Y);
    }
}
