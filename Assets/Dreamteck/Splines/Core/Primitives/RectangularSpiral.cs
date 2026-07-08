using UnityEngine;

namespace Dreamteck.Splines.Primitives
{
    /// <summary>
    /// Rectangular spiral with rounded corners.
    /// Positions grow per-edge (only in that edge's direction).
    /// Tangent logic is identical to RoundedRectangle — SetTangentPosition /
    /// SetTangent2Position called with absolute world positions.
    /// </summary>
    public class RectangularSpiral : SplinePrimitive
    {
        [Header("Shape")]
        public float startWidth = 1f;
        public float startHeight = 1f;
        public float endWidth = 4f;
        public float endHeight = 4f;

        [Header("Spiral")]
        public float spacing = 0.5f;
        public bool clockwise = true;

        [Header("Corners")]
        public float cornerRadius = 0.2f;
        public int cornerSegments = 4;

        // Each side grows one dimension by spacing.
        // Total sides = max delta / spacing, minimum 4.
        public int TotalSides
        {
            get
            {
                float deltaW = Mathf.Abs(endWidth - startWidth);
                float deltaH = Mathf.Abs(endHeight - startHeight);
                float maxDelta = Mathf.Max(deltaW, deltaH);
                if (spacing <= 0f) return 4;
                return Mathf.Max(4, Mathf.CeilToInt(maxDelta / spacing));
            }
        }

        public float TurnsApprox => TotalSides / 4f;

        public override Spline.Type GetSplineType() => Spline.Type.Bezier;

        protected override void Generate()
        {
            base.Generate();
            closed = false;

            int totalSides = TotalSides;
            int totalPoints = totalSides * 2 + 1;   // 2 pts per corner + 1 closing pt
            CreatePoints(totalPoints, SplinePoint.Type.Broken);

            // Same constant as RoundedRectangle
            float tK = 2f * (Mathf.Sqrt(2f) - 1f) / 3f;

            // Running full width/height (not half-extents — matches RoundedRectangle's `size`)
            float w = startWidth;
            float h = startHeight;

            int ptIdx = 0;

            for (int s = 0; s < totalSides; s++)
            {
                int sideInTurn = s % 4;
                bool isVertical = (sideInTurn == 0 || sideInTurn == 2);

                // Entry size = current w/h before this edge grows
                // Exit  size = after growing only in this edge's axis
                float wEntry = w;
                float hEntry = h;
                float wExit = w + (isVertical ? 0f : spacing);
                float hExit = h + (isVertical ? spacing : 0f);

                WriteCornerPair(ptIdx, sideInTurn, tK, wEntry, hEntry, wExit, hExit);
                ptIdx += 2;

                w = wExit;
                h = hExit;
            }

            // Closing point — same style as RoundedRectangle's points[8] = points[0]
            // Re-enter side 0 at the final grown size
            {
                float cr = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(w, h) * 0.5f);
                float yRad = tK * cr * 2f;
                // Side 0 entry point (CW: top of left edge / CCW: top of right edge)
                Vector3 pos = clockwise
                    ? Vector3.forward / 2f * (h - cr * 2f) + Vector3.left / 2f * w
                    : Vector3.forward / 2f * (h - cr * 2f) + Vector3.right / 2f * w;

                points[ptIdx].SetPosition(pos);
                // Tangent2 points forward (+Z), tangent points backward (-Z) — same as pt[0] in RoundedRectangle
                points[ptIdx].SetTangent2Position(pos + Vector3.forward * yRad);
                points[ptIdx].SetTangentPosition(pos - Vector3.forward * yRad);
            }
        }

