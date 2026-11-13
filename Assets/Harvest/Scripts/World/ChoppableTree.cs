using System;
using UnityEngine;

public struct TreeGenConfig
{
    public float Radius;
    public float Height;
    public float ColDensity;
    public float RowDensity;
    public float MinRadius;
    public float RadiusNoiseScale;
    public float RadiusNoiseStrength;
    public int RadiusNoiseSeed;
    public float HorzNoiseScale;
    public float HorzNoiseStrength;
    public float VertNoiseScale;
    public float VertNoiseStrength;
}

public sealed class TreeGridSettings
{
    public readonly float ColDensity;
    public readonly float RowDensity;
    public readonly float MinRadius;
    public readonly float HeightPerRow;

    public TreeGridSettings(float colDensity, float rowDensity, float minRadius, float heightPerRow)
    {
        ColDensity = colDensity;
        RowDensity = rowDensity;
        MinRadius = minRadius;
        HeightPerRow = heightPerRow;
    }
}

public struct GridHitSummary
{
    public float PeakDepth;
    public float PeakDamage;
    public float LitresRemoved;
    public int? SplitRow;
}

public sealed class TreeGrid
{
    public readonly TreeGridSettings Settings;
    public readonly int Cols;
    public readonly int Rows;
    public readonly float[,] Radius;
    public readonly float[,] BaseRadius;
    public readonly float[,] Damage;
    public readonly Vector3[,] Offsets;

    public float Height => Rows * Settings.HeightPerRow;

