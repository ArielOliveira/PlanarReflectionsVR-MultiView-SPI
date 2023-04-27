////////////////////////////////////////////////////////////////////////////////////////
//                                                                                    //
// Planar Reflections Probe for Unity                                                 //
//                                                                                    //
// Author: Ariel Corrêa                                                               //
// Date: Apil 27, 2023                                                                //
// Last Update: April 27, 2023                                                        //
// Email: ariel.oliveira01@gmail.com                                                  //
// Repository: https://github.com/ArielOliveira/PlanarReflectionsVR-MultiView-SPI.git //
//                                                                                    //
////////////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class PlanarReflections : MonoBehaviour {
    public LayerMask renderMask;
    private Camera leftCamera;
    private Camera rightCamera;
    private RenderTexture leftTexture;
    private RenderTexture rightTexture;
    public Transform normal;
    public float clipPlaneOffset;
    [Range(0,1)] public float scale = 1f;
    
    private readonly int leftCameraProperty = Shader.PropertyToID("_LeftReflCameraTex");
    private readonly int rightCameraProperty = Shader.PropertyToID("_RightReflCameraTex");

    private void OnEnable() {
        RenderPipelineManager.beginCameraRendering += ExecutePlanarReflections;
    }
    
    private void OnDisable() {
        CleanUp();
    }

    private void OnDestroy() {
        CleanUp();
    }

    private void CleanUp() {
        RenderPipelineManager.beginCameraRendering -= ExecutePlanarReflections;

        if (leftCamera != null) {
            leftCamera.targetTexture = null;
            SafeDestroy(leftCamera.gameObject);
        }

        if (rightCamera != null) {
            rightCamera.targetTexture = null;
            SafeDestroy(rightCamera.gameObject);
        }

        if (leftTexture != null)
            RenderTexture.ReleaseTemporary(leftTexture);

        if (rightTexture != null)    
            RenderTexture.ReleaseTemporary(rightTexture);
    }

    private static void SafeDestroy(Object obj) {
        #if UNITY_EDITOR
            if (!Application.isPlaying) 
                DestroyImmediate(obj);
            else
                Destroy(obj);
        #else
            Destroy(obj);
        #endif    
    }

    private void ExecutePlanarReflections(ScriptableRenderContext context, Camera camera) {
        if (normal == null) return;

        switch(camera.cameraType) {
            case CameraType.Game:
            case CameraType.SceneView:
            case CameraType.VR:
                if (leftCamera == null) {
                        leftCamera = CreateCamera(camera);
                        PlanarReflectionTexture(camera, leftCamera, ref leftTexture);
                    }

                if (XRSettings.enabled) {
                    if (rightCamera == null) {
                        rightCamera = CreateCamera(camera);
                        PlanarReflectionTexture(camera, rightCamera, ref rightTexture);
                    }

                    VRPlanarReflections(context, camera);
                } else { 
                    RegularPlanarReflections(context, camera);
                }
                break;
        }
    }

    private void VRPlanarReflections(ScriptableRenderContext context, Camera camera) {
        if (leftCamera == null || rightCamera == null) return;

        SetupReflectionCamera(leftCamera, camera, Camera.StereoscopicEye.Left);
        SetupReflectionCamera(rightCamera, camera, Camera.StereoscopicEye.Right);

        GL.invertCulling = true;
        UniversalRenderPipeline.RenderSingleCamera(context, leftCamera);
        UniversalRenderPipeline.RenderSingleCamera(context, rightCamera);
        GL.invertCulling = false;

        Shader.SetGlobalTexture(leftCameraProperty, leftCamera.targetTexture);
        Shader.SetGlobalTexture(rightCameraProperty, rightCamera.targetTexture);
    }
    private void RegularPlanarReflections(ScriptableRenderContext context, Camera camera) {
        if (leftCamera == null) return;

        SetupReflectionCamera(leftCamera, camera);

        GL.invertCulling = true;
        UniversalRenderPipeline.RenderSingleCamera(context, leftCamera);
        GL.invertCulling = false;

        Shader.SetGlobalTexture(leftCameraProperty, leftCamera.targetTexture);
    }

    private void PerformPlanarReflections(ScriptableRenderContext context, Camera src, Camera dst) {
        if (src == null || dst == null) return;

        SetupReflectionCamera(src, dst);
    }

    private void SetupReflectionCamera(Camera reflectionCamera, Camera renderingCamera, Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left) {
        Vector3 pos = transform.position;

        Vector3 normal = this.normal.forward;

        float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;

        Vector4 reflPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.zero;

        CalculateReflectionMatrix(ref reflection, reflPlane);
        
        Matrix4x4 m = renderingCamera.worldToCameraMatrix * reflection;
   
        Matrix4x4 projectionMatrix = Matrix4x4.identity;

        if (XRSettings.enabled) {
            float eyeSeparation = renderingCamera.stereoSeparation * 2f;

            switch(eye) {
                case Camera.StereoscopicEye.Left: m[12] += eyeSeparation; break;
                case Camera.StereoscopicEye.Right: m[12] -= eyeSeparation; break;
            }

            projectionMatrix = renderingCamera.GetStereoProjectionMatrix(eye); 
        } else {
            projectionMatrix = renderingCamera.projectionMatrix;
        }

        reflectionCamera.worldToCameraMatrix = m;
        reflectionCamera.projectionMatrix = projectionMatrix;
    }


    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane){

        reflectionMat.m00 = (1.0f - 2 * plane[0] * plane[0]);

        reflectionMat.m01 = (-2 * plane[0] * plane[1]);

        reflectionMat.m02 = (-2 * plane[0] * plane[2]);

        reflectionMat.m03 = (-2 * plane[3] * plane[0]);

        reflectionMat.m10 = (-2 * plane[1] * plane[0]);

        reflectionMat.m11 = (1.0f - 2 * plane[1] * plane[1]);

        reflectionMat.m12 = (-2 * plane[1] * plane[2]);

        reflectionMat.m13 = (-2 * plane[3] * plane[1]);

        reflectionMat.m20 = (-2 * plane[2] * plane[0]);

        reflectionMat.m21 = (-2 * plane[2] * plane[1]);

        reflectionMat.m22 = (1.0f - 2 * plane[2] * plane[2]);

        reflectionMat.m23 = (-2 * plane[3] * plane[2]);

        reflectionMat.m30 = 0.0f;

        reflectionMat.m31 = 0.0f;

        reflectionMat.m32 = 0.0f;

        reflectionMat.m33 = 1.0f;
    }

     // Extended sign: returns -1, 0 or 1 based on sign of a
    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }

    private static void MakeProjectionMatrixOblique(ref Matrix4x4 matrix, Vector4 clipPlane)
        {
            Vector4 q;

            // Calculate the clip-space corner point opposite the clipping plane
            // as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
            // transform it into camera space by multiplying it
            // by the inverse of the projection matrix

            q.x = (sgn(clipPlane.x) + matrix[8]) / matrix[0];
            q.y = (sgn(clipPlane.y) + matrix[9]) / matrix[5];
            q.z = -1.0F;
            q.w = (1.0F + matrix[10]) / matrix[14];

            // Calculate the scaled plane vector
            Vector4 c = clipPlane * (2.0F / Vector3.Dot(clipPlane, q));

            // Replace the third row of the projection matrix
            matrix[2] = c.x;
            matrix[6] = c.y;
            matrix[10] = c.z + 1.0F;
            matrix[14] = c.w;
        }

    private Camera CreateCamera(Camera source) {
        GameObject go = new GameObject("", typeof(Camera));

        var camData = go.AddComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;

        camData.requiresColorOption = CameraOverrideOption.Off;
        camData.requiresDepthOption = CameraOverrideOption.Off;
        camData.SetRenderer(0);
        
        Camera cam = go.GetComponent<Camera>();
        cam.CopyFrom(source);
        cam.cullingMask = renderMask;
        cam.enabled = false;
        go.hideFlags = HideFlags.HideAndDontSave;

        return cam;
    }

    private void PlanarReflectionTexture(Camera src, Camera dst, ref RenderTexture _reflectionTexture) {
        if (_reflectionTexture == null) {
            float renderScale = UniversalRenderPipeline.asset.renderScale;
            Vector2Int res = new Vector2Int((int)(src.pixelWidth * scale * renderScale), (int)(src.pixelHeight * scale * renderScale));
            const bool useHdr10 = true;
            const RenderTextureFormat hdrFormat = useHdr10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
            _reflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 16, hdrFormat);    
            }
            
            dst.targetTexture = _reflectionTexture;
    }
}
