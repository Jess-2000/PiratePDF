using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;

namespace BlackFlag.Engine.Rendering.Interpretation;

public sealed partial class PdfGraphicsInterpreter
{
    private sealed class PathBuilder
    {
        private readonly List<PdfPathSegment> segments = [];
        private readonly int maximumSegments;
        private PdfPoint? currentPoint;
        private PdfPoint? subpathStart;

        public PathBuilder(int maximumSegments)
        {
            maximumSegments = Math.Max(1, maximumSegments);
            this.maximumSegments = maximumSegments;
        }

        public bool IsEmpty => segments.Count == 0;

        public void MoveTo(PdfPoint point)
        {
            Add(new PdfPathSegment(PdfPathSegmentKind.MoveTo, point));
            currentPoint = point;
            subpathStart = point;
        }

        public void LineTo(
            PdfPoint point,
            PdfGraphicsOperation operation)
        {
            RequireCurrentPoint(operation);
            Add(new PdfPathSegment(PdfPathSegmentKind.LineTo, point));
            currentPoint = point;
        }

        public void CubicTo(
            PdfPoint control1,
            PdfPoint control2,
            PdfPoint end,
            PdfGraphicsOperation operation)
        {
            RequireCurrentPoint(operation);
            Add(
                new PdfPathSegment(
                    PdfPathSegmentKind.CubicBezier,
                    end,
                    control1,
                    control2));
            currentPoint = end;
        }

        public void CubicToUsingCurrentFirstControl(
            PdfPoint control2,
            PdfPoint end,
            PdfGraphicsOperation operation)
        {
            PdfPoint current = RequireCurrentPoint(operation);
            CubicTo(current, control2, end, operation);
        }

        public void CubicToUsingEndpointSecondControl(
            PdfPoint control1,
            PdfPoint end,
            PdfGraphicsOperation operation) =>
            CubicTo(control1, end, end, operation);

        public void Close(PdfGraphicsOperation operation)
        {
            RequireCurrentPoint(operation);
            if (subpathStart is not PdfPoint start)
            {
                throw CreateException(
                    PdfGraphicsInterpretationFailure.InvalidPathState,
                    operation,
                    "The current path has no open subpath to close.");
            }

            Add(new PdfPathSegment(PdfPathSegmentKind.ClosePath, start));
            currentPoint = start;
        }

        public PdfPathGeometry ToGeometry(PdfGraphicsOperation operation)
        {
            if (segments.Count == 0)
            {
                throw CreateException(
                    PdfGraphicsInterpretationFailure.InvalidPathState,
                    operation,
                    "A path painting operation occurred without a current path.");
            }

            return new PdfPathGeometry(segments);
        }

        public void Clear()
        {
            segments.Clear();
            currentPoint = null;
            subpathStart = null;
        }

        private PdfPoint RequireCurrentPoint(PdfGraphicsOperation operation) =>
            currentPoint
            ?? throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidPathState,
                operation,
                "A path segment requires an established current point.");

        private void Add(PdfPathSegment segment)
        {
            if (segments.Count >= maximumSegments)
            {
                throw new InvalidOperationException(
                    "The current path exceeded its configured segment limit.");
            }

            segments.Add(segment);
        }
    }
}
