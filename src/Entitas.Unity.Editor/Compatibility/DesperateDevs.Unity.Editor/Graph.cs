using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DesperateDevs.Unity.Editor
{
    public sealed class Graph
    {
        readonly int _dataLength;
        readonly Vector3[] _linePoints;

        public Graph(int dataLength)
        {
            _dataLength = dataLength;
            _linePoints = new Vector3[Math.Max(dataLength, 2)];
        }

        public void Draw(float[] data, float height)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.12f));

            if (data == null || data.Length == 0)
                return;

            var min = data.Min();
            var max = data.Max();
            var avg = data.Average();
            var range = Math.Max(max - min, 0.0001f);

            Handles.BeginGUI();

            var avgY = Mathf.Lerp(rect.yMax - 4f, rect.y + 4f, (float)((avg - min) / range));
            Handles.color = Color.yellow;
            Handles.DrawLine(new Vector3(rect.x, avgY), new Vector3(rect.xMax, avgY));

            Handles.color = Color.red;
            var pointCount = Math.Min(data.Length, _dataLength);
            for (var i = 0; i < pointCount; i++)
            {
                var normalizedX = pointCount == 1 ? 0f : (float)i / (pointCount - 1);
                var normalizedY = (float)((data[i] - min) / range);
                _linePoints[i] = new Vector3(
                    Mathf.Lerp(rect.x + 2f, rect.xMax - 2f, normalizedX),
                    Mathf.Lerp(rect.yMax - 4f, rect.y + 4f, normalizedY),
                    0f);
            }

            if (pointCount >= 2)
                Handles.DrawAAPolyLine(2f, _linePoints.Take(pointCount).ToArray());

            Handles.EndGUI();

            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 18f), $"min {min:0.000}  avg {avg:0.000}  max {max:0.000}");
        }
    }
}