    public static TreeGrid Generate(TreeGenConfig config)
    {
        int cols = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * config.Radius * config.ColDensity));
        int rows = Mathf.Max(4, Mathf.CeilToInt(config.Height * config.RowDensity));
        float heightPerRow = config.Height / rows;

        var constants = new TreeGridSettings(
            config.ColDensity,
            config.RowDensity,
            config.MinRadius,
            heightPerRow
        );

        var grid = new TreeGrid(cols, rows, constants);

        grid.GenerateBaseRadius(
            config.Radius,
            config.RadiusNoiseScale,
            config.RadiusNoiseStrength,
            config.RadiusNoiseSeed
        );

        grid.GeneratePerVertexOffsets(
            config.HorzNoiseScale,
            config.HorzNoiseStrength,
            config.VertNoiseScale,
            config.VertNoiseStrength
        );

        return grid;
    }

    public TreeGrid(int cols, int rows, TreeGridSettings constants)
    {
        Settings = constants;
        Cols = cols;
        Rows = rows;
        Radius = new float[cols, rows];
        BaseRadius = new float[cols, rows];
        Damage = new float[cols, rows];
        Offsets = new Vector3[cols, rows];
    }

    public float HardnessAt(int c, int r, float baseHardness)
    {
        return TreeHitMath.Hardness(baseHardness, BaseRadius[c, r], Radius[c, r], Settings.MinRadius);
    }

    private void GenerateBaseRadius(float baseRadius, float noiseScale, float noiseStrength, int seed)
    {
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                float angle = (c / (float)Cols) * Mathf.PI * 2f;
                float nx = Mathf.Cos(angle);
                float nz = Mathf.Sin(angle) + (r / (float)(Rows - 1));
                float radiusNoise = Mathf.PerlinNoise(nx * noiseScale + seed, nz * noiseScale + seed);
                radiusNoise = (radiusNoise - 0.5f) * noiseStrength;

                float radius = baseRadius + radiusNoise;
                BaseRadius[c, r] = radius;
                Radius[c, r] = radius;
            }
        }
    }

    private void GeneratePerVertexOffsets(float horzScale, float horzStrength, float vertScale, float vertStrength)
    {
        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < Rows; r++)
            {
                float angle = (c / (float)Cols) * Mathf.PI * 2f;
                float nx = Mathf.Cos(angle);
                float nz = Mathf.Sin(angle) + (r / (float)(Rows - 1));
                float p = Mathf.PerlinNoise(nx * horzScale + 500f, nz * horzScale + 600f);
                float v = Mathf.PerlinNoise(nx * vertScale + 700f, nz * vertScale + 800f);
                float perpOffset = (p - 0.5f) * horzStrength;
                float vertOffset = (v - 0.5f) * vertStrength;

                Offsets[c, r] = new(perpOffset, vertOffset, 0f);
            }
        }
    }

    public GridHitSummary ApplyHitToGrid(Vector2Int hitGrid, float halfWidth, float halfHeight, float effectiveStrength, float depthMultiplier, float woodDamage, float baseHardness)
    {
        int maxHitCols = Mathf.Max(1, Mathf.FloorToInt((Cols * MAX_HIT_ARC_FRACTION - 1f) / 2f));
        int hitCols = Mathf.Max(1, Mathf.Min(Mathf.CeilToInt(halfWidth * Settings.ColDensity), maxHitCols));
        int hitRows = Mathf.Max(1, Mathf.CeilToInt(halfHeight * Settings.RowDensity));
        float cellAngle = Mathf.PI * 2f / Cols;

        var summary = new GridHitSummary();
        float cubicMetresRemoved = 0f;

        for (int dc = -hitCols; dc <= hitCols; dc++)
        {
            int c = (hitGrid.x + dc + Cols) % Cols;

            for (int dr = -hitRows; dr <= hitRows; dr++)
            {
                int r = hitGrid.y + dr;
                if (r < 0 || r >= Rows) continue;

                // Normalised one cell past the edge so the outermost cells still take a dent
                float colDist = Mathf.Abs(dc) / (hitCols + 1f);
                float rowDist = Mathf.Abs(dr) / (hitRows + 1f);
                float dist = Mathf.Sqrt(colDist * colDist + rowDist * rowDist);
                if (dist > 1f) continue;

                // Each cell dents by its own hardness, scaled by the footprint falloff
                float falloff = 1f - dist;
                float hardness = HardnessAt(c, r, baseHardness);
                float depth = TreeHitMath.DentDepth(effectiveStrength, hardness) * depthMultiplier * falloff;

                float oldRadius = Radius[c, r];
                float newRadius = Mathf.Max(Settings.MinRadius, oldRadius - depth);
                Radius[c, r] = newRadius;

                float cellDamage = woodDamage * falloff;
                Damage[c, r] += cellDamage;

                cubicMetresRemoved += 0.5f * (oldRadius * oldRadius - newRadius * newRadius) * cellAngle * Settings.HeightPerRow;
                summary.PeakDepth = Mathf.Max(summary.PeakDepth, oldRadius - newRadius);
                summary.PeakDamage = Mathf.Max(summary.PeakDamage, cellDamage);
            }
        }

        summary.LitresRemoved = cubicMetresRemoved * 1000f;
        summary.SplitRow = CheckSplit(hitGrid.y - hitRows, hitGrid.y + hitRows, hitGrid.y);
        return summary;
    }

    public int? CheckSplit(int lowRow, int highRow, int preferredRow)
    {
        lowRow = Mathf.Clamp(lowRow, 0, Rows - 1);
        highRow = Mathf.Clamp(highRow, 0, Rows - 1);

        // Search outwards from the preferred row so the cut lands closest to the hit
        int maxOffset = Mathf.Max(preferredRow - lowRow, highRow - preferredRow);
        for (int offset = 0; offset <= maxOffset; offset++)
        {
            int below = preferredRow - offset;
            if (below >= lowRow && below <= highRow && RowInChoppableRange(below) && CheckRowSplitRequirement(below)) return below;

            int above = preferredRow + offset;
            if (offset > 0 && above >= lowRow && above <= highRow && RowInChoppableRange(above) && CheckRowSplitRequirement(above)) return above;
        }

        return null;
    }

    private bool CheckRowSplitRequirement(int r)
    {
        // A row gives way once about a third of the trunk's angle is cut most of the way through
        int windowCols = Mathf.Max(1, Mathf.CeilToInt(Cols * CUT_THROUGH_ARC_FRACTION));

        float windowDepth = 0f;
        for (int i = 0; i < windowCols; i++) windowDepth += CutDepthFraction(i, r);

        for (int start = 0; start < Cols; start++)
        {
            if (windowDepth / windowCols >= CUT_THROUGH_DEPTH_FRACTION) return true;
            windowDepth += CutDepthFraction(start + windowCols, r) - CutDepthFraction(start, r);
        }

        return false;
    }

    private float CutDepthFraction(int c, int r)
    {
        c %= Cols;
        float span = Mathf.Max(0.0001f, BaseRadius[c, r] - Settings.MinRadius);
        return Mathf.Clamp01((BaseRadius[c, r] - Radius[c, r]) / span);
    }

    public (TreeGrid bottom, TreeGrid top) SplitAtRow(int splitRow)
    {
        int bottomRows = splitRow;
        int topRows = Rows - bottomRows;
        if (topRows <= 0) return (this, null);

        var bottom = Slice(0, bottomRows);
        var top = Slice(bottomRows + 1, topRows - 1);

        bottom.SmoothCutEnd(bottom.Rows - 1, -1);
        top.SmoothCutEnd(0, 1);
        return (bottom, top);
    }

    private void SmoothCutEnd(int endRow, int towardsInside)
    {
        var source = (float[,])Radius.Clone();

        for (int i = 0; i < CUT_SMOOTH_ROWS; i++)
        {
            int r = endRow + towardsInside * i;
            if (r < 0 || r >= Rows) break;

            float weight = CUT_SMOOTH_STRENGTH * (1f - i / (float)CUT_SMOOTH_ROWS);
            for (int c = 0; c < Cols; c++)
            {
                float neighbourAverage = (source[(c - 1 + Cols) % Cols, r] + source[c, r] + source[(c + 1) % Cols, r]) / 3f;
                Radius[c, r] = Mathf.Lerp(source[c, r], neighbourAverage, weight);
            }
        }
    }

    private TreeGrid Slice(int startRow, int rows)
    {
        var grid = new TreeGrid(Cols, rows, Settings);

        for (int c = 0; c < Cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                int sourceRow = startRow + r;
                grid.Radius[c, r] = Radius[c, sourceRow];
                grid.BaseRadius[c, r] = BaseRadius[c, sourceRow];
                grid.Damage[c, r] = Damage[c, sourceRow];
                grid.Offsets[c, r] = Offsets[c, sourceRow];
            }
        }

        return grid;
    }

    public float SampleRadius(Vector3 localPos, bool useBaseRadius = false)
    {
        // Bilinear between the four nearest cells, wrapping round the trunk
        float angle = Mathf.Atan2(localPos.z, localPos.x);
        if (angle < 0f) angle += Mathf.PI * 2f;

        float colF = angle / (Mathf.PI * 2f) * Cols;
        int col0 = Mathf.FloorToInt(colF) % Cols;
        int col1 = (col0 + 1) % Cols;
        float colT = colF - Mathf.Floor(colF);

        float rowF = Mathf.Clamp(localPos.y / Height * (Rows - 1), 0f, Rows - 1);
        int row0 = Mathf.Min(Mathf.FloorToInt(rowF), Rows - 1);
        int row1 = Mathf.Min(row0 + 1, Rows - 1);
        float rowT = rowF - row0;

        float[,] radii = useBaseRadius ? BaseRadius : Radius;
        float low = Mathf.Lerp(radii[col0, row0], radii[col1, row0], colT);
        float high = Mathf.Lerp(radii[col0, row1], radii[col1, row1], colT);
        return Mathf.Lerp(low, high, rowT);
    }

    public Vector3 GridToLocal(int c, int r)
    {
        float angle = (c / (float)Cols) * Mathf.PI * 2f;
        float radius = Radius[c, r];
        float y = (r / (float)(Rows - 1)) * Height;

        Vector3 p = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);

        var off = Offsets[c, r];
        Vector3 n = new Vector3(p.x, 0f, p.z).normalized;
        Vector3 tangent = new(-n.z, 0f, n.x);

        p += tangent * off.x;
        p += Vector3.up * off.y;

        return p;
    }

    public Vector2Int? LocalToGrid(Vector3 localPos)
    {
        float angle = Mathf.Atan2(localPos.z, localPos.x);
        if (angle < 0) angle += Mathf.PI * 2f;

        int col = Mathf.FloorToInt(angle / (Mathf.PI * 2f) * Cols);
        int row = Mathf.RoundToInt(localPos.y / Height * (Rows - 1));

        if (row < 0 || row > Rows - 1) return null;
        return new Vector2Int(col, row);
    }

    private bool RowInChoppableRange(int r)
    {
        int minSplitRow = 2;
        int maxSplitRow = Rows - 3;
        return r >= minSplitRow && r <= maxSplitRow;
    }

    private static readonly float MAX_HIT_ARC_FRACTION = 1f / 3f;
    private static readonly float CUT_THROUGH_ARC_FRACTION = 1f / 3f;
    private static readonly float CUT_THROUGH_DEPTH_FRACTION = 0.8f;
    private static readonly int CUT_SMOOTH_ROWS = 3;
    private static readonly float CUT_SMOOTH_STRENGTH = 0.4f;
}