        /// <summary>
        /// Writes 2 spline points for one corner pair (entry anchor + exit anchor).
        ///
        /// Positions are computed from wEntry/hEntry (before growth) and wExit/hExit (after).
        /// Tangents use SetTangentPosition / SetTangent2Position with absolute positions,
        /// exactly matching the RoundedRectangle pattern.
        ///
        /// CW side order:  0=left (+Z), 1=top (+X), 2=right (-Z), 3=bottom (-X)
        /// CCW:            0=right(+Z), 1=top (-X), 2=left (-Z),  3=bottom (+X)
        /// </summary>
        private void WriteCornerPair(int idx, int sideInTurn, float tK,
                                     float wEntry, float hEntry,
                                     float wExit, float hExit)
        {
            // Corner radius clamped to local size at entry and exit
            float crEn = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(wEntry, hEntry) * 0.5f);
            float crEx = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(wExit, hExit) * 0.5f);

            // Bezier handle lengths — same formula as RoundedRectangle's xRad/yRad
            float xRadEn = tK * crEn * 2f;
            float yRadEn = tK * crEn * 2f;
            float xRadEx = tK * crEx * 2f;
            float yRadEx = tK * crEx * 2f;

            // Edge sizes (straight sections) — full size minus the two corner radii
            float exEn = wEntry - crEn * 2f;   // edgeSize.x at entry
            float eyEn = hEntry - crEn * 2f;   // edgeSize.y at entry
            float exEx = wExit - crEx * 2f;   // edgeSize.x at exit
            float eyEx = hExit - crEx * 2f;   // edgeSize.y at exit

            // p0 = entry anchor (end of the incoming straight edge, start of the curve)
            // p1 = exit  anchor (end of the curve, start of the outgoing straight edge)
            Vector3 p0, p1;

            if (clockwise)
            {
                // ── RoundedRectangle point numbering (CW) ──────────────────────────
                // pt0 → pt1 : left  edge  → top-left  corner → top   edge
                // pt2 → pt3 : top   edge  → top-right corner → right edge
                // pt4 → pt5 : right edge  → bot-right corner → bot   edge
                // pt6 → pt7 : bot   edge  → bot-left  corner → left  edge
                // ──────────────────────────────────────────────────────────────────
                switch (sideInTurn)
                {
                    default:
                    case 0: // LEFT edge → TOP-LEFT corner
                        // p0 mirrors RoundedRectangle pt[0]: forward/2*edgeY + left/2*sizeX
                        // but edgeY uses hEntry (before growth), sizeX uses wEntry (unchanged)
                        p0 = Vector3.forward / 2f * eyEn + Vector3.left / 2f * wEntry;
                        // p1 mirrors pt[1]: forward/2*sizeY + left/2*edgeX
                        // sizeY uses hExit (grown), edgeX uses wExit (unchanged = wEntry)
                        p1 = Vector3.forward / 2f * hExit + Vector3.left / 2f * exEx;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.forward * yRadEn);  // pt[0] tangent2
                        points[idx].SetTangentPosition(p0 - Vector3.forward * yRadEn);  // pt[0] tangent (mirror)

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.left * xRadEx);    // pt[1] tangent
                        points[idx + 1].SetTangent2Position(p1 - Vector3.left * xRadEx);    // pt[1] tangent2 (mirror)
                        break;

                    case 1: // TOP edge → TOP-RIGHT corner
                        // p0 mirrors pt[2]: forward/2*sizeY + right/2*edgeX
                        p0 = Vector3.forward / 2f * hEntry + Vector3.right / 2f * exEn;
                        // p1 mirrors pt[3]: forward/2*edgeY + right/2*sizeX
                        p1 = Vector3.forward / 2f * eyEx + Vector3.right / 2f * wExit;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.right * xRadEn);    // pt[2] tangent2
                        points[idx].SetTangentPosition(p0 - Vector3.right * xRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.forward * yRadEx);  // pt[3] tangent
                        points[idx + 1].SetTangent2Position(p1 - Vector3.forward * yRadEx);
                        break;

                    case 2: // RIGHT edge → BOT-RIGHT corner
                        // p0 mirrors pt[4]: back/2*edgeY + right/2*sizeX
                        p0 = Vector3.back / 2f * eyEn + Vector3.right / 2f * wEntry;
                        // p1 mirrors pt[5]: back/2*sizeY + right/2*edgeX
                        p1 = Vector3.back / 2f * hExit + Vector3.right / 2f * exEx;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.back * yRadEn);    // pt[4] tangent2
                        points[idx].SetTangentPosition(p0 - Vector3.back * yRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.right * xRadEx);    // pt[5] tangent
                        points[idx + 1].SetTangent2Position(p1 - Vector3.right * xRadEx);
                        break;

                    case 3: // BOTTOM edge → BOT-LEFT corner
                        // p0 mirrors pt[6]: back/2*sizeY + left/2*edgeX
                        p0 = Vector3.back / 2f * hEntry + Vector3.left / 2f * exEn;
                        // p1 mirrors pt[7]: back/2*edgeY + left/2*sizeX
                        p1 = Vector3.back / 2f * eyEx + Vector3.left / 2f * wExit;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.left * xRadEn);     // pt[6] tangent2
                        points[idx].SetTangentPosition(p0 - Vector3.left * xRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.back * yRadEx);    // pt[7] tangent
                        points[idx + 1].SetTangent2Position(p1 - Vector3.back * yRadEx);
                        break;
                }
            }
            else // counter-clockwise: mirror X (left ↔ right)
            {
                switch (sideInTurn)
                {
                    default:
                    case 0: // RIGHT edge → TOP-RIGHT corner
                        p0 = Vector3.forward / 2f * eyEn + Vector3.right / 2f * wEntry;
                        p1 = Vector3.forward / 2f * hExit + Vector3.right / 2f * exEx;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.forward * yRadEn);
                        points[idx].SetTangentPosition(p0 - Vector3.forward * yRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.right * xRadEx);
                        points[idx + 1].SetTangent2Position(p1 - Vector3.right * xRadEx);
                        break;

                    case 1: // TOP edge → TOP-LEFT corner
                        p0 = Vector3.forward / 2f * hEntry + Vector3.left / 2f * exEn;
                        p1 = Vector3.forward / 2f * eyEx + Vector3.left / 2f * wExit;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.left * xRadEn);
                        points[idx].SetTangentPosition(p0 - Vector3.left * xRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.forward * yRadEx);
                        points[idx + 1].SetTangent2Position(p1 - Vector3.forward * yRadEx);
                        break;

                    case 2: // LEFT edge → BOT-LEFT corner
                        p0 = Vector3.back / 2f * eyEn + Vector3.left / 2f * wEntry;
                        p1 = Vector3.back / 2f * hExit + Vector3.left / 2f * exEx;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.back * yRadEn);
                        points[idx].SetTangentPosition(p0 - Vector3.back * yRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.left * xRadEx);
                        points[idx + 1].SetTangent2Position(p1 - Vector3.left * xRadEx);
                        break;

                    case 3: // BOTTOM edge → BOT-RIGHT corner
                        p0 = Vector3.back / 2f * hEntry + Vector3.right / 2f * exEn;
                        p1 = Vector3.back / 2f * eyEx + Vector3.right / 2f * wExit;

                        points[idx].SetPosition(p0);
                        points[idx].SetTangent2Position(p0 + Vector3.right * xRadEn);
                        points[idx].SetTangentPosition(p0 - Vector3.right * xRadEn);

                        points[idx + 1].SetPosition(p1);
                        points[idx + 1].SetTangentPosition(p1 + Vector3.back * yRadEx);
                        points[idx + 1].SetTangent2Position(p1 - Vector3.back * yRadEx);
                        break;
                }
            }
        }
    }
}