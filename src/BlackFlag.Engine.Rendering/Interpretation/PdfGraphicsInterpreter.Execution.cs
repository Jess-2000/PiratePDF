using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

public sealed partial class PdfGraphicsInterpreter
{
    private PdfDisplayList InterpretCore(
        IEnumerable<PdfGraphicsOperation> operations,
        PdfResourceScope resources,
        PdfGraphicsStateSnapshot initialState,
        HashSet<PdfResourceIdentity> activeForms,
        int formDepth)
    {
        var items = new List<PdfDisplayItem>();
        var stateStack = new Stack<PdfGraphicsStateSnapshot>();
        var path = new PathBuilder(options.MaximumPathSegmentCount);
        PdfGraphicsStateSnapshot state = initialState;
        PdfFillRule? pendingClip = null;
        PdfMarkedContentContext? markedContent = null;
        var pendingTextClipRuns = new List<PdfDisplayTextRun>();
        bool inTextObject = false;

        foreach (PdfGraphicsOperation operation in operations)
        {
            try
            {
                switch (operation.Operator.Code)
                {
                    case "q":
                        EnsureLimit(
                            stateStack.Count + 1,
                            options.MaximumGraphicsStateDepth,
                            operation,
                            "The graphics-state stack exceeded its configured depth.");
                        stateStack.Push(state);
                        break;

                    case "Q":
                        if (stateStack.Count == 0)
                        {
                            throw CreateException(
                                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                                operation,
                                "The graphics-state stack was restored without a matching save.");
                        }

                        state = stateStack.Pop();
                        break;

                    case "cm":
                        state = state with
                        {
                            Transform = state.Transform.Multiply(ReadMatrix(operation.Operands))
                        };
                        break;

                    case "w":
                        state = state with
                        {
                            LineWidth = ReadNonNegativeNumber(
                                operation.Operands[0],
                                operation,
                                "Line width")
                        };
                        break;

                    case "J":
                        state = state with
                        {
                            LineCap = ReadLineCap(operation.Operands[0], operation)
                        };
                        break;

                    case "j":
                        state = state with
                        {
                            LineJoin = ReadLineJoin(operation.Operands[0], operation)
                        };
                        break;

                    case "M":
                        state = state with
                        {
                            MiterLimit = ReadMiterLimit(
                                operation.Operands[0],
                                operation)
                        };
                        break;

                    case "d":
                        state = state with
                        {
                            DashPattern = ReadDashPattern(operation.Operands, operation)
                        };
                        break;

                    case "ri":
                        state = state with
                        {
                            RenderingIntent = ReadName(operation.Operands[0]).Value
                        };
                        break;

                    case "i":
                        state = state with
                        {
                            Flatness = ReadFlatness(
                                operation.Operands[0],
                                operation)
                        };
                        break;

                    case "gs":
                        state = ApplyExtGState(
                            state,
                            resources.ResolveExtGState(ReadName(operation.Operands[0])),
                            operation);
                        break;

                    case "m":
                        path.MoveTo(TransformPoint(state, operation.Operands, 0));
                        break;

                    case "l":
                        path.LineTo(TransformPoint(state, operation.Operands, 0), operation);
                        break;

                    case "c":
                        path.CubicTo(
                            TransformPoint(state, operation.Operands, 0),
                            TransformPoint(state, operation.Operands, 2),
                            TransformPoint(state, operation.Operands, 4),
                            operation);
                        break;

                    case "v":
                        path.CubicToUsingCurrentFirstControl(
                            TransformPoint(state, operation.Operands, 0),
                            TransformPoint(state, operation.Operands, 2),
                            operation);
                        break;

                    case "y":
                        path.CubicToUsingEndpointSecondControl(
                            TransformPoint(state, operation.Operands, 0),
                            TransformPoint(state, operation.Operands, 2),
                            operation);
                        break;

                    case "h":
                        path.Close(operation);
                        break;

                    case "re":
                        AddRectangle(path, state, operation);
                        break;

                    case "W":
                        pendingClip = PdfFillRule.Nonzero;
                        break;

                    case "W*":
                        pendingClip = PdfFillRule.EvenOdd;
                        break;

                    case "S":
                        PaintPath(false, true, PdfFillRule.Nonzero, false, operation);
                        break;

                    case "s":
                        PaintPath(false, true, PdfFillRule.Nonzero, true, operation);
                        break;

                    case "f":
                    case "F":
                        PaintPath(true, false, PdfFillRule.Nonzero, false, operation);
                        break;

                    case "f*":
                        PaintPath(true, false, PdfFillRule.EvenOdd, false, operation);
                        break;

                    case "B":
                        PaintPath(true, true, PdfFillRule.Nonzero, false, operation);
                        break;

                    case "B*":
                        PaintPath(true, true, PdfFillRule.EvenOdd, false, operation);
                        break;

                    case "b":
                        PaintPath(true, true, PdfFillRule.Nonzero, true, operation);
                        break;

                    case "b*":
                        PaintPath(true, true, PdfFillRule.EvenOdd, true, operation);
                        break;

                    case "n":
                        EndPath(operation);
                        break;

                    case "BT":
                        if (inTextObject)
                        {
                            throw CreateException(
                                PdfGraphicsInterpretationFailure.InvalidTextState,
                                operation,
                                "A BT operator occurred while a text object was already active.");
                        }

                        inTextObject = true;
                        pendingTextClipRuns.Clear();
                        state = state with
                        {
                            TextMatrix = PdfMatrix.Identity,
                            TextLineMatrix = PdfMatrix.Identity
                        };
                        break;

                    case "ET":
                        if (!inTextObject)
                        {
                            throw CreateException(
                                PdfGraphicsInterpretationFailure.InvalidTextState,
                                operation,
                                "An ET operator occurred outside a text object.");
                        }

                        inTextObject = false;

                        if (pendingTextClipRuns.Count > 0)
                        {
                            int depth = state.Clip is null
                                ? 1
                                : checked(state.Clip.Depth + 1);

                            EnsureLimit(
                                depth,
                                options.MaximumClipDepth,
                                operation,
                                "The clipping hierarchy exceeded its configured depth while applying text clipping.");

                            state = state with
                            {
                                Clip = new PdfClipState(
                                    pendingTextClipRuns,
                                    state.Clip)
                            };

                            pendingTextClipRuns.Clear();
                        }

                        break;

                    case "Tf":
                        RequireTextObject(inTextObject, operation);
                        state = state with
                        {
                            Font = resources.ResolveFont(ReadName(operation.Operands[0])),
                            FontSize = ReadNumber(operation.Operands[1])
                        };
                        break;

                    case "Tc":
                        RequireTextObject(inTextObject, operation);
                        state = state with { CharacterSpacing = ReadNumber(operation.Operands[0]) };
                        break;

                    case "Tw":
                        RequireTextObject(inTextObject, operation);
                        state = state with { WordSpacing = ReadNumber(operation.Operands[0]) };
                        break;

                    case "Tz":
                        RequireTextObject(inTextObject, operation);
                        state = state with { HorizontalScaling = ReadNumber(operation.Operands[0]) };
                        break;

                    case "TL":
                        RequireTextObject(inTextObject, operation);
                        state = state with { Leading = ReadNumber(operation.Operands[0]) };
                        break;

                    case "Tr":
                        RequireTextObject(inTextObject, operation);
                        state = state with
                        {
                            TextRenderingMode = ReadTextRenderingMode(
                                operation.Operands[0],
                                operation)
                        };
                        break;

                    case "Ts":
                        RequireTextObject(inTextObject, operation);
                        state = state with { TextRise = ReadNumber(operation.Operands[0]) };
                        break;

                    case "Td":
                        RequireTextObject(inTextObject, operation);
                        state = MoveTextLine(
                            state,
                            ReadNumber(operation.Operands[0]),
                            ReadNumber(operation.Operands[1]));
                        break;

                    case "TD":
                        RequireTextObject(inTextObject, operation);
                        double tx = ReadNumber(operation.Operands[0]);
                        double ty = ReadNumber(operation.Operands[1]);
                        state = state with { Leading = -ty };
                        state = MoveTextLine(state, tx, ty);
                        break;

                    case "Tm":
                        RequireTextObject(inTextObject, operation);
                        PdfMatrix textMatrix = ReadMatrix(operation.Operands);
                        state = state with
                        {
                            TextMatrix = textMatrix,
                            TextLineMatrix = textMatrix
                        };
                        break;

                    case "T*":
                        RequireTextObject(inTextObject, operation);
                        state = MoveTextLine(state, 0, -state.Leading);
                        break;

                    case "Tj":
                        RequireTextObject(inTextObject, operation);
                        AddTextRun(operation.Operands[0], operation);
                        break;

                    case "TJ":
                        RequireTextObject(inTextObject, operation);
                        AddTextArray((PdfArray)operation.Operands[0], operation);
                        break;

                    case "'":
                        RequireTextObject(inTextObject, operation);
                        state = MoveTextLine(state, 0, -state.Leading);
                        AddTextRun(operation.Operands[0], operation);
                        break;

                    case "\"":
                        RequireTextObject(inTextObject, operation);
                        state = state with
                        {
                            WordSpacing = ReadNumber(operation.Operands[0]),
                            CharacterSpacing = ReadNumber(operation.Operands[1])
                        };
                        state = MoveTextLine(state, 0, -state.Leading);
                        AddTextRun(operation.Operands[2], operation);
                        break;

                    case "G":
                        state = state with
                        {
                            StrokeColor = CreateDeviceColor("DeviceGray", operation.Operands)
                        };
                        break;

                    case "g":
                        state = state with
                        {
                            FillColor = CreateDeviceColor("DeviceGray", operation.Operands)
                        };
                        break;

                    case "RG":
                        state = state with
                        {
                            StrokeColor = CreateDeviceColor("DeviceRGB", operation.Operands)
                        };
                        break;

                    case "rg":
                        state = state with
                        {
                            FillColor = CreateDeviceColor("DeviceRGB", operation.Operands)
                        };
                        break;

                    case "K":
                        state = state with
                        {
                            StrokeColor = CreateDeviceColor("DeviceCMYK", operation.Operands)
                        };
                        break;

                    case "k":
                        state = state with
                        {
                            FillColor = CreateDeviceColor("DeviceCMYK", operation.Operands)
                        };
                        break;

                    case "CS":
                        state = state with
                        {
                            StrokeColor = ResolveColorSpace(resources, ReadName(operation.Operands[0]))
                        };
                        break;

                    case "cs":
                        state = state with
                        {
                            FillColor = ResolveColorSpace(resources, ReadName(operation.Operands[0]))
                        };
                        break;

                    case "SC":
                    case "SCN":
                        state = state with
                        {
                            StrokeColor = ReplaceColorComponents(
                                state.StrokeColor,
                                operation.Operands,
                                resources)
                        };
                        break;

                    case "sc":
                    case "scn":
                        state = state with
                        {
                            FillColor = ReplaceColorComponents(
                                state.FillColor,
                                operation.Operands,
                                resources)
                        };
                        break;

                    case "Do":
                        AddXObject(resources, ReadName(operation.Operands[0]), operation);
                        break;

                    case "BI":
                        if (operation.InlineImage is null)
                        {
                            throw CreateException(
                                PdfGraphicsInterpretationFailure.MalformedGraphicsContent,
                                operation,
                                "The BI operation does not contain inline-image data.");
                        }

                        AddItem(
                            new PdfDisplayImage(
                                null,
                                operation.InlineImage,
                                state.Transform,
                                state,
                                operation.SourceSpan,
                                markedContent),
                            operation);
                        break;

                    case "sh":
                        AddItem(
                            new PdfDisplayShading(
                                resources.ResolveShading(ReadName(operation.Operands[0])),
                                state,
                                operation.SourceSpan,
                                markedContent),
                            operation);
                        break;

                    case "MP":
                        AddItem(
                            new PdfDisplayMarkedContentPoint(
                                ReadName(operation.Operands[0]),
                                null,
                                null,
                                state,
                                operation.SourceSpan,
                                markedContent),
                            operation);
                        break;

                    case "DP":
                        PdfObject pointProperties = operation.Operands[1];
                        PdfResolvedProperties? pointResolved =
                            pointProperties is PdfName pointPropertyName
                                ? resources.ResolveProperties(pointPropertyName)
                                : null;
                        AddItem(
                            new PdfDisplayMarkedContentPoint(
                                ReadName(operation.Operands[0]),
                                pointProperties,
                                pointResolved,
                                state,
                                operation.SourceSpan,
                                markedContent),
                            operation);
                        break;

                    case "BMC":
                        BeginMarkedContent(
                            ReadName(operation.Operands[0]),
                            null,
                            null,
                            operation);
                        break;

                    case "BDC":
                        PdfName tag = ReadName(operation.Operands[0]);
                        PdfObject properties = operation.Operands[1];
                        PdfResolvedProperties? resolved = properties is PdfName propertyName
                            ? resources.ResolveProperties(propertyName)
                            : null;
                        BeginMarkedContent(tag, properties, resolved, operation);
                        break;

                    case "EMC":
                        if (markedContent is null)
                        {
                            throw CreateException(
                                PdfGraphicsInterpretationFailure.InvalidMarkedContent,
                                operation,
                                "An EMC operator occurred without an active marked-content sequence.");
                        }

                        markedContent = markedContent.Parent;
                        break;

                    case "BX":
                    case "EX":
                    case "d0":
                    case "d1":
                        // These operations either affect later rendering policies,
                        // Type 3 glyph metrics, or compatibility behavior but do not
                        // produce a semantic display item in BF-1047.
                        break;

                    default:
                        if (!options.IgnoreUnknownOperations)
                        {
                            throw CreateException(
                                PdfGraphicsInterpretationFailure.UnsupportedOperation,
                                operation,
                                $"Graphics operator '{operation.Operator.Code}' is not interpreted.");
                        }

                        break;
                }
            }
            catch (PdfGraphicsInterpretationException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                or InvalidCastException
                or InvalidOperationException
                or KeyNotFoundException
                or OverflowException)
            {
                throw CreateException(
                    PdfGraphicsInterpretationFailure.MalformedGraphicsContent,
                    operation,
                    $"Graphics operator '{operation.Operator.Code}' could not be interpreted.",
                    exception);
            }
        }

        if (stateStack.Count != 0)
        {
            throw new PdfGraphicsInterpretationException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                default,
                "The graphics-state stack is not balanced at the end of interpretation.");
        }