public class ChoppableTree : MonoBehaviour
{
    public event Action<TreeHitResult> OnHit = delegate { };
    public event Action<ChoppableTree> OnFelled = delegate { };
    public event Action<float> OnLanded = delegate { };

    public bool CanChop { get; private set; } = true;
    public WoodType WoodType => woodType;

    public bool TrySampleSurface(Vector3 posWorld, out Vector3 point, out Vector3 normal, bool useBaseRadius = false)
    {
        // Read the surface straight from the grid, either as cut so far or as the original untouched trunk
        Vector3 local = transform.InverseTransformPoint(posWorld);
        Vector3 radial = new(local.x, 0f, local.z);
        point = posWorld;
        normal = transform.TransformDirection(radial.normalized);
        if (radial.sqrMagnitude < 0.0001f) return false;

        float surfaceRadius = grid.SampleRadius(local, useBaseRadius);
        float angle = Mathf.Atan2(local.z, local.x);
        point = transform.TransformPoint(new Vector3(Mathf.Cos(angle) * surfaceRadius, local.y, Mathf.Sin(angle) * surfaceRadius));
        return true;
    }

    public TreeHitResult Hit(TreeHitInput input)
    {
        Vector2Int? posGrid = WorldToGrid(input.Position);
        if (posGrid == null) return new TreeHitResult { Outcome = TreeHitOutcome.Miss, Position = input.Position };

        Vector3 hitTrunkPos = new(transform.position.x, input.Position.y, transform.position.z);
        Vector3 normal = (input.Position - hitTrunkPos).normalized;

        // Accuracy scales both tool stats before anything else reads them
        float swingQuality = TreeHitMath.SwingQuality(input.Accuracy);
        float effectiveSharpness = input.Sharpness * swingQuality;
        float effectiveStrength = input.Strength * swingQuality;

        // Sharpness against the struck cell's hardness sets how well the edge bites
        float baseHardness = woodType.BaseHardness;
        float hardness = grid.HardnessAt(posGrid.Value.x, posGrid.Value.y, baseHardness);
        float biteQuality = TreeHitMath.BiteQuality(effectiveSharpness, hardness);

        var result = new TreeHitResult
        {
            Outcome = TreeHitMath.IsBlunt(biteQuality) ? TreeHitOutcome.Blunt : TreeHitOutcome.Clean,
            Position = input.Position,
            Normal = normal,
            Cell = posGrid.Value,
            BiteQuality = biteQuality
        };

        // Every hit lands: a better bite is narrower and deeper, a blunt one wider and shallower
        float woodDamage = TreeHitMath.WoodDamage(effectiveStrength, biteQuality);
        float halfWidth = TreeHitMath.HalfWidth(input.BitWidth, biteQuality);
        float halfHeight = TreeHitMath.HalfHeightFromHalfWidth(halfWidth);
        var summary = grid.ApplyHitToGrid(posGrid.Value, halfWidth, halfHeight, effectiveStrength, TreeHitMath.DepthMultiplier(biteQuality), woodDamage, baseHardness);

        result.Depth = summary.PeakDepth;
        result.LitresRemoved = summary.LitresRemoved;
        result.Damage = summary.PeakDamage;

        // Only wood that actually came off throws chips
        if (summary.LitresRemoved > MIN_CHIP_LITRES)
        {
            var particles = ParticleManager.Instance.Spawn(ParticleEffect.TreeChop, input.Position, Quaternion.LookRotation(normal));
            if (particles != null) particles.Play();
        }

        // Just rebuild mesh with the updated grid
        if (summary.SplitRow == null)
        {
            RebuildMesh();
            OnHit?.Invoke(result);
            return result;
        }

        // Otherwise we need to split the grid, rebuild bottom half, then spawn top half
        var (bottomGrid, topGrid) = grid.SplitAtRow(summary.SplitRow.Value);

        grid = bottomGrid;
        RebuildMesh();

        if (topGrid != null)
        {
            // Spawn and loosen top half as a log
            Vector3 spawnPos = transform.position + Vector3.up * (grid.Rows - 1) * (grid.Height / grid.Rows) + Vector3.up * 0.01f;
            var go = Instantiate(gameObject, spawnPos, transform.rotation);
            var tree = go.GetComponent<ChoppableTree>();

            tree.grid = topGrid;
            tree.CanChop = false;
            tree.RebuildMesh();
            tree.SetDraggable();

            result.CausedSplit = true;
            result.Log = tree;
        }

        OnHit?.Invoke(result);
        if (result.Log != null) OnFelled?.Invoke(result.Log);
        return result;
    }

