namespace UnityEngine.Rendering.LookDev
{
    public interface IDataProvider
    {
        CustomRenderSettings GetEnvironmentSetup();
        void SetupCamera(Camera camera);
    }

    public struct CustomRenderSettings
    {
        public DefaultReflectionMode defaultReflectionMode;
        public Cubemap customReflection;
        public Material skybox;
        public AmbientMode ambientMode;
    }

    /// <summary>Runtime link to reflect some Stage functionality for SRP editing</summary>
    public class StageRuntimeInterface
    {
        System.Func<bool, GameObject> m_AddGameObject;

        System.Func<Camera> m_GetCamera;

        public StageRuntimeInterface(System.Func<bool, GameObject> AddGameObject, System.Func<Camera> GetCamera)
        {
            m_AddGameObject = AddGameObject;
            m_GetCamera = GetCamera;
        }

        /// <summary>Create a gameObject in the stage</summary>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <returns></returns>
        public GameObject AddGameObject(bool persistent = false)
            => m_AddGameObject?.Invoke(persistent);

        /// <summary>Get the camera used in the stage</summary>
        public Camera camera => m_GetCamera?.Invoke();

        /// <summary>Custom data pointer for convenience</summary>
        public object SRPData;
    }
}
