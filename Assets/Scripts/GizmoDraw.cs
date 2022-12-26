using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[DisallowMultipleComponent]
public class GizmoDraw : MonoBehaviour
{
    public enum Type
    {
        Cube,
        Sphere,
        WireCube,
        WireSphere,
        CustomMesh
    }
    [Header("for custom mesh")]
    public Mesh mesh;
    [Header("Size and Colour")]
    public bool OnlySelected = true;
    public Type tyep = Type.Cube;
    public Vector3 offset;
    public float Size = 0.5f;
    public Color colour = Color.white;

    private void OnDrawGizmosSelected()
    {
        if(OnlySelected)
        {
            if(tyep == Type.Cube)
            {
                Gizmos.color = colour;
                Gizmos.DrawCube(transform.position + offset, new Vector3(Size, Size, Size));
            }
            if (tyep == Type.Sphere)
            {
                Gizmos.color = colour;
                Gizmos.DrawSphere(transform.position + offset, Size);
            }
            if (tyep == Type.WireCube)
            {
                Gizmos.color = colour;
                Gizmos.DrawWireCube(transform.position + offset, new Vector3(Size, Size, Size));
            }
            if (tyep == Type.WireSphere)
            {
                Gizmos.color = colour;
                Gizmos.DrawWireSphere(transform.position + offset, Size);
            }
            if (tyep == Type.CustomMesh)
            {
                Gizmos.color = colour;
                Gizmos.DrawMesh(mesh, 0, transform.position + offset, Quaternion.identity, new Vector3(Size, Size, Size));
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!OnlySelected)
        {
            if (tyep == Type.Cube)
            {
                Gizmos.color = colour;
                Gizmos.DrawCube(transform.position + offset, new Vector3(Size, Size, Size));
            }
            if (tyep == Type.Sphere)
            {
                Gizmos.color = colour;
                Gizmos.DrawSphere(transform.position + offset, Size);
            }
            if (tyep == Type.WireCube)
            {
                Gizmos.color = colour;
                Gizmos.DrawWireCube(transform.position + offset, new Vector3(Size, Size, Size));
            }
            if (tyep == Type.WireSphere)
            {
                Gizmos.color = colour;
                Gizmos.DrawWireSphere(transform.position + offset, Size);
            }
            if (tyep == Type.CustomMesh)
            {
                Gizmos.color = colour;
                Gizmos.DrawMesh(mesh, 0, transform.position + offset, Quaternion.identity, new Vector3(Size, Size, Size));
            }
        }
    }
}