    [Header("References")]
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshCollider mc;
    [SerializeField] private Rigidbody rb;

    [Header("Wood")]
    [SerializeField] private WoodType woodType;
    [SerializeField] private int colourSeed;

    [Header("Debug")]
    [SerializeField] private bool debugShowDamage;

    [Header("Generation Config")]
    [SerializeField] private float genColDensity = 20f;
    [SerializeField] private float genRowDensity = 20f;
    [SerializeField] private float genRadiusNoiseScale = 1f;
    [SerializeField] private float genRadiusNoiseStrength = 0.25f;
    [SerializeField] private int genRadiusNoiseSeed = 0;
    [SerializeField] private float genRadius = 0.35f;
    [SerializeField] private float genMinRadius = 0.05f;
    [SerializeField] private float genHeight = 4f;
    [SerializeField] private float genHorzNoiseScale = 10.0f;
    [SerializeField] private float genVertNoiseScale = 10.0f;
    [SerializeField] private float genHorzNoiseStrength = 0.04f;
    [SerializeField] private float genVertNoiseStrength = 0.04f;

    private TreeGrid grid;
    private Mesh mesh;
    private MaterialPropertyBlock colourBlock;

    private void Awake()
    {
        Debug.Assert(woodType != null, $"ChoppableTree '{name}' has no WoodType assigned.", this);
        ApplyColourBlock();
        RegenerateTree();
        rb.isKinematic = true;
        CanChop = true;
    }

    private void OnValidate()
    {
        ApplyColourBlock();
    }

    [ContextMenu("Roll Wood Colours")]
    private void RollWoodColours()
    {
        colourSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        ApplyColourBlock();
    }

    private static Color ShiftColour(Color colour, float hueShift, float saturationShift, float valueShift)
    {
        Color.RGBToHSV(colour, out float hue, out float saturation, out float value);
        return Color.HSVToRGB(
            Mathf.Repeat(hue + hueShift, 1f),
            Mathf.Clamp01(saturation * (1f + saturationShift)),
            Mathf.Clamp01(value * (1f + valueShift)));
    }

    private void ApplyColourBlock()
    {
        if (woodType == null || mf == null || !mf.TryGetComponent(out MeshRenderer meshRenderer)) return;

        // One shared shift per tree keeps its three tones coherent
        var rng = new System.Random(colourSeed);
        float valueShift = ((float)rng.NextDouble() * 2f - 1f) * woodType.ColourVariance;
        float saturationShift = ((float)rng.NextDouble() * 2f - 1f) * woodType.ColourVariance * 0.5f;
        float hueShift = ((float)rng.NextDouble() * 2f - 1f) * woodType.ColourVariance * 0.1f;

        colourBlock ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(colourBlock);
        colourBlock.SetColor(BARK_COLOR_ID, ShiftColour(woodType.BarkColor, hueShift, saturationShift, valueShift));
        colourBlock.SetColor(INNER_COLOR_ID, ShiftColour(woodType.InnerColor, hueShift, saturationShift, valueShift));
        colourBlock.SetColor(CORE_COLOR_ID, ShiftColour(woodType.CoreColor, hueShift, saturationShift, valueShift));
        colourBlock.SetFloat(DEBUG_DAMAGE_ID, debugShowDamage ? 1f : 0f);
        meshRenderer.SetPropertyBlock(colourBlock);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb.isKinematic) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < LANDING_MIN_SPEED) return;

        OnLanded?.Invoke(impactSpeed);
    }

    [ContextMenu("Regenerate Tree")]
    private void RegenerateTree()
    {
        grid = TreeGrid.Generate(new()
        {
            Radius = genRadius,
            Height = genHeight,
            ColDensity = genColDensity,
            RowDensity = genRowDensity,
            MinRadius = genMinRadius,
            RadiusNoiseScale = genRadiusNoiseScale,
            RadiusNoiseStrength = genRadiusNoiseStrength,
            RadiusNoiseSeed = genRadiusNoiseSeed,
            HorzNoiseScale = genHorzNoiseScale,
            HorzNoiseStrength = genHorzNoiseStrength,
            VertNoiseScale = genVertNoiseScale,
            VertNoiseStrength = genVertNoiseStrength
        });

        RebuildMesh();
    }

    private void RebuildMesh()
    {
        if (mesh == null) mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        else mesh.Clear();

        int trunkVerts = grid.Cols * grid.Rows;
        int trunkTris = grid.Cols * (grid.Rows - 1) * 6;
        int capVerts = grid.Cols + 1;
        int capTris = grid.Cols * 3;
        int totalVerts = trunkVerts + capVerts * 2;
        int totalTris = trunkTris + capTris * 2;

        Vector3[] verts = new Vector3[totalVerts];
        Vector3[] norms = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        int[] tris = new int[totalTris];

        int v = 0;
        int t = 0;

        (Vector3 point, Vector3 normal) SamplePoint(int c, int r)
        {
            Vector3 p = grid.GridToLocal(c, r);
            Vector3 n = new Vector3(p.x, 0f, p.z).normalized;
            Vector3 tangent = new(-n.z, 0f, n.x);

            p += tangent * grid.Offsets[c, r].x;
            p += Vector3.up * grid.Offsets[c, r].y;

            return (p, n);
        }

        Vector2 UV(int c, int r) => new(1.0f - grid.Radius[c, r] / grid.BaseRadius[c, r], Mathf.Clamp01(grid.Damage[c, r] / DEBUG_DAMAGE_MAX));

        // ---------------------------- Trunk ----------------------------

        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                var (p, n) = SamplePoint(c, r);
                verts[v] = p;
                norms[v] = n;
                uvs[v] = UV(c, r);
                v++;
            }
        }

        for (int r = 0; r < grid.Rows - 1; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                int cn = (c + 1) % grid.Cols;

                int A = c + r * grid.Cols;
                int B = cn + r * grid.Cols;
                int C = c + (r + 1) * grid.Cols;
                int D = cn + (r + 1) * grid.Cols;

                tris[t++] = A;
                tris[t++] = C;
                tris[t++] = B;

                tris[t++] = B;
                tris[t++] = C;
                tris[t++] = D;
            }
        }

        // ---------------------------- Bottom Cap ----------------------------

        int bottomCenter = v;
        verts[v] = new Vector3(0f, 0f, 0f);
        norms[v] = Vector3.down;
        uvs[v] = new Vector2(0.5f, 0f);
        v++;

        for (int c = 0; c < grid.Cols; c++)
        {
            var (p, _) = SamplePoint(c, 0);
            verts[v] = p;
            norms[v] = Vector3.down;
            uvs[v] = UV(c, 0);
            v++;
        }

        for (int c = 0; c < grid.Cols; c++)
        {
            int A = bottomCenter;
            int B = bottomCenter + 1 + c;
            int C = bottomCenter + 1 + ((c + 1) % grid.Cols);

            tris[t++] = A;
            tris[t++] = B;
            tris[t++] = C;
        }

        // ---------------------------- Top Cap ----------------------------

        int topCenter = v;
        verts[v] = new Vector3(0f, grid.Height, 0f);
        norms[v] = Vector3.up;
        uvs[v] = new Vector2(0.5f, 0f);
        v++;

        for (int c = 0; c < grid.Cols; c++)
        {
            var (p, _) = SamplePoint(c, grid.Rows - 1);
            verts[v] = p;
            norms[v] = Vector3.up;
            uvs[v] = UV(c, grid.Rows - 1);
            v++;
        }

        for (int c = 0; c < grid.Cols; c++)
        {
            int A = topCenter;
            int B = topCenter + 1 + ((c + 1) % grid.Cols);
            int C = topCenter + 1 + c;

            tris[t++] = A;
            tris[t++] = B;
            tris[t++] = C;
        }

        // ---------------------------- Build ----------------------------

        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;

        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
    }

    private Vector2Int? WorldToGrid(Vector3 posWorld)
    {
        Vector3 local = transform.InverseTransformPoint(posWorld);
        return grid.LocalToGrid(local);
    }

    private void SetDraggable()
    {
        rb.isKinematic = false;
        var outline = gameObject.AddComponent<Outline>();
        var draggable = gameObject.AddComponent<DraggableObject>();
        draggable.Init(outline, rb);
    }

    private static readonly float LANDING_MIN_SPEED = 1f;
    private static readonly int BARK_COLOR_ID = Shader.PropertyToID("_BarkColor");
    private static readonly int INNER_COLOR_ID = Shader.PropertyToID("_InnerColor");
    private static readonly int CORE_COLOR_ID = Shader.PropertyToID("_CoreColor");
    private static readonly int DEBUG_DAMAGE_ID = Shader.PropertyToID("_DebugDamage");
    private static readonly float DEBUG_DAMAGE_MAX = 8f;
    private static readonly float MIN_CHIP_LITRES = 0.01f;
}
