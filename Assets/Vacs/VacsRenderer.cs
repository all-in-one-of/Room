using UnityEngine;

namespace Vacs
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("Vacs/Renderer")]
    public sealed class VacsRenderer : MonoBehaviour
    {
        #region Exposed attributes

        [SerializeField] VacsData _data;
        [SerializeField, Range(0, 1)] float _dissolve;
        [SerializeField, Range(0, 1)] float _inflate;
        [SerializeField, Range(0, 1)] float _voxelize;
        [SerializeField, Range(0, 1)] float _jitter;
        [SerializeField, Range(0, 1)] float _digitize;

        public float dissolve { set { _dissolve = value; } }
        public float inflate  { set { _inflate  = value; } }
        public float voxelize { set { _voxelize = value; } }
        public float jitter   { set { _jitter   = value; } }
        public float digitize { set { _digitize = value; } }

        #endregion

        #region Hidden attributes

        [SerializeField, HideInInspector] ComputeShader _computeDissolve;
        [SerializeField, HideInInspector] ComputeShader _computeInflate;
        [SerializeField, HideInInspector] ComputeShader _computeVoxelize;
        [SerializeField, HideInInspector] ComputeShader _computeJitter;
        [SerializeField, HideInInspector] ComputeShader _computeDigitize;
        [SerializeField, HideInInspector] ComputeShader _computeReconstruct;

        #endregion

        #region Private objects

        ComputeBuffer _positionSource;
        ComputeBuffer _positionBuffer1;
        ComputeBuffer _positionBuffer2;

        ComputeBuffer _normalSource;
        ComputeBuffer _normalBuffer;

        ComputeBuffer _tangentSource;
        ComputeBuffer _tangentBuffer;

        MaterialPropertyBlock _drawProps;
        float _randomSeed;

        #endregion

        #region Internal properties and methods

        float globalTime {
            get { return Application.isPlaying ? Time.time : 10; }
        }

        void SetupBuffers()
        {
            if (_positionSource  == null) _positionSource  = _data.CreatePositionBuffer();
            if (_positionBuffer1 == null) _positionBuffer1 = _data.CreatePositionBuffer();
            if (_positionBuffer2 == null) _positionBuffer2 = _data.CreatePositionBuffer();

            if (_normalSource == null) _normalSource = _data.CreateNormalBuffer();
            if (_normalBuffer == null) _normalBuffer = _data.CreateNormalBuffer();

            if (_tangentSource == null) _tangentSource = _data.CreateTangentBuffer();
            if (_tangentBuffer == null) _tangentBuffer = _data.CreateTangentBuffer();
        }

        void ReleaseBuffers()
        {
            if (_positionSource  != null) _positionSource .Release();
            if (_positionBuffer1 != null) _positionBuffer1.Release();
            if (_positionBuffer2 != null) _positionBuffer2.Release();
            _positionSource = _positionBuffer1 = _positionBuffer2 = null;

            if (_normalSource != null) _normalSource.Release();
            if (_normalBuffer != null) _normalBuffer.Release();
            _normalSource = _normalBuffer = null;

            if (_tangentSource != null) _tangentSource.Release();
            if (_tangentBuffer != null) _tangentBuffer.Release();
            _tangentSource = _tangentBuffer = null;
        }

        void ApplyCompute(
            ComputeShader compute, int trianglePerThread, float amplitude,
            ComputeBuffer inputBuffer, ComputeBuffer outputBuffer)
        {
            var kernel = compute.FindKernel("Main");

            compute.SetBuffer(kernel, "PositionSource", _positionSource);
            compute.SetBuffer(kernel, "NormalSource", _normalSource);
            compute.SetBuffer(kernel, "TangentSource", _tangentSource);

            compute.SetBuffer(kernel, "PositionInput", inputBuffer);
            compute.SetBuffer(kernel, "PositionOutput", outputBuffer);

            compute.SetInt("TriangleCount", _data.triangleCount);
            compute.SetFloat("Amplitude", amplitude);
            compute.SetFloat("RandomSeed", _randomSeed);
            compute.SetFloat("Time", globalTime);

            var c = compute.GetKernelThreadGroupSizeX(kernel) * trianglePerThread;
            compute.Dispatch(kernel, (_data.triangleCount + c - 1) / c, 1, 1);
        }

        void UpdateVertices()
        {
            ApplyCompute(_computeDissolve, 1, _dissolve, _positionSource, _positionBuffer1);
            ApplyCompute(_computeInflate, 1, _inflate, _positionBuffer1, _positionBuffer2);
            ApplyCompute(_computeVoxelize, 1, _voxelize, _positionBuffer2, _positionBuffer1);
            ApplyCompute(_computeJitter, 1, _jitter, _positionBuffer1, _positionBuffer2);
            ApplyCompute(_computeDigitize, 2, _digitize, _positionBuffer2, _positionBuffer1);

            var compute = _computeReconstruct;
            var kernel = compute.FindKernel("Main");

            compute.SetBuffer(kernel, "PositionSource", _positionSource);
            compute.SetBuffer(kernel, "PositionModified", _positionBuffer1);

            compute.SetBuffer(kernel, "NormalInput", _normalSource);
            compute.SetBuffer(kernel, "NormalOutput", _normalBuffer);

            compute.SetBuffer(kernel, "TangentInput", _tangentSource);
            compute.SetBuffer(kernel, "TangentOutput", _tangentBuffer);

            compute.SetInt("TriangleCount", _data.triangleCount);

            var c = compute.GetKernelThreadGroupSizeX(kernel);
            compute.Dispatch(kernel, (_data.triangleCount + c - 1) / c, 1, 1);
        }

        #endregion

        #region External component handling

        void UpdateMeshFilter()
        {
            var meshFilter = GetComponent<MeshFilter>();

            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.hideFlags = HideFlags.NotEditable;
            }

            if (meshFilter.sharedMesh != _data.templateMesh)
                meshFilter.sharedMesh = _data.templateMesh;
        }

        void UpdateMeshRenderer()
        {
            var meshRenderer = GetComponent<MeshRenderer>();

            if (_drawProps == null)
                _drawProps = new MaterialPropertyBlock();

            _drawProps.SetBuffer("_OriginalPositionBuffer", _positionSource);
            _drawProps.SetBuffer("_OriginalNormalBuffer", _normalSource);
            _drawProps.SetBuffer("_PositionBuffer", _positionBuffer1);
            _drawProps.SetBuffer("_NormalBuffer", _normalBuffer);
            _drawProps.SetBuffer("_TangentBuffer", _tangentBuffer);
            _drawProps.SetFloat("_TriangleCount", _data.triangleCount);

            meshRenderer.SetPropertyBlock(_drawProps);
        }

        #endregion

        #region MonoBehaviour methods

        void OnDisable()
        {
            // In edit mode, we release the compute buffers OnDisable not
            // OnDestroy, because Unity spits out warnings before OnDestroy.
            // (OnDestroy is too late to prevent warning.)
            if (!Application.isPlaying) ReleaseBuffers();
        }

        void OnDestroy()
        {
            ReleaseBuffers();
        }

        void Start()
        {
            _randomSeed = Random.value;
        }

        void LateUpdate()
        {
            if (_data != null)
            {
                SetupBuffers();
                UpdateVertices();
                UpdateMeshFilter();
                UpdateMeshRenderer();
            }
        }

        #endregion
    }
}
