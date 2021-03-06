﻿using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SimpleDecalMesh
{
    [AddComponentMenu("")]
    public class TransformVolume : MonoBehaviour
    {
        public Volume Volume = new Volume(Vector3.zero, Vector3.one);

        public Vector3 Origin { get { return Volume.Origin; } }
        public Vector3 Size { get { return Volume.Size; } }

        public bool IsInBounds(Vector3[] points)
        {
            return GetBounds().Intersects(GetBounds(points));
        }

        public bool IsOnBorder(Vector3[] points)
        {
            if (!IsInBounds(points)) return false;
            return !IsInVolume(points);
        }

        public bool IsAnyInVolume(Vector3[] points)
        {
            for (int i = 0, count = points.Length; i < count; i++)
            {
                if (IsInVolume(points[i])) return true;
            }

            return false;
        }

        public bool IsInVolume(Vector3[] points)
        {
            for (int i = 0, count = points.Length; i < count; i++)
            {
                if (!IsInVolume(points[i])) return false;
            }

            return true;
        }

        public bool IsInVolume(Vector3 position)
        {
            for (int i = 0; i < 6; i++)
            {
                var plane = new Plane(GetSideDirection(i), GetSidePosition(i));
                if (plane.GetSide(position)) return false;
            }

            return true;
        }

        public Vector3[] GetCorners()
        {
            Vector3[] corners =
            {
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i].x *= Volume.Size.x;
                corners[i].y *= Volume.Size.y;
                corners[i].z *= Volume.Size.z;

                corners[i] = transform.TransformPoint(Volume.Origin + corners[i]);
            }

            return corners;
        }

        public Bounds GetBounds()
        {
            return GetBounds(GetCorners());
        }

        public Bounds GetBounds(Vector3[] points)
        {
            var center = Vector3.zero;
            for (int i = 0, count = points.Length; i < count; i++)
            {
                center += points[i];
            }
            center /= points.Length;

            var bounds = new Bounds(center, Vector3.zero);

            for (int i = 0; i < points.Length; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        public GameObject[] GetGameObjectsInBounds(LayerMask layerMask)
        {
            MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
            List<GameObject> list = new List<GameObject>();
            Bounds bounds = GetBounds();

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i].gameObject == gameObject) continue;
                if (meshRenderers[i].GetComponent<TransformVolume>() != null) continue;
                if ((1 << meshRenderers[i].gameObject.layer & layerMask.value) == 0) continue;
                if (bounds.Intersects(meshRenderers[i].bounds))
                {
                    list.Add(meshRenderers[i].gameObject);
                }
            }

            return list.ToArray();
        }

        public Vector3 GetSideDirection(int side)
        {
            Vector3[] sides = new Vector3[6];

            var right = Vector3.right;
            var up = Vector3.up;
            var forward = Vector3.forward;

            sides[0] = right;
            sides[1] = -right;
            sides[2] = up;
            sides[3] = -up;
            sides[4] = forward;
            sides[5] = -forward;

            return transform.TransformDirection(sides[side]);
        }

        public Vector3 GetSidePosition(int side)
        {
            Vector3[] sides = new Vector3[6];

            var right = Vector3.right;
            var up = Vector3.up;
            var forward = Vector3.forward;

            sides[0] = right;
            sides[1] = -right;
            sides[2] = up;
            sides[3] = -up;
            sides[4] = forward;
            sides[5] = -forward;

            return transform.TransformPoint(sides[side] * GetSizeAxis(side) + Volume.Origin);
        }

        public float GetSizeAxis(int side)
        {
            switch (side)
            {
                case 0:
                case 1: return Volume.Size.x * 0.5f;
                case 2:
                case 3: return Volume.Size.y * 0.5f;
                default: return Volume.Size.z * 0.5f;
            }
        }

#if UNITY_EDITOR
        public static Volume EditorVolumeControl(TransformVolume transformVolume, float handleSize, Color color)
        {
            Vector3 origin, size;
            Vector3[] controlHandles = new Vector3[6];
            var transform = transformVolume.transform;

            Handles.color = color;

            for (int i = 0; i < controlHandles.Length; i++)
            {
                controlHandles[i] = transformVolume.GetSidePosition(i);
            }

            controlHandles[0] = Handles.Slider(controlHandles[0], transform.right, handleSize, Handles.DotCap, 1);
            controlHandles[1] = Handles.Slider(controlHandles[1], transform.right, handleSize, Handles.DotCap, 1);
            controlHandles[2] = Handles.Slider(controlHandles[2], transform.up, handleSize, Handles.DotCap, 1);
            controlHandles[3] = Handles.Slider(controlHandles[3], transform.up, handleSize, Handles.DotCap, 1);
            controlHandles[4] = Handles.Slider(controlHandles[4], transform.forward, handleSize, Handles.DotCap, 1);
            controlHandles[5] = Handles.Slider(controlHandles[5], transform.forward, handleSize, Handles.DotCap, 1);

            origin.x = transform.InverseTransformPoint((controlHandles[0] + controlHandles[1]) * 0.5f).x;
            origin.y = transform.InverseTransformPoint((controlHandles[2] + controlHandles[3]) * 0.5f).y;
            origin.z = transform.InverseTransformPoint((controlHandles[4] + controlHandles[5]) * 0.5f).z;

            size.x = transform.InverseTransformPoint(controlHandles[0]).x - transform.InverseTransformPoint(controlHandles[1]).x;
            size.y = transform.InverseTransformPoint(controlHandles[2]).y - transform.InverseTransformPoint(controlHandles[3]).y;
            size.z = transform.InverseTransformPoint(controlHandles[4]).z - transform.InverseTransformPoint(controlHandles[5]).z;

            return new Volume(origin, size);
        }
#endif
    }
}
