using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws the sight around the same viewport center used by WeaponSystem.</summary>
public sealed class GunSight : MaskableGraphic
{
    private GunSightStyle style;
    private float opacity;
    public void Show(GunSightStyle next, float blend)
    {
        if (style == next && Mathf.Approximately(opacity, blend)) return;
        style = next;
        opacity = blend;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (opacity <= 0f) return;
        var rect = rectTransform.rect;
        var center = rect.center;
        float unit = Mathf.Min(rect.width, rect.height);
        float radius = unit * (style == GunSightStyle.Scope ? .37f : .024f);
        Color ink = new Color(.045f, .038f, .032f, opacity * .94f);
        float line = Mathf.Max(1.4f, unit * .0022f);
        if (style != GunSightStyle.Iron)
        {
            const int sides = 64;
            float outer = style == GunSightStyle.Scope ? rect.size.magnitude : radius + 2.5f * line;
            for (int i = 0; i < sides; i++)
            {
                float a = 2f * Mathf.PI * i / sides, b = 2f * Mathf.PI * (i + 1) / sides;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var v = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Quad(mesh, center + u * radius, center + v * radius, center + v * outer, center + u * outer, ink);
            }
        }
        float gap = style == GunSightStyle.Scope ? 3f * line : radius * .40f;
        Bar(mesh, center, -radius, -line, -gap, line, ink);
        Bar(mesh, center, gap, -line, radius, line, ink);
        Bar(mesh, center, -line, -radius, line, -gap, ink);
        if (style == GunSightStyle.Scope) Bar(mesh, center, -line, gap, line, radius, ink);
        if (style == GunSightStyle.Iron)
        {
            Bar(mesh, center, -radius, 0f, -radius + 2f * line, radius * .55f, ink);
            Bar(mesh, center, radius - 2f * line, 0f, radius, radius * .55f, ink);
        }
    }

    private static void Bar(VertexHelper mesh, Vector2 p, float x, float y, float right, float top, Color ink)
    {
        Quad(mesh, p + new Vector2(x,y), p + new Vector2(x,top), p + new Vector2(right,top), p + new Vector2(right,y), ink);
    }

    private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ink)
    {
        int i = mesh.currentVertCount;
        mesh.AddVert(a, ink, Vector2.zero);
        mesh.AddVert(b, ink, Vector2.zero);
        mesh.AddVert(c, ink, Vector2.zero);
        mesh.AddVert(d, ink, Vector2.zero);
        mesh.AddTriangle(i,i+1,i+2);
        mesh.AddTriangle(i,i+2,i+3);
    }
}