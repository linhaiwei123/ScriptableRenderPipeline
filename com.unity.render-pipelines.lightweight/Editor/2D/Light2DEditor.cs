using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering.LWRP;
using System.Linq;
using System.Collections.Generic;
using Unity.Path2D;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    [CustomEditor(typeof(Light2D))]
    [CanEditMultipleObjects]
    internal class Light2DEditor : Editor
    {
        internal class ShapeEditor : PolygonEditor
        {
            const string k_ShapePath = "m_ShapePath";

            protected override int GetPointCount(SerializedObject serializedObject)
            {
                return (serializedObject.targetObject as Light2D).shapePath.Length;
            }

            protected override Vector3 GetPoint(SerializedObject serializedObject, int index)
            {
                return (serializedObject.targetObject as Light2D).shapePath[index];
            }

            protected override void SetPoint(SerializedObject serializedObject, int index, Vector3 position)
            {
                serializedObject.Update();
                serializedObject.FindProperty(k_ShapePath).GetArrayElementAtIndex(index).vector3Value = position;
                serializedObject.ApplyModifiedProperties();
            }

            protected override void InsertPoint(SerializedObject serializedObject, int index, Vector3 position)
            {
                serializedObject.Update();
                var shapePath = serializedObject.FindProperty(k_ShapePath);
                shapePath.InsertArrayElementAtIndex(index);
                shapePath.GetArrayElementAtIndex(index).vector3Value = position;
                serializedObject.ApplyModifiedProperties();
            }

            protected override void RemovePoint(SerializedObject serializedObject, int index)
            {
                serializedObject.Update();
                serializedObject.FindProperty(k_ShapePath).DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
            }
        }

        static string k_TexturePath = "Textures/";

        private class Styles
        {
            public static Texture lightCapTopRight;
            public static Texture lightCapTopLeft;
            public static Texture lightCapBottomLeft;
            public static Texture lightCapBottomRight;
            public static Texture lightCapUp;
            public static Texture lightCapDown;

            public static GUIContent generalLightType = EditorGUIUtility.TrTextContent("Light Type", "Specify the light type");
            public static GUIContent generalFalloffSize = EditorGUIUtility.TrTextContent("Falloff", "Specify the falloff of the light");
            public static GUIContent generalFalloffIntensity = EditorGUIUtility.TrTextContent("Falloff Intensity", "Adjusts the falloff curve");
            public static GUIContent generalLightColor = EditorGUIUtility.TrTextContent("Color", "Specify the light color");
            public static GUIContent generalVolumeOpacity = EditorGUIUtility.TrTextContent("Volume Opacity", "Specify the light's volumetric light volume opacity");
            public static GUIContent generalLightOperation = EditorGUIUtility.TrTextContent("Light Operation", "Specify the light operation");

            public static GUIContent pointLightQuality = EditorGUIUtility.TrTextContent("Quality", "Use accurate if there are noticable visual issues");
            public static GUIContent pointLightInnerAngle =  EditorGUIUtility.TrTextContent("Inner Angle", "Specify the inner angle of the light");
            public static GUIContent pointLightOuterAngle = EditorGUIUtility.TrTextContent("Outer Angle", "Specify the outer angle of the light");
            public static GUIContent pointLightInnerRadius = EditorGUIUtility.TrTextContent("Inner Radius", "Specify the inner radius of the light");
            public static GUIContent pointLightOuterRadius = EditorGUIUtility.TrTextContent("Outer Radius", "Specify the outer radius of the light");
            public static GUIContent pointLightZDistance = EditorGUIUtility.TrTextContent("Distance", "Specify the Z Distance of the light");
            public static GUIContent pointLightCookie = EditorGUIUtility.TrTextContent("Cookie", "Specify a sprite as the cookie for the light");

            public static GUIContent shapeLightNoLightDefined = EditorGUIUtility.TrTextContentWithIcon("No valid Shape Light type is defined.", MessageType.Error);
            public static GUIContent shapeLightSprite = EditorGUIUtility.TrTextContent("Sprite", "Specify the sprite");
            public static GUIContent shapeLightParametricRadius = EditorGUIUtility.TrTextContent("Radius", "Adjust the size of the object");
            public static GUIContent shapeLightParametricSides = EditorGUIUtility.TrTextContent("Sides", "Adjust the shapes number of sides");
            public static GUIContent shapeLightFalloffOffset = EditorGUIUtility.TrTextContent("Falloff Offset", "Specify the shape's falloff offset");
            public static GUIContent shapeLightAngleOffset = EditorGUIUtility.TrTextContent("Angle Offset", "Adjust the rotation of the object");
            public static GUIContent shapeLightOverlapMode = EditorGUIUtility.TrTextContent("Light Overlap Mode", "Specify what should happen when this light overlaps other lights");
            public static GUIContent shapeLightOrder = EditorGUIUtility.TrTextContent("Light Order", "Shape light order");

            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply this light to the specified sorting layers.");
            public static GUIContent sortingLayerAll = EditorGUIUtility.TrTempContent("All");
            public static GUIContent sortingLayerNone = EditorGUIUtility.TrTempContent("None");
            public static GUIContent sortingLayerMixed = EditorGUIUtility.TrTempContent("Mixed...");

            public static GUIContent renderPipelineUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("Lightweight scriptable renderpipeline asset must be assigned in graphics settings", MessageType.Warning);
            public static GUIContent asset2DUnassignedWarning = EditorGUIUtility.TrTextContentWithIcon("2D renderer data must be assigned to your lightweight render pipeline asset", MessageType.Warning);
        }

        static float s_GlobalLightGizmoSize = 1.2f;
        static float s_AngleCapSize = 0.16f * s_GlobalLightGizmoSize;
        static float s_AngleCapOffset = 0.08f * s_GlobalLightGizmoSize;
        static float s_AngleCapOffsetSecondary = -0.05f;
        static float s_RangeCapSize = 0.025f * s_GlobalLightGizmoSize;
        static Handles.CapFunction s_RangeCapFunction = Handles.DotHandleCap;
        static float s_InnerRangeCapSize = 0.08f * s_GlobalLightGizmoSize;
        static Handles.CapFunction s_InnerRangeCapFunction = Handles.SphereHandleCap;

        SerializedProperty m_LightType;
        SerializedProperty m_LightColor;
        SerializedProperty m_ApplyToSortingLayers;
        SerializedProperty m_VolumetricAlpha;
        SerializedProperty m_LightOperation;
        SerializedProperty m_FalloffCurve;

        // Point Light Properties
        SerializedProperty m_PointInnerAngle;
        SerializedProperty m_PointOuterAngle;
        SerializedProperty m_PointInnerRadius;
        SerializedProperty m_PointOuterRadius;
        SerializedProperty m_PointZDistance;
        SerializedProperty m_PointLightCookie;
        SerializedProperty m_PointLightQuality;

        // Shape Light Properies
        SerializedProperty m_ShapeLightRadius;
        SerializedProperty m_ShapeLightFalloffSize;
        SerializedProperty m_ShapeLightParametricSides;
        SerializedProperty m_ShapeLightParametricAngleOffset;
        SerializedProperty m_ShapeLightFalloffOffset;
        SerializedProperty m_ShapeLightSprite;
        SerializedProperty m_ShapeLightOrder;
        SerializedProperty m_ShapeLightOverlapMode;

        string[] m_LayerNames;
        bool m_ModifiedMesh = false;
        int[] m_LightOperationIndices;
        GUIContent[] m_LightOperationNames;
        bool m_AnyLightOperationEnabled = false;

        private Light2D lightObject { get { return target as Light2D; } }
        private Rect m_SortingLayerDropdownRect = new Rect();
        private SortingLayer[] m_AllSortingLayers;
        private GUIContent[] m_AllSortingLayerNames;
        private List<int> m_ApplyToSortingLayersList;

        ShapeEditor m_ShapeEditor = new ShapeEditor();

        #region Handle Utilities

        public static void TriangleCapTopRight(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightCapTopRight == null)
                Styles.lightCapTopRight = Resources.Load<Texture>(k_TexturePath + "LightCapTopRight");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightCapTopRight, position, rotation, size, eventType);
        }

        public static void TriangleCapTopLeft(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightCapTopLeft == null)
                Styles.lightCapTopLeft = Resources.Load<Texture>(k_TexturePath + "LightCapTopLeft");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightCapTopLeft, position, rotation, size, eventType);
        }

        public static void TriangleCapBottomRight(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightCapBottomRight == null)
                Styles.lightCapBottomRight = Resources.Load<Texture>(k_TexturePath + "LightCapBottomRight");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightCapBottomRight, position, rotation, size, eventType);
        }

        public static void TriangleCapBottomLeft(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightCapBottomLeft == null)
                Styles.lightCapBottomLeft = Resources.Load<Texture>(k_TexturePath + "LightCapBottomLeft");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightCapBottomLeft, position, rotation, size, eventType);
        }

        public static void SemiCircleCapUp(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightCapUp == null)
                Styles.lightCapUp = Resources.Load<Texture>(k_TexturePath + "LightCapUp");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightCapUp, position, rotation, size, eventType);
        }

        public static void SemiCircleCapDown(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (Styles.lightCapDown == null)
                Styles.lightCapDown = Resources.Load<Texture>(k_TexturePath + "LightCapDown");
            Light2DEditorUtility.GUITextureCap(controlID, Styles.lightCapDown, position, rotation, size, eventType);
        }
        #endregion

        private void OnEnable()
        {
            m_LightType = serializedObject.FindProperty("m_LightType");
            m_LightColor = serializedObject.FindProperty("m_Color");
            m_ApplyToSortingLayers = serializedObject.FindProperty("m_ApplyToSortingLayers");
            m_VolumetricAlpha = serializedObject.FindProperty("m_LightVolumeOpacity");
            m_LightOperation = serializedObject.FindProperty("m_LightOperationIndex");
            m_FalloffCurve = serializedObject.FindProperty("m_FalloffCurve");

            // Point Light
            m_PointInnerAngle = serializedObject.FindProperty("m_PointLightInnerAngle");
            m_PointOuterAngle = serializedObject.FindProperty("m_PointLightOuterAngle");
            m_PointInnerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
            m_PointOuterRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
            m_PointZDistance = serializedObject.FindProperty("m_PointLightDistance");
            m_PointLightCookie = serializedObject.FindProperty("m_LightCookieSprite");
            m_PointLightQuality = serializedObject.FindProperty("m_PointLightQuality");

            // Shape Light
            m_ShapeLightRadius = serializedObject.FindProperty("m_ShapeLightRadius");
            m_ShapeLightFalloffSize = serializedObject.FindProperty("m_ShapeLightFalloffSize");
            m_ShapeLightParametricSides = serializedObject.FindProperty("m_ShapeLightParametricSides");
            m_ShapeLightParametricAngleOffset = serializedObject.FindProperty("m_ShapeLightParametricAngleOffset");
            m_ShapeLightFalloffOffset = serializedObject.FindProperty("m_ShapeLightFalloffOffset");
            m_ShapeLightSprite = serializedObject.FindProperty("m_LightCookieSprite");
            m_ShapeLightOrder = serializedObject.FindProperty("m_ShapeLightOrder");
            m_ShapeLightOverlapMode = serializedObject.FindProperty("m_ShapeLightOverlapMode");

            m_AnyLightOperationEnabled = false;
            var light = target as Light2D;
            var lightOperationIndices = new List<int>();
            var lightOperationNames = new List<string>();
            var pipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            var rendererData = pipelineAsset != null ? pipelineAsset.scriptableRendererData as _2DRendererData : null;
            if (rendererData != null)
            {
                for (int i = 0; i < rendererData.lightOperations.Length; ++i)
                {
                    var lightOperation = rendererData.lightOperations[i];
                    if (lightOperation.enabled)
                    {
                        lightOperationIndices.Add(i);
                        lightOperationNames.Add(lightOperation.name);
                    }
                }

                m_AnyLightOperationEnabled = lightOperationIndices.Count != 0;
            }
            else
            {
                for (int i = 0; i < 3; ++i)
                {
                    lightOperationIndices.Add(i);
                    lightOperationNames.Add("Type" + i);
                }
            }

            m_LightOperationIndices = lightOperationIndices.ToArray();
            m_LightOperationNames = lightOperationNames.Select(x => EditorGUIUtility.TrTextContent(x)).ToArray();

            m_AllSortingLayers = SortingLayer.layers;
            m_AllSortingLayerNames = m_AllSortingLayers.Select(x => new GUIContent(x.name)).ToArray();

            int applyToSortingLayersSize = m_ApplyToSortingLayers.arraySize;
            m_ApplyToSortingLayersList = new List<int>(applyToSortingLayersSize);

            for (int i = 0; i < applyToSortingLayersSize; ++i)
            {
                int layerID = m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue;
                if (SortingLayer.IsValid(layerID))
                    m_ApplyToSortingLayersList.Add(layerID);
            }
        }

        private void OnPointLight(SerializedObject serializedObject)
        {
            EditorGUILayout.PropertyField(m_PointLightQuality, Styles.pointLightQuality);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(m_PointInnerAngle, 0, 360, Styles.pointLightInnerAngle);
            if (EditorGUI.EndChangeCheck())
                m_PointInnerAngle.floatValue = Mathf.Min(m_PointInnerAngle.floatValue, m_PointOuterAngle.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(m_PointOuterAngle, 0, 360, Styles.pointLightOuterAngle);
            if (EditorGUI.EndChangeCheck())
                m_PointOuterAngle.floatValue = Mathf.Max(m_PointInnerAngle.floatValue, m_PointOuterAngle.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PointInnerRadius, Styles.pointLightInnerRadius);
            if (EditorGUI.EndChangeCheck())
                m_PointInnerRadius.floatValue = Mathf.Min(m_PointInnerRadius.floatValue, m_PointOuterRadius.floatValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_PointOuterRadius, Styles.pointLightOuterRadius);
            if (EditorGUI.EndChangeCheck())
                m_PointOuterRadius.floatValue = Mathf.Max(m_PointInnerRadius.floatValue, m_PointOuterRadius.floatValue);

            EditorGUILayout.PropertyField(m_PointZDistance, Styles.pointLightZDistance);
            EditorGUILayout.Slider(m_FalloffCurve, 0, 1, Styles.generalFalloffIntensity);
            EditorGUILayout.PropertyField(m_PointLightCookie, Styles.pointLightCookie);
            if (m_PointInnerRadius.floatValue < 0) m_PointInnerRadius.floatValue = 0;
            if (m_PointOuterRadius.floatValue < 0) m_PointOuterRadius.floatValue = 0;
            if (m_PointZDistance.floatValue < 0) m_PointZDistance.floatValue = 0;
        }

        private bool OnShapeLight(Light2D.LightType lightProjectionType, bool changedType, SerializedObject serializedObject)
        {
            if (!m_AnyLightOperationEnabled)
            {
                EditorGUILayout.HelpBox(Styles.shapeLightNoLightDefined);
                return false;
            }

            bool updateMesh = false;

            if (lightProjectionType == Light2D.LightType.Sprite)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ShapeLightSprite, Styles.shapeLightSprite);
                updateMesh |= EditorGUI.EndChangeCheck();
            }
            else if (lightProjectionType == Light2D.LightType.Parametric || lightProjectionType == Light2D.LightType.Freeform)
            {
                if (m_ModifiedMesh)
                    updateMesh = true;

                if (changedType)
                {
                    int sides = m_ShapeLightParametricSides.intValue;
                    if (lightProjectionType == Light2D.LightType.Parametric) sides = 6;
                    else if (lightProjectionType == Light2D.LightType.Freeform) sides = 4; // This one should depend on if this has data at the moment
                    m_ShapeLightParametricSides.intValue = sides;
                }

                m_ModifiedMesh = false;

                if (lightProjectionType == Light2D.LightType.Parametric)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.Slider(m_ShapeLightRadius, 0, 20, Styles.shapeLightParametricRadius);
                    EditorGUILayout.IntSlider(m_ShapeLightParametricSides, 3, 24, Styles.shapeLightParametricSides);
                    EditorGUILayout.Slider(m_ShapeLightParametricAngleOffset, 0, 359, Styles.shapeLightAngleOffset);
                }

                EditorGUILayout.Slider(m_ShapeLightFalloffSize, 0, 5, Styles.generalFalloffSize);
                EditorGUILayout.Slider(m_FalloffCurve, 0, 1, Styles.generalFalloffIntensity);
                if (lightProjectionType == Light2D.LightType.Parametric)
                {
                    EditorGUILayout.PropertyField(m_ShapeLightFalloffOffset, Styles.shapeLightFalloffOffset);
                }
            }

            EditorGUILayout.PropertyField(m_ShapeLightOverlapMode, Styles.shapeLightOverlapMode);
            EditorGUILayout.PropertyField(m_ShapeLightOrder, Styles.shapeLightOrder);


            return updateMesh;
        }

        private void OnTargetSortingLayers()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Styles.sortingLayerPrefixLabel);

            GUIContent selectedLayers;
            if (m_ApplyToSortingLayersList.Count == 1)
                selectedLayers = new GUIContent(SortingLayer.IDToName(m_ApplyToSortingLayersList[0]));
            else if (m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length)
                selectedLayers = Styles.sortingLayerAll;
            else if (m_ApplyToSortingLayersList.Count == 0)
                selectedLayers = Styles.sortingLayerNone;
            else
                selectedLayers = Styles.sortingLayerMixed;

            bool buttonDown = EditorGUILayout.DropdownButton(selectedLayers, FocusType.Keyboard, EditorStyles.popup);

            if (Event.current.type == EventType.Repaint)
                m_SortingLayerDropdownRect = GUILayoutUtility.GetLastRect();

            if (buttonDown)
            {
                GenericMenu menu = new GenericMenu();
                menu.allowDuplicateNames = true;

                GenericMenu.MenuFunction2 menuFunction = (layerIDObject) =>
                {
                    int layerID = (int)layerIDObject;

                    if (m_ApplyToSortingLayersList.Contains(layerID))
                        m_ApplyToSortingLayersList.RemoveAll(id => id == layerID);
                    else
                        m_ApplyToSortingLayersList.Add(layerID);


                    // Compare the list to our array

                    
                    // Copy the new sorting layer list into our array
                    m_ApplyToSortingLayers.ClearArray();
                    for (int i = 0; i < m_ApplyToSortingLayersList.Count; ++i)
                    {
                        m_ApplyToSortingLayers.InsertArrayElementAtIndex(i);
                        m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue = m_ApplyToSortingLayersList[i];
                    }


                    for (int i = 0; i < targets.Length; i++)
                    {
                        Light2D light = targets[i] as Light2D;
                        if (light.lightType == Light2D.LightType.Global)
                            Light2D.RemoveGlobalLight(light);
                    }

                    serializedObject.ApplyModifiedProperties();

                    for (int i = 0; i < targets.Length; i++)
                    {
                        Light2D light = targets[i] as Light2D;
                        if (light.lightType == Light2D.LightType.Global)
                            Light2D.AddGlobalLight(light);
                    }
                };

                for (int i = 0; i < m_AllSortingLayers.Length; ++i)
                {
                    var sortingLayer = m_AllSortingLayers[i];
                    menu.AddItem(m_AllSortingLayerNames[i], m_ApplyToSortingLayersList.Contains(sortingLayer.id), menuFunction, sortingLayer.id);
                }

                menu.DropDown(m_SortingLayerDropdownRect);
            }

            EditorGUILayout.EndHorizontal();
        }

        private Vector3 DrawAngleSlider2D(Transform transform, Quaternion rotation, float radius, float offset, Handles.CapFunction capFunc, float capSize, bool leftAngle, bool drawLine, bool useCapOffset, ref float angle)
        {
            float oldAngle = angle;

            float angleBy2 = (angle / 2) * (leftAngle ? -1.0f : 1.0f);
            Vector3 trcwPos = Quaternion.AngleAxis(angleBy2, -transform.forward) * (transform.up);
            Vector3 cwPos = transform.position + trcwPos * (radius + offset);

            float direction = leftAngle ? 1 : -1;

            // Offset the handle
            float size = .25f * capSize;

            Vector3 handleOffset = useCapOffset ? rotation * new Vector3(direction * size, 0, 0) : Vector3.zero;

            EditorGUI.BeginChangeCheck();
            var id = GUIUtility.GetControlID("AngleSlider".GetHashCode(), FocusType.Passive);
            Vector3 cwHandle = Handles.Slider2D(id, cwPos, handleOffset, Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, capSize, capFunc, Vector3.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 toCwHandle = (transform.position - cwHandle).normalized;

                angle = 360 - 2 * Quaternion.Angle(Quaternion.FromToRotation(transform.up, toCwHandle), Quaternion.identity);
                angle = Mathf.Round(angle * 100) / 100f;

                float side = Vector3.Dot(direction * transform.right, toCwHandle);
                if (side < 0)
                {
                    if (oldAngle < 180)
                        angle = 0;
                    else 
                        angle = 360;
                }
            }

            if (drawLine)
                Handles.DrawLine(transform.position, cwHandle);

            return cwHandle;
        }

        private void DEBUG_DrawCaps(Vector3 position, Quaternion rotation, float size)
        {
            Vector3 topLeft = rotation * new Vector3(-size, size, 0) + position;
            Vector3 topRight = rotation * new Vector3(size, size, 0) + position;
            Vector3 bottomRight = rotation * new Vector3(size, -size, 0) + position;
            Vector3 bottomLeft = rotation * new Vector3(-size, -size, 0) + position;

            Handles.DrawLine(topLeft, topRight);
            Handles.DrawLine(topRight, bottomRight);
            Handles.DrawLine(bottomRight, bottomLeft);
            Handles.DrawLine(bottomLeft, topLeft);
        }

        private float DrawAngleHandle(Transform transform, float radius, float offset, Handles.CapFunction capLeft, Handles.CapFunction capRight, ref float angle)
        {
            float old = angle;
            float handleOffset = HandleUtility.GetHandleSize(transform.position) * offset;
            float handleSize = HandleUtility.GetHandleSize(transform.position) * s_AngleCapSize;

            Quaternion rotLt = Quaternion.AngleAxis(-angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotLt, radius, handleOffset, capLeft, handleSize, true, true, true, ref angle);

            Quaternion rotRt = Quaternion.AngleAxis(angle / 2, -transform.forward) * transform.rotation;
            DrawAngleSlider2D(transform, rotRt, radius, handleOffset, capRight, handleSize, false, true, true, ref angle);

            return angle - old;
        }

        private void DrawRadiusArc(Transform transform, float radius, float angle, int steps, Handles.CapFunction capFunc, float capSize, bool even)
        {
            Handles.DrawWireArc(transform.position, transform.forward, Quaternion.AngleAxis(180 - angle / 2, transform.forward) * -transform.up, angle, radius);
        }

        private void DrawAngleHandles(Light2D light)
        {
            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerAngle = light.pointLightOuterAngle;
            float diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, s_AngleCapOffset, TriangleCapTopRight, TriangleCapBottomRight, ref outerAngle);
            light.pointLightOuterAngle = outerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = Mathf.Max(0.0f, light.pointLightInnerAngle + diff);

            float innerAngle = light.pointLightInnerAngle;
            diff = DrawAngleHandle(light.transform, light.pointLightOuterRadius, -s_AngleCapOffset, TriangleCapTopLeft, TriangleCapBottomLeft, ref innerAngle);
            light.pointLightInnerAngle = innerAngle;

            if (diff != 0.0f)
                light.pointLightInnerAngle = light.pointLightInnerAngle < light.pointLightOuterAngle ? light.pointLightInnerAngle : light.pointLightOuterAngle;

            light.pointLightInnerAngle = Mathf.Min(light.pointLightInnerAngle, light.pointLightOuterAngle);

            Handles.color = oldColor;
        }

        private float DrawRadiusHandle(Transform transform, float radius, float angle, Handles.CapFunction capFunc, float capSize, ref Vector3 handlePos)
        {
            Vector3 dir = (Quaternion.AngleAxis(angle, -transform.forward) * transform.up).normalized;
            Vector3 handle = transform.position + dir * radius;
            handlePos = Handles.FreeMoveHandle(handle, Quaternion.identity, HandleUtility.GetHandleSize(transform.position) * capSize, Vector3.zero, capFunc);
            return (transform.position - handlePos).magnitude;
        }

        private void DrawRangeHandles(Light2D light)
        {
            var handleColor = Handles.color;
            var dummy = 0.0f;
            bool radiusChanged = false;
            Vector3 handlePos = Vector3.zero;
            Quaternion rotLeft = Quaternion.AngleAxis(0, -light.transform.forward) * light.transform.rotation;
            float handleOffset = HandleUtility.GetHandleSize(light.transform.position) * s_AngleCapOffsetSecondary;
            float handleSize = HandleUtility.GetHandleSize(light.transform.position) * s_AngleCapSize;

            var oldColor = Handles.color;
            Handles.color = Color.yellow;

            float outerRadius = light.pointLightOuterRadius;
            EditorGUI.BeginChangeCheck();
            Vector3 returnPos = DrawAngleSlider2D(light.transform, rotLeft, outerRadius, -handleOffset, SemiCircleCapUp, handleSize, false, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                var vec = (returnPos - light.transform.position).normalized;
                light.transform.up = new Vector3(vec.x, vec.y, 0);
                outerRadius = (returnPos - light.transform.position).magnitude;
                outerRadius = outerRadius + handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(light.transform, light.pointLightOuterRadius, light.pointLightOuterAngle, 0, s_RangeCapFunction, s_RangeCapSize, false);

            Handles.color = Color.gray;
            float innerRadius = light.pointLightInnerRadius;
            EditorGUI.BeginChangeCheck();
            returnPos = DrawAngleSlider2D(light.transform, rotLeft, innerRadius, handleOffset, SemiCircleCapDown, handleSize, true, false, false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                innerRadius = (returnPos - light.transform.position).magnitude;
                innerRadius = innerRadius - handleOffset;
                radiusChanged = true;
            }
            DrawRadiusArc(light.transform, light.pointLightInnerRadius, light.pointLightOuterAngle, 0, s_InnerRangeCapFunction, s_InnerRangeCapSize, false);

            Handles.color = oldColor;

            if (radiusChanged)
            {
                light.pointLightInnerRadius = (outerRadius < innerRadius) ? outerRadius : innerRadius;
                light.pointLightOuterRadius = (innerRadius > outerRadius) ? innerRadius : outerRadius;
            }
            
            Handles.color = handleColor;
        }

        protected virtual void OnSceneGUI()
        {
            var light = target as Light2D;
            if (light == null)
                return;

            if (light.lightType == Light2D.LightType.Point)
            {

                Undo.RecordObject(light, "Edit Target Light");
                Undo.RecordObject(light.transform, light.transform.GetHashCode() + "_undo");

                DrawRangeHandles(light);
                DrawAngleHandles(light);

                if (GUI.changed)
                    EditorUtility.SetDirty(light);
            }
            else
            {
                Transform t = light.transform;
                Vector3 falloffOffset = light.shapeLightFalloffOffset;

                if (light.lightType == Light2D.LightType.Sprite)
                {
                    var cookieSprite = light.lightCookieSprite;
                    if (cookieSprite != null)
                    {
                        Vector3 min = cookieSprite.bounds.min;
                        Vector3 max = cookieSprite.bounds.max;

                        Vector3 v0 = t.TransformPoint(new Vector3(min.x, min.y));
                        Vector3 v1 = t.TransformPoint(new Vector3(max.x, min.y));
                        Vector3 v2 = t.TransformPoint(new Vector3(max.x, max.y));
                        Vector3 v3 = t.TransformPoint(new Vector3(min.x, max.y));
                        Handles.DrawLine(v0, v1);
                        Handles.DrawLine(v1, v2);
                        Handles.DrawLine(v2, v3);
                        Handles.DrawLine(v3, v0);
                    }
                }
                else if (light.lightType == Light2D.LightType.Parametric)
                {
                    float radius = light.shapeLightRadius;
                    float sides = light.shapeLightParametricSides;
                    float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * light.shapeLightParametricAngleOffset;

                    if (sides < 3)
                        sides = 4;

                    if (sides == 4)
                        angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * light.shapeLightParametricAngleOffset;

                    Vector3 startPoint = new Vector3(radius * Mathf.Cos(angleOffset), radius * Mathf.Sin(angleOffset), 0);
                    Vector3 featherStartPoint = startPoint + light.shapeLightFalloffSize * Vector3.Normalize(startPoint);
                    float radiansPerSide = 2 * Mathf.PI / sides;
                    for (int i = 0; i < sides; i++)
                    {
                        float endAngle = (i + 1) * radiansPerSide;
                        Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0);
                        Vector3 featherEndPoint = endPoint + light.shapeLightFalloffSize * Vector3.Normalize(endPoint);

                        Handles.DrawLine(t.TransformPoint(startPoint), t.TransformPoint(endPoint));
                        Handles.DrawLine(t.TransformPoint(featherStartPoint + falloffOffset), t.TransformPoint(featherEndPoint + falloffOffset));

                        startPoint = endPoint;
                        featherStartPoint = featherEndPoint;
                    }
                }
                else if(light.lightType == Light2D.LightType.Freeform)
                {
                    m_ShapeEditor.OnGUI(target);

                    // Draw the falloff shape's outline
                    List<Vector2> falloffShape = light.GetFalloffShape();
                    Handles.color = Color.white;
                    for (int i = 0; i < falloffShape.Count-1; i++)
                    {
                        Handles.DrawLine(t.TransformPoint(falloffShape[i]), t.TransformPoint(falloffShape[i + 1]));
                    }
                    Handles.DrawLine(t.TransformPoint(falloffShape[falloffShape.Count - 1]), t.TransformPoint(falloffShape[0]));
                }
            }
        }

        public override void OnInspectorGUI()
        {
            LightweightRenderPipeline pipeline = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as LightweightRenderPipeline;
            if (pipeline == null)
            {
                EditorGUILayout.HelpBox(Styles.renderPipelineUnassignedWarning);
                return;
            }

            LightweightRenderPipelineAsset asset = LightweightRenderPipeline.asset;
            _2DRendererData assetData = asset.scriptableRendererData as _2DRendererData; 
            if(assetData == null)
            {
                EditorGUILayout.HelpBox(Styles.asset2DUnassignedWarning);
                return;
            }


            bool updateMesh = false;
            

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_LightType, Styles.generalLightType);
            updateMesh |= EditorGUI.EndChangeCheck();

            switch (m_LightType.intValue)
            {
                case (int)Light2D.LightType.Point:
                    {
                        OnPointLight(serializedObject);
                    }
                    break;
                case (int)Light2D.LightType.Parametric:
                case (int)Light2D.LightType.Freeform:
                case (int)Light2D.LightType.Sprite:
                    {
                        
                        updateMesh |= OnShapeLight((Light2D.LightType)m_LightType.intValue, updateMesh, serializedObject);
                    }
                    break;
            }

            Color previousColor = m_LightColor.colorValue;
            EditorGUILayout.IntPopup(m_LightOperation, m_LightOperationNames, m_LightOperationIndices, Styles.generalLightOperation);
            EditorGUILayout.PropertyField(m_LightColor, Styles.generalLightColor);
            if(m_LightType.intValue != (int)Light2D.LightType.Global)
                EditorGUILayout.Slider(m_VolumetricAlpha, 0, 1, Styles.generalVolumeOpacity);

            OnTargetSortingLayers();

            if (lightObject.lightType == Light2D.LightType.Freeform )
            {
                // Draw the edit shape tool button here.
            }

            serializedObject.ApplyModifiedProperties();

            if (updateMesh)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Light2D light = (Light2D)targets[i];
                    light.UpdateMesh();
                }
            }
        }
    }
}
