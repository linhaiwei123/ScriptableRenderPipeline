using UnityEngine.Rendering;
using UnityEngine.Rendering.LookDev;

using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Main entry point for scripting LookDev
    /// </summary>
    public static class LookDev
    {
        const string lastRenderingDataSavePath = "Library/LookDevConfig.asset";

        //TODO: ensure only one displayer at time for the moment
        static IViewDisplayer s_Displayer;
        static Compositer s_Compositor;
        static StageCache s_Stages;
        static ComparisonGizmo s_Comparator;

        static IDataProvider dataProvider
            => RenderPipelineManager.currentPipeline as IDataProvider;

        public static Context currentContext { get; private set; }

        //[TODO: not compatible with multiple displayer. To rework if needed]
        public static IViewDisplayer currentDisplayer => s_Displayer;

        public static bool open { get; private set; }
        
        /// <summary>
        /// Does LookDev is supported with the current render pipeline?
        /// </summary>
        public static bool supported => dataProvider != null;
        
        static LookDev()
            => currentContext = LoadConfigInternal() ?? GetDefaultContext();

        static Context GetDefaultContext()
            => UnityEngine.ScriptableObject.CreateInstance<Context>();

        public static void ResetConfig()
            => currentContext = GetDefaultContext();

        static Context LoadConfigInternal(string path = lastRenderingDataSavePath)
        {
            var objs = InternalEditorUtility.LoadSerializedFileAndForget(path);
            var last = (objs.Length > 0 ? objs[0] : null) as Context;
            if (last != null && !last.Equals(null))
                return ((Context)last);
            return null;
        }

        public static void LoadConfig(string path = lastRenderingDataSavePath)
        {
            var last = LoadConfigInternal(path);
            if (last != null)
            {
                last.Validate();
                currentContext = last;
            }
        }

        public static void SaveConfig(string path = lastRenderingDataSavePath)
        {
            if (currentContext != null && !currentContext.Equals(null))
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { currentContext }, path, true);
        }

        [MenuItem("Window/Experimental/NEW Look Dev", false, -1)]
        public static void Open()
        {
            if (!supported)
                throw new System.Exception("LookDev is not supported by this Scriptable Render Pipeline: "
                    + (RenderPipelineManager.currentPipeline == null ? "No SRP in use" : RenderPipelineManager.currentPipeline.ToString()));

            s_Displayer = EditorWindow.GetWindow<DisplayWindow>();
            ConfigureLookDev();
        }

        [Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            var windows = Resources.FindObjectsOfTypeAll<DisplayWindow>();
            s_Displayer = windows.Length > 0 ? windows[0] : null;
            open = s_Displayer != null;
            if (open)
                ConfigureLookDev();
        }

        static void ConfigureLookDev()
        {
            open = true;
            LoadConfig();
            WaitingSRPReloadForConfiguringRenderer(5);
        }

        static void WaitingSRPReloadForConfiguringRenderer(int maxAttempt, int attemptNumber = 0)
        {
            if (supported)
                ConfigureRenderer();
            else if (attemptNumber < maxAttempt)
                EditorApplication.delayCall +=
                    () => WaitingSRPReloadForConfiguringRenderer(maxAttempt, ++attemptNumber);
            else if (s_Displayer is EditorWindow)
                (s_Displayer as EditorWindow).Close();
        }
        
        static void ConfigureRenderer()
        {
            s_Stages = new StageCache(dataProvider, currentContext);
            s_Comparator = new ComparisonGizmo(currentContext.layout.gizmoState, s_Displayer);
            s_Compositor = new Compositer(s_Displayer, currentContext, dataProvider, s_Stages);
            s_Displayer.OnClosed += () =>
            {
                s_Compositor?.Dispose();
                s_Compositor = null;

                SaveConfig();

                open = false;

                //free references for memory cleaning
                s_Displayer = null;
                s_Stages = null;
                s_Comparator = null;
                s_Compositor = null;
                //currentContext = null;
            };
            s_Displayer.OnChangingObjectInView += (go, index, localPos) =>
            {
                switch (index)
                {
                    case ViewCompositionIndex.First:
                    case ViewCompositionIndex.Second:
                        currentContext.GetViewContent((ViewIndex)index).contentPrefab = go;
                        PushSceneChangesToRenderer((ViewIndex)index);
                        break;
                    case ViewCompositionIndex.Composite:
                        ViewIndex viewIndex = s_Compositor.GetViewFromComposition(localPos);
                        currentContext.GetViewContent(viewIndex).contentPrefab = go;
                        PushSceneChangesToRenderer(viewIndex);
                        break;
                }
            };
        }

        public static void PushSceneChangesToRenderer(ViewIndex index)
        {
            s_Stages.UpdateScene(index);
            s_Displayer.Repaint();
        }
    }
}
