using UnityEngine;
using UnityEngine.UI;

// The same red sun and basalt altar silhouette appears on the reveal and corner tracker.
public sealed class OfferingSeal : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var center = rectTransform.rect.center;
        float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 100f;
        Vector2 At(float x, float y) => center + new Vector2(x, y) * scale;
        void Tri(Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(i, i+1, i+2);
        }
        Color brass = new Color(.91f,.62f,.36f);
        Color basalt = new Color(.14f,.055f,.07f);
        for (int i=0; i<48; i++)
        {
            float a=i*Mathf.PI/24f, b=(i+1)*Mathf.PI/24f;
            Vector2 Ray(float angle,float r) => At(Mathf.Cos(angle)*r, 4f+Mathf.Sin(angle)*r);
            Tri(At(0,4), Ray(a,30), Ray(b,30),color);
            Tri(Ray(a,37),Ray(b,37),Ray(b,35.6f),brass);
            Tri(Ray(a,37),Ray(b,35.6f),Ray(a,35.6f),brass);
        }
        Tri(At(-36,-24),At(-27,15),At(-15,-18),basalt);
        Tri(At(36,-24),At(27,15),At(15,-18),basalt);
        Tri(At(-38,-21),At(38,-21),At(26,-29),brass);
        Tri(At(-38,-21),At(26,-29),At(-26,-29),brass);
        Tri(At(-12,-21),At(0,-11),At(12,-21),basalt);
    }
}
