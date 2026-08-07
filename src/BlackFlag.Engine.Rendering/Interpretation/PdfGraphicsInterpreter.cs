using System.Text;
using BlackFlag.Engine.Objects;
using BlackFlag.Engine.Rendering.DisplayList;
using BlackFlag.Engine.Rendering.Graphics;
using BlackFlag.Engine.Rendering.Resources;

namespace BlackFlag.Engine.Rendering.Interpretation;

/// <summary>
/// Interprets canonical PDF graphics operations into an immutable semantic
/// display list without rasterizing paths, glyphs, images, or colors.
/// </summary>
public sealed class PdfGraphicsInterpreter
{
    private readonly PdfGraphicsInterpretationOptions options;
    private readonly IPdfFormOperationProvider? formOperationProvider;

    /// <summary>Initializes a graphics interpreter.</summary>
    public PdfGraphicsInterpreter(
        PdfGraphicsInterpretationOptions? options = null,
        IPdfFormOperationProvider? formOperationProvider = null)
    {
        this.options = options ?? new PdfGraphicsInterpretationOptions();
        this.options.Validate();
        this.formOperationProvider = formOperationProvider;
    }

    /// <summary>
    /// Interprets canonical operations using the supplied active resource scope.
    /// </summary>
    public PdfDisplayList Interpret(
        IEnumerable<PdfGraphicsOperation> operations,
        PdfResourceScope resources)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(resources);

