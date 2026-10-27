#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.Editor
{
    public static class HouseScaleInspector
    {
        [MenuItem("BattleSummoner/Inspect House Scale Details", false, 100)]
        public static void Inspect()
        {
            StringBuilder sb = new StringBuilder();
            var rowHouses = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHouses != null && rowHouses.transform.childCount > 0)
            {
                var h0 = rowHouses.transform.GetChild(0);
                sb.AppendLine($"House[0] name: {h0.name}");
                sb.AppendLine($"  localPos: {h0.localPosition}, worldPos: {h0.position}");
                sb.AppendLine($"  localScale: {h0.localScale}, lossyScale: {h0.lossyScale}");
                sb.AppendLine($"  localRotation: {h0.localRotation.eulerAngles}, worldRotation: {h0.rotation.eulerAngles}");
                
                var mf = h0.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    sb.AppendLine($"  Mesh name: {mf.sharedMesh.name}");
                    sb.AppendLine($"  Mesh vertexCount: {mf.sharedMesh.vertexCount}");
                    sb.AppendLine($"  Mesh bounds: center = {mf.sharedMesh.bounds.center}, size = {mf.sharedMesh.bounds.size}");
                    if (mf.sharedMesh.vertices.Length > 0)
                    {
                        sb.AppendLine($"  First vert: {mf.sharedMesh.vertices[0]}, Last vert: {mf.sharedMesh.vertices[mf.sharedMesh.vertices.Length - 1]}");
                    }
                }

                var mr = h0.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    sb.AppendLine($"  Renderer enabled: {mr.enabled}, isVisible: {mr.isVisible}");
                    sb.AppendLine($"  Renderer world bounds: center = {mr.bounds.center}, size = {mr.bounds.size}");
                    sb.AppendLine($"  Materials count: {mr.sharedMaterials.Length}");
                    for (int m = 0; m < mr.sharedMaterials.Length; m++)
                    {
                        var mat = mr.sharedMaterials[m];
                        if (mat != null)
                        {
                            sb.AppendLine($"    Mat[{m}]: '{mat.name}', Shader: '{mat.shader.name}', MainTex: {mat.mainTexture != null}");
                        }
                        else
                        {
                            sb.AppendLine($"    Mat[{m}]: NULL");
                        }
                    }
                }

                sb.AppendLine($"  Child count: {h0.childCount}");
                for (int i = 0; i < h0.childCount; i++)
                {
                    var ch = h0.GetChild(i);
                    sb.AppendLine($"    Child[{i}] '{ch.name}': localPos = {ch.localPosition}, localScale = {ch.localScale}");
                    var cmr = ch.GetComponent<Renderer>();
                    if (cmr != null)
                    {
                        sb.AppendLine($"      Renderer world bounds size: {cmr.bounds.size}");
                    }
                    for (int j = 0; j < ch.childCount; j++)
                    {
                        var gch = ch.GetChild(j);
                        sb.AppendLine($"      GrandChild[{j}] '{gch.name}': localPos = {gch.localPosition}, localScale = {gch.localScale}");
                        var gmr = gch.GetComponent<Renderer>();
                        if (gmr != null)
                        {
                            sb.AppendLine($"        Renderer world bounds size: {gmr.bounds.size}, Mat: {(gmr.sharedMaterial != null ? gmr.sharedMaterial.name : "NULL")}");
                        }
                    }
                }
            }

            File.WriteAllText("HouseScaleReport.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
#endif