        if (inTextObject)
        {
            throw new PdfGraphicsInterpretationException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                default,
                "The content ended while a text object was active.");
        }

        if (markedContent is not null)
        {
            throw new PdfGraphicsInterpretationException(
                PdfGraphicsInterpretationFailure.InvalidMarkedContent,
                default,
                "The content ended while marked content was still active.");
        }

        return new PdfDisplayList(items);

        void PaintPath(
            bool fill,
            bool stroke,
            PdfFillRule fillRule,
            bool close,
            PdfGraphicsOperation operation)
        {
            // A path-painting operator with no current path has no visible
            // effect. Treat it as a no-op rather than manufacturing geometry
            // or failing interpretation.
            if (path.IsEmpty)
            {
                pendingClip = null;
                return;
            }

            if (close)
            {
                path.Close(operation);
            }

            PdfPathGeometry geometry = path.ToGeometry(operation);
            ApplyPendingClip(geometry, operation);

            AddItem(
                new PdfDisplayPath(
                    geometry,
                    fill,
                    stroke,
                    fillRule,
                    state,
                    operation.SourceSpan,
                    markedContent),
                operation);

            path.Clear();
            pendingClip = null;
        }

        void EndPath(PdfGraphicsOperation operation)
        {
            if (!path.IsEmpty)
            {
                PdfPathGeometry geometry = path.ToGeometry(operation);
                ApplyPendingClip(geometry, operation);
            }

            path.Clear();
            pendingClip = null;
        }

        void ApplyPendingClip(
            PdfPathGeometry geometry,
            PdfGraphicsOperation operation)
        {
            if (!pendingClip.HasValue)
            {
                return;
            }

            int depth = state.Clip is null ? 1 : checked(state.Clip.Depth + 1);
            EnsureLimit(
                depth,
                options.MaximumClipDepth,
                operation,
                "The clipping hierarchy exceeded its configured depth.");

            state = state with
            {
                Clip = new PdfClipState(
                    geometry,
                    pendingClip.Value,
                    state.Clip)
            };
        }

        void AddTextRun(
            PdfObject textObject,
            PdfGraphicsOperation operation)
        {
            PdfResolvedFont font = RequireFont(state, operation);
            byte[] bytes = ReadTextBytes(textObject, operation);

            // Content-stream strings contain font character codes, not
            // intrinsically Unicode text. Preserve the exact bytes and leave
            // Unicode null until a font/CMap decoding service can derive it.
            PdfTextRunElement element =
                PdfTextRunElement.CreateText(bytes);

            PdfGraphicsStateSnapshot runState = state;

            var displayRun = new PdfDisplayTextRun(
                font,
                state.FontSize,
                [element],
                state.TextMatrix.Multiply(state.Transform),
                runState,
                operation.SourceSpan,
                markedContent);

            AddItem(displayRun, operation);

            if (runState.TextRenderingMode >= 4)
            {
                pendingTextClipRuns.Add(displayRun);
            }

            state = AdvanceTextMatrix(
                state,
                font,
                [element],
                operation);
        }

        void AddTextArray(
            PdfArray array,
            PdfGraphicsOperation operation)
        {
            PdfResolvedFont font = RequireFont(state, operation);
            var elements = new List<PdfTextRunElement>();

            foreach (PdfObject value in array)
            {
                switch (value)
                {
                    case PdfLiteralString literal:
                        elements.Add(
                            PdfTextRunElement.CreateText(
                                Encoding.Latin1.GetBytes(literal.Value)));
                        break;

                    case PdfHexString hex:
                        elements.Add(
                            PdfTextRunElement.CreateText(hex.Value.Span));
                        break;

                    case PdfInteger integer:
                        elements.Add(
                            PdfTextRunElement.CreateAdjustment(integer.Value));
                        break;

                    case PdfReal real:
                        elements.Add(
                            PdfTextRunElement.CreateAdjustment(real.Value));
                        break;

                    default:
                        throw CreateException(
                            PdfGraphicsInterpretationFailure.InvalidTextState,
                            operation,
                            "A TJ array may contain only strings and numeric adjustments.");
                }
            }

            PdfGraphicsStateSnapshot runState = state;

            var displayRun = new PdfDisplayTextRun(
                font,
                state.FontSize,
                elements,
                state.TextMatrix.Multiply(state.Transform),
                runState,
                operation.SourceSpan,
                markedContent);

            AddItem(displayRun, operation);

            if (runState.TextRenderingMode >= 4)
            {
                pendingTextClipRuns.Add(displayRun);
            }

            state = AdvanceTextMatrix(
                state,
                font,
                elements,
                operation);
        }

        void AddXObject(
            PdfResourceScope scope,
            PdfName name,
            PdfGraphicsOperation operation)
        {
            PdfResolvedXObject xObject = scope.ResolveXObject(name);
            switch (xObject)
            {
                case PdfResolvedImageXObject image:
                    AddItem(
                        new PdfDisplayImage(
                            image,
                            null,
                            state.Transform,
                            state,
                            operation.SourceSpan,
                            markedContent),
                        operation);
                    break;

                case PdfResolvedFormXObject form:
                    AddForm(form, scope, operation);
                    break;

                default:
                    throw CreateException(
                        PdfGraphicsInterpretationFailure.InvalidResource,
                        operation,
                        $"XObject '/{name.Value}' has an unsupported canonical type.");
            }
        }

        void AddForm(
            PdfResolvedFormXObject form,
            PdfResourceScope parentScope,
            PdfGraphicsOperation operation)
        {
            if (formOperationProvider is null)
            {
                throw CreateException(
                    PdfGraphicsInterpretationFailure.InvalidResource,
                    operation,
                    "A Form XObject requires an IPdfFormOperationProvider.");
            }

            int nextDepth = checked(formDepth + 1);
            EnsureLimit(
                nextDepth,
                options.MaximumFormDepth,
                operation,
                "Form XObject nesting exceeded its configured depth.");

            if (!activeForms.Add(form.Identity))
            {
                throw CreateException(
                    PdfGraphicsInterpretationFailure.RecursiveForm,
                    operation,
                    "Recursive Form XObject interpretation was detected.");
            }

            try
            {
                // BF-1046 constructs Form resource scopes with the parent
                // scope already attached, so nested lookup remains correct
                // for Forms that define resources and for legacy Forms that
                // fall back to their invoking scope.
                PdfResourceScope nestedScope =
                    form.GetResourceScope()
                    ?? parentScope;

                PdfMatrix formMatrix = form.Matrix is null
                    ? PdfMatrix.Identity
                    : ReadMatrix(form.Matrix);

                PdfMatrix effective =
                    state.Transform.Multiply(formMatrix);

                PdfPathGeometry formBounds =
                    CreateRectangleGeometry(
                        form.BoundingBox,
                        effective);

                int clipDepth =
                    state.Clip is null
                        ? 1
                        : checked(state.Clip.Depth + 1);

                EnsureLimit(
                    clipDepth,
                    options.MaximumClipDepth,
                    operation,
                    "The clipping hierarchy exceeded its configured depth while entering a Form XObject.");

                PdfGraphicsStateSnapshot nestedState = state with
                {
                    Transform = effective,
                    Clip = new PdfClipState(
                        formBounds,
                        PdfFillRule.Nonzero,
                        state.Clip)
                };

                PdfDisplayList nested = InterpretCore(
                    formOperationProvider.GetOperations(form, nestedScope),
                    nestedScope,
                    nestedState,
                    activeForms,
                    nextDepth);

                AddItem(
                    new PdfDisplayForm(
                        form,
                        effective,
                        nested,
                        state,
                        operation.SourceSpan,
                        markedContent),
                    operation);
            }
            finally
            {
                activeForms.Remove(form.Identity);
            }
        }

        void BeginMarkedContent(
            PdfName tag,
            PdfObject? properties,
            PdfResolvedProperties? resolvedProperties,
            PdfGraphicsOperation operation)
        {
            int depth = markedContent is null
                ? 1
                : checked(markedContent.Depth + 1);

            EnsureLimit(
                depth,
                options.MaximumMarkedContentDepth,
                operation,
                "Marked-content nesting exceeded its configured depth.");

            markedContent = new PdfMarkedContentContext(
                tag,
                properties,
                resolvedProperties,
                markedContent);
        }

        void AddItem(
            PdfDisplayItem item,
            PdfGraphicsOperation operation)
        {
            EnsureLimit(
                items.Count + 1,
                options.MaximumDisplayItemCount,
                operation,
                "The display list exceeded its configured item limit.");
            items.Add(item);
        }
    }
}