        var activeForms = new HashSet<PdfResourceIdentity>();
        return InterpretCore(
            operations,
            resources,
            new PdfGraphicsStateSnapshot(),
            activeForms,
            formDepth: 0);
    }

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

            AddItem(
                new PdfDisplayTextRun(
                    font,
                    state.FontSize,
                    [element],
                    state.TextMatrix.Multiply(state.Transform),
                    runState,
                    operation.SourceSpan,
                    markedContent),
                operation);

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

            AddItem(
                new PdfDisplayTextRun(
                    font,
                    state.FontSize,
                    elements,
                    state.TextMatrix.Multiply(state.Transform),
                    runState,
                    operation.SourceSpan,
                    markedContent),
                operation);

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

    private static PdfGraphicsStateSnapshot ApplyExtGState(
        PdfGraphicsStateSnapshot state,
        PdfResolvedExtGState extGState,
        PdfGraphicsOperation operation)
    {
        double strokingAlpha = extGState.StrokingAlpha ?? state.StrokingAlpha;
        double nonstrokingAlpha = extGState.NonstrokingAlpha ?? state.NonstrokingAlpha;

        if (strokingAlpha is < 0 or > 1 || nonstrokingAlpha is < 0 or > 1)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "An ExtGState alpha value is outside the valid zero-to-one range.");
        }

        return state with
        {
            StrokingAlpha = strokingAlpha,
            NonstrokingAlpha = nonstrokingAlpha,
            OverprintStroke = extGState.OverprintStroke ?? state.OverprintStroke,
            OverprintFill = extGState.OverprintFill ?? state.OverprintFill,
            OverprintMode = extGState.OverprintMode ?? state.OverprintMode,
            BlendMode = extGState.BlendMode ?? state.BlendMode,
            SoftMask = extGState.SoftMask ?? state.SoftMask
        };
    }

    private static PdfGraphicsStateSnapshot MoveTextLine(
        PdfGraphicsStateSnapshot state,
        double x,
        double y)
    {
        PdfMatrix translation = new(1, 0, 0, 1, x, y);
        PdfMatrix line = state.TextLineMatrix.Multiply(translation);
        return state with
        {
            TextLineMatrix = line,
            TextMatrix = line
        };
    }

    private static PdfResolvedFont RequireFont(
        PdfGraphicsStateSnapshot state,
        PdfGraphicsOperation operation) =>
        state.Font
        ?? throw CreateException(
            PdfGraphicsInterpretationFailure.InvalidTextState,
            operation,
            "Text showing requires an active font selected by Tf.");

    private static void RequireTextObject(
        bool inTextObject,
        PdfGraphicsOperation operation)
    {
        if (!inTextObject)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                $"Text operator '{operation.Operator.Code}' occurred outside BT/ET.");
        }
    }

    private static PdfColorValue ResolveColorSpace(
        PdfResourceScope resources,
        PdfName name)
    {
        return name.Value switch
        {
            "DeviceGray" => new PdfColorValue("DeviceGray", [0]),
            "DeviceRGB" => new PdfColorValue("DeviceRGB", [0, 0, 0]),
            "DeviceCMYK" => new PdfColorValue("DeviceCMYK", [0, 0, 0, 1]),
            _ => CreateResolvedColorSpace(resources.ResolveColorSpace(name))
        };
    }

    private static PdfColorValue CreateResolvedColorSpace(
        PdfResolvedColorSpace colorSpace) =>
        new(
            colorSpace.ColorSpaceKind.ToString(),
            new double[Math.Max(0, colorSpace.ComponentCount)],
            colorSpace);

    private static PdfColorValue CreateDeviceColor(
        string name,
        IReadOnlyList<PdfObject> operands) =>
        new(name, operands.Select(ReadNumber));

    private static PdfColorValue ReplaceColorComponents(
        PdfColorValue current,
        IReadOnlyList<PdfObject> operands,
        PdfResourceScope resources)
    {
        PdfObject? lastOperand = operands.Count > 0 ? operands[operands.Count - 1] : null;
        PdfResolvedPattern? pattern = lastOperand is PdfName patternName
            ? resources.ResolvePattern(patternName)
            : current.Pattern;

        IEnumerable<PdfObject> numericOperands = operands.Where(IsNumber);
        return new PdfColorValue(
            current.ColorSpaceName,
            numericOperands.Select(ReadNumber),
            current.Resource,
            pattern);
    }

    private static bool IsNumber(PdfObject value) =>
        value is PdfInteger or PdfReal;

    private static PdfLineCapStyle ReadLineCap(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        long number = ReadInteger(value);
        return number switch
        {
            0 => PdfLineCapStyle.Butt,
            1 => PdfLineCapStyle.Round,
            2 => PdfLineCapStyle.ProjectingSquare,
            _ => throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                $"Line-cap value {number} is invalid.")
        };
    }

    private static PdfLineJoinStyle ReadLineJoin(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        long number = ReadInteger(value);
        return number switch
        {
            0 => PdfLineJoinStyle.Miter,
            1 => PdfLineJoinStyle.Round,
            2 => PdfLineJoinStyle.Bevel,
            _ => throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                $"Line-join value {number} is invalid.")
        };
    }

    private static PdfDashPattern ReadDashPattern(
        IReadOnlyList<PdfObject> operands,
        PdfGraphicsOperation operation)
    {
        if (operands[0] is not PdfArray array)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "The d operator requires an array of dash lengths.");
        }

        double[] elements = array.Select(ReadNumber).ToArray();
        if (elements.Any(static value => value < 0))
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "Dash lengths cannot be negative.");
        }

        double phase = ReadNumber(operands[1]);
        return new PdfDashPattern(elements, phase);
    }

    private static void AddRectangle(
        PathBuilder path,
        PdfGraphicsStateSnapshot state,
        PdfGraphicsOperation operation)
    {
        double x = ReadNumber(operation.Operands[0]);
        double y = ReadNumber(operation.Operands[1]);
        double width = ReadNumber(operation.Operands[2]);
        double height = ReadNumber(operation.Operands[3]);

        PdfPoint p0 = state.Transform.Transform(new PdfPoint(x, y));
        PdfPoint p1 = state.Transform.Transform(new PdfPoint(x + width, y));
        PdfPoint p2 = state.Transform.Transform(new PdfPoint(x + width, y + height));
        PdfPoint p3 = state.Transform.Transform(new PdfPoint(x, y + height));

        path.MoveTo(p0);
        path.LineTo(p1, operation);
        path.LineTo(p2, operation);
        path.LineTo(p3, operation);
        path.Close(operation);
    }

    private static PdfPoint TransformPoint(
        PdfGraphicsStateSnapshot state,
        IReadOnlyList<PdfObject> operands,
        int offset) =>
        state.Transform.Transform(
            new PdfPoint(
                ReadNumber(operands[offset]),
                ReadNumber(operands[offset + 1])));

    private static PdfMatrix ReadMatrix(IReadOnlyList<PdfObject> operands)
    {
        if (operands.Count < 6)
        {
            throw new ArgumentException("A PDF matrix requires six numeric values.");
        }

        return new PdfMatrix(
            ReadNumber(operands[0]),
            ReadNumber(operands[1]),
            ReadNumber(operands[2]),
            ReadNumber(operands[3]),
            ReadNumber(operands[4]),
            ReadNumber(operands[5]));
    }

    private static double ReadNumber(PdfObject value) =>
        value switch
        {
            PdfInteger integer => integer.Value,
            PdfReal real => real.Value,
            _ => throw new ArgumentException("A numeric PDF operand was required.", nameof(value))
        };

    private static long ReadInteger(PdfObject value) =>
        value is PdfInteger integer
            ? integer.Value
            : throw new ArgumentException("An integer PDF operand was required.", nameof(value));

    private static PdfName ReadName(PdfObject value) =>
        value as PdfName
        ?? throw new ArgumentException("A PDF name operand was required.", nameof(value));

    private static byte[] ReadTextBytes(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        return value switch
        {
            // PdfObjectParser stores literal-string bytes through a Latin-1
            // round-trip, so this reconstructs the exact decoded byte values
            // without claiming those bytes are Unicode characters.
            PdfLiteralString literal =>
                Encoding.Latin1.GetBytes(literal.Value),

            PdfHexString hex =>
                hex.Value.ToArray(),

            _ => throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                "A text-showing operator requires a literal or hexadecimal string.")
        };
    }

    private static PdfGraphicsStateSnapshot AdvanceTextMatrix(
        PdfGraphicsStateSnapshot state,
        PdfResolvedFont font,
        IEnumerable<PdfTextRunElement> elements,
        PdfGraphicsOperation operation)
    {
        double horizontalScale =
            state.HorizontalScaling / 100.0;

        double translation = 0;

        foreach (PdfTextRunElement element in elements)
        {
            if (element.Kind == PdfTextRunElementKind.Adjustment)
            {
                translation +=
                    (-element.Adjustment / 1000.0)
                    * state.FontSize
                    * horizontalScale;

                continue;
            }

            translation += CalculateEncodedTextAdvance(
                font,
                element.EncodedBytes.Span,
                state,
                horizontalScale,
                operation);
        }

        if (translation == 0)
        {
            return state;
        }

        PdfMatrix advance =
            new(1, 0, 0, 1, translation, 0);

        return state with
        {
            TextMatrix =
                state.TextMatrix.Multiply(advance)
        };
    }

    private static double CalculateEncodedTextAdvance(
        PdfResolvedFont font,
        ReadOnlySpan<byte> encodedBytes,
        PdfGraphicsStateSnapshot state,
        double horizontalScale,
        PdfGraphicsOperation operation)
    {
        if (encodedBytes.IsEmpty)
        {
            return 0;
        }

        return string.Equals(
            font.Subtype,
            "Type0",
            StringComparison.Ordinal)
                ? CalculateCompositeFontAdvance(
                    font,
                    encodedBytes,
                    state,
                    horizontalScale,
                    operation)
                : CalculateSimpleFontAdvance(
                    font,
                    encodedBytes,
                    state,
                    horizontalScale);
    }

    private static double CalculateSimpleFontAdvance(
        PdfResolvedFont font,
        ReadOnlySpan<byte> encodedBytes,
        PdfGraphicsStateSnapshot state,
        double horizontalScale)
    {
        int firstCharacter =
            TryGetInteger(font.Dictionary, "FirstChar")
            ?? 0;

        PdfArray? widths =
            TryGetArray(font.Dictionary, "Widths");

        double missingWidth =
            TryGetNumber(
                font.Descriptor,
                "MissingWidth")
            ?? 0;

        double total = 0;

        foreach (byte characterCode in encodedBytes)
        {
            int widthIndex =
                characterCode - firstCharacter;

            double glyphWidth =
                widths is not null
                && widthIndex >= 0
                && widthIndex < widths.Count
                && TryReadNumber(widths[widthIndex], out double explicitWidth)
                    ? explicitWidth
                    : missingWidth;

            double spacing =
                state.CharacterSpacing;

            if (characterCode == 0x20)
            {
                spacing +=
                    state.WordSpacing;
            }

            total +=
                ((glyphWidth / 1000.0) * state.FontSize + spacing)
                * horizontalScale;
        }

        return total;
    }

    private static double CalculateCompositeFontAdvance(
        PdfResolvedFont font,
        ReadOnlySpan<byte> encodedBytes,
        PdfGraphicsStateSnapshot state,
        double horizontalScale,
        PdfGraphicsOperation operation)
    {
        if (font.Encoding is not PdfName encoding
            || !string.Equals(
                encoding.Value,
                "Identity-H",
                StringComparison.Ordinal))
        {
            // BF-1046 preserves arbitrary CMap resources but does not yet
            // expose a decoded CMap service. Do not invent character-code
            // boundaries for an arbitrary composite-font encoding.
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                $"Composite font '{font.FaceName}' uses an encoding that requires decoded CMap support before text advancement can be interpreted.");
        }

        if ((encodedBytes.Length & 1) != 0)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                "Identity-H text contains an odd number of bytes.");
        }

        PdfDictionary? descendant =
            GetDirectDescendantFont(font);

        double defaultWidth =
            TryGetNumber(
                descendant,
                "DW")
            ?? 1000;

        PdfArray? widths =
            descendant is null
                ? null
                : TryGetArray(descendant, "W");

        double total = 0;

        for (int index = 0; index < encodedBytes.Length; index += 2)
        {
            int cid =
                (encodedBytes[index] << 8)
                | encodedBytes[index + 1];

            double glyphWidth =
                TryGetCidWidth(
                    widths,
                    cid,
                    out double explicitWidth)
                    ? explicitWidth
                    : defaultWidth;

            total +=
                ((glyphWidth / 1000.0) * state.FontSize
                    + state.CharacterSpacing)
                * horizontalScale;
        }

        return total;
    }

    private static PdfDictionary? GetDirectDescendantFont(
        PdfResolvedFont font)
    {
        if (font.DescendantFonts is null
            || font.DescendantFonts.Count == 0)
        {
            return null;
        }

        return font.DescendantFonts[0] as PdfDictionary;
    }

    private static bool TryGetCidWidth(
        PdfArray? widths,
        int cid,
        out double width)
    {
        width = 0;

        if (widths is null)
        {
            return false;
        }

        int index = 0;

        while (index < widths.Count)
        {
            if (!TryReadInteger(
                widths[index],
                out long firstCodeLong))
            {
                return false;
            }

            if (firstCodeLong is < 0 or > int.MaxValue)
            {
                return false;
            }

            int firstCode =
                checked((int)firstCodeLong);

            index++;

            if (index >= widths.Count)
            {
                return false;
            }

            if (widths[index] is PdfArray sequentialWidths)
            {
                int widthIndex =
                    cid - firstCode;

                if (widthIndex >= 0
                    && widthIndex < sequentialWidths.Count
                    && TryReadNumber(
                        sequentialWidths[widthIndex],
                        out width))
                {
                    return true;
                }

                index++;
                continue;
            }

            if (!TryReadInteger(
                    widths[index],
                    out long lastCodeLong)
                || lastCodeLong is < 0 or > int.MaxValue)
            {
                return false;
            }

            int lastCode =
                checked((int)lastCodeLong);

            index++;

            if (index >= widths.Count
                || !TryReadNumber(
                    widths[index],
                    out double rangeWidth))
            {
                return false;
            }

            if (cid >= firstCode
                && cid <= lastCode)
            {
                width = rangeWidth;
                return true;
            }

            index++;
        }

        return false;
    }

    private static PdfPathGeometry CreateRectangleGeometry(
        PdfArray rectangle,
        PdfMatrix transform)
    {
        if (rectangle.Count != 4)
        {
            throw new ArgumentException(
                "A PDF rectangle requires exactly four numeric values.",
                nameof(rectangle));
        }

        double x1 =
            ReadNumber(rectangle[0]);

        double y1 =
            ReadNumber(rectangle[1]);

        double x2 =
            ReadNumber(rectangle[2]);

        double y2 =
            ReadNumber(rectangle[3]);

        PdfPoint p0 =
            transform.Transform(
                new PdfPoint(x1, y1));

        PdfPoint p1 =
            transform.Transform(
                new PdfPoint(x2, y1));

        PdfPoint p2 =
            transform.Transform(
                new PdfPoint(x2, y2));

        PdfPoint p3 =
            transform.Transform(
                new PdfPoint(x1, y2));

        return new PdfPathGeometry(
            [
                new PdfPathSegment(
                    PdfPathSegmentKind.MoveTo,
                    p0),

                new PdfPathSegment(
                    PdfPathSegmentKind.LineTo,
                    p1),

                new PdfPathSegment(
                    PdfPathSegmentKind.LineTo,
                    p2),

                new PdfPathSegment(
                    PdfPathSegmentKind.LineTo,
                    p3),

                new PdfPathSegment(
                    PdfPathSegmentKind.ClosePath,
                    p0)
            ]);
    }

    private static double ReadNonNegativeNumber(
        PdfObject value,
        PdfGraphicsOperation operation,
        string description)
    {
        double number =
            ReadNumber(value);

        if (number < 0)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                $"{description} cannot be negative.");
        }

        return number;
    }

    private static double ReadMiterLimit(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        double number =
            ReadNumber(value);

        if (number < 1)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "The miter limit must be at least 1.");
        }

        return number;
    }

    private static double ReadFlatness(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        double number =
            ReadNumber(value);

        if (number is < 0 or > 100)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidGraphicsState,
                operation,
                "The flatness tolerance must be between 0 and 100.");
        }

        return number;
    }

    private static int ReadTextRenderingMode(
        PdfObject value,
        PdfGraphicsOperation operation)
    {
        long number =
            ReadInteger(value);

        if (number is < 0 or > 7)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.InvalidTextState,
                operation,
                $"Text rendering mode {number} is invalid.");
        }

        return checked((int)number);
    }

    private static int? TryGetInteger(
        PdfDictionary dictionary,
        string key)
    {
        if (!dictionary.TryGetValue(
                new PdfName(key),
                out PdfObject value)
            || !TryReadInteger(
                value,
                out long number)
            || number is < int.MinValue or > int.MaxValue)
        {
            return null;
        }

        return checked((int)number);
    }

    private static PdfArray? TryGetArray(
        PdfDictionary dictionary,
        string key)
    {
        return dictionary.TryGetValue(
                new PdfName(key),
                out PdfObject value)
            ? value as PdfArray
            : null;
    }

    private static double? TryGetNumber(
        PdfDictionary? dictionary,
        string key)
    {
        if (dictionary is null
            || !dictionary.TryGetValue(
                new PdfName(key),
                out PdfObject value)
            || !TryReadNumber(
                value,
                out double number))
        {
            return null;
        }

        return number;
    }

    private static bool TryReadInteger(
        PdfObject value,
        out long number)
    {
        if (value is PdfInteger integer)
        {
            number = integer.Value;
            return true;
        }

        number = 0;
        return false;
    }

    private static bool TryReadNumber(
        PdfObject value,
        out double number)
    {
        switch (value)
        {
            case PdfInteger integer:
                number = integer.Value;
                return true;

            case PdfReal real:
                number = real.Value;
                return true;

            default:
                number = 0;
                return false;
        }
    }

    private static void EnsureLimit(
        int value,
        int maximum,
        PdfGraphicsOperation operation,
        string message)
    {
        if (value > maximum)
        {
            throw CreateException(
                PdfGraphicsInterpretationFailure.LimitExceeded,
                operation,
                message);
        }
    }

    private static PdfGraphicsInterpretationException CreateException(
        PdfGraphicsInterpretationFailure failure,
        PdfGraphicsOperation operation,
        string message,
        Exception? innerException = null) =>
        new(failure, operation.SourceSpan, message, innerException);

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