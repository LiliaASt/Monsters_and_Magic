using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a chain-lightning arc alive: re-randomizes the jagged discharge
/// every few frames and spawns short branch forks off the main line, the way
/// real lightning flickers instead of hanging in the air as a static zigzag.
/// The attack only needs to call Initialize with both endpoints.
/// </summary>
public class LightningArcAnimator : MonoBehaviour
{
    private const int BranchCount = 1;

    private LineRenderer core;
    private LineRenderer glow;
    private LineRenderer glowOuter;
    private readonly LineRenderer[] branchCores = new LineRenderer[BranchCount];
    private readonly LineRenderer[] branchGlows = new LineRenderer[BranchCount];

    private Vector3 from;
    private Vector3 to;
    private int segments = 12;
    private float jitter = 0.18f;
    private float timer;

    public void Initialize(Vector3 start, Vector3 end, int segmentCount, float arcJitter)
    {
        from = start;
        to = end;
        segments = Mathf.Max(3, segmentCount);
        jitter = arcJitter;
        CacheLines();
        Regenerate();
    }

    private void Awake()
    {
        CacheLines();
    }

    private void CacheLines()
    {
        if (core != null)
            return;

        LineRenderer[] lines = GetComponentsInChildren<LineRenderer>(true);
        List<LineRenderer> branchCoreList = new List<LineRenderer>();
        List<LineRenderer> branchGlowList = new List<LineRenderer>();

        foreach (LineRenderer line in lines)
        {
            if (line.name == "Glow")
                glow = line;
            else if (line.name == "GlowOuter")
                glowOuter = line;
            else if (line.name.StartsWith("BranchCore"))
                branchCoreList.Add(line);
            else if (line.name.StartsWith("BranchGlow"))
                branchGlowList.Add(line);
            else
                core = line;
        }

        for (int i = 0; i < BranchCount; i++)
        {
            if (i < branchCoreList.Count)
                branchCores[i] = branchCoreList[i];
            if (i < branchGlowList.Count)
                branchGlows[i] = branchGlowList[i];
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        Regenerate();
        // Irregular flicker cadence: real discharges stutter instead of ticking.
        timer = 0.05f * Random.Range(0.6f, 1.4f);
    }

    private void Regenerate()
    {
        Vector3[] points = BuildArcPoints(from, to, segments, jitter);
        float distance = Vector3.Distance(from, to);
        float tiles = Mathf.Max(1f, distance * 0.9f);
        // Wider brightness swings make the arc read as superheated plasma
        // rather than a painted line.
        float intensity = Random.Range(0.6f, 1.1f);

        ApplyPoints(core, points, tiles, intensity);
        // The core thickness stutters but never grows past the base width -
        // a fat pale line kills the electric read.
        if (core != null)
            core.widthMultiplier = Random.Range(0.7f, 1f);
        ApplyPoints(glow, points, tiles, intensity);
        // The outer halo flickers independently of the tight glow, the way
        // an ionized sheath lags behind the discharge itself.
        ApplyPoints(glowOuter, points, tiles, intensity * Random.Range(0.7f, 1.05f));

        for (int i = 0; i < BranchCount; i++)
        {
            // Branches fork from the middle section of the main arc and reach
            // roughly a third of the remaining length, offset to either side.
            int anchor = Random.Range(segments / 3, Mathf.Max(segments / 3 + 1, (segments * 2) / 3));
            Vector3 origin = points[Mathf.Clamp(anchor, 0, points.Length - 1)];
            float side = (i % 2 == 0) ? 1f : -1f;
            float length = distance * Random.Range(0.12f, 0.24f);
            Vector3 direction = (to - from).normalized;
            Vector2 fork = (Vector2)(direction * Random.Range(0.3f, 0.7f)
                + (Vector3)(Perpendicular(direction) * side * Random.Range(0.6f, 1f))).normalized;

            Vector3[] branch = BuildArcPoints(
                origin,
                origin + (Vector3)(fork * length),
                Mathf.Max(3, segments / 3),
                jitter * 0.6f);

            float branchTiles = Mathf.Max(1f, length * 0.9f);
            float branchIntensity = intensity * Random.Range(0.45f, 0.75f);

            if (branchCores[i] != null)
                ApplyPoints(branchCores[i], branch, branchTiles, branchIntensity);
            if (branchGlows[i] != null)
                ApplyPoints(branchGlows[i], branch, branchTiles, branchIntensity);
        }
    }

    private static void ApplyPoints(LineRenderer line, Vector3[] points, float tiles, float intensity)
    {
        if (line == null)
            return;

        line.positionCount = points.Length;
        line.SetPositions(points);
        line.textureScale = new Vector2(tiles, 1f);
        line.startColor = new Color(1f, 1f, 1f, intensity);
        line.endColor = new Color(1f, 1f, 1f, intensity * 0.7f);
    }

    private static Vector3[] BuildArcPoints(Vector3 start, Vector3 end, int count, float amount)
    {
        Vector3 direction = end - start;
        Vector2 perpendicular = Perpendicular(direction.normalized);

        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            // Pin both ends, jitter in between with a sine envelope.
            float envelope = Mathf.Sin(t * Mathf.PI);
            float offset = (Random.value * 2f - 1f) * amount * envelope;

            point += (Vector3)(perpendicular * offset);
            point.z = -0.3f;
            points[i] = point;
        }

        return points;
    }

    private static Vector2 Perpendicular(Vector2 direction)
    {
        return new Vector2(-direction.y, direction.x);
    }
}
