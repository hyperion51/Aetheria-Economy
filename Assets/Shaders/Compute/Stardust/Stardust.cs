﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;


public class Stardust : MonoBehaviour
{
    #region variables
    //public bool debugLog = false;
    //public float proxyPersistTime = 2;
    public GameSettings Settings;
    public Camera TargetCamera;
    public ComputeShader ParticleCalculation;
    public Material ParticleMaterial;
    public int Span = 512;
    public float Speed = .25f;

    public float MinimumSize = .1f;
    public float MaximumSize = 2;

    public float Spacing = 4.0f;
    public float Ceiling = -250.0f;
    public float Floor = -25.0f;
    public float HeightExponent = 4;

    public RenderTexture NebulaSurfaceHeight;
    public RenderTexture NebulaPatchHeight;
    public RenderTexture NebulaPatch;
    public RenderTexture NebulaTint;
    public Texture Heightmap;
    public Texture2D ParticleColors;
    //public Transform TargetTransform;
    public Camera GravityCamera;

    private float _flowScroll;
    private const int GROUP_SIZE = 128;
    private int _updateParticlesKernel;
    #endregion

    #region Structs
    //Notice that this struct has to match the one in the compute shader exactly.
    struct Particle
    {
        public Vector3 Position;
        public Vector3 Color; //Color is a float3 in the compute shader, we need a Vector3 to match that layout, not a Color!
        public float Size;
    };
    #endregion

    #region buffers
    private ComputeBuffer _particlesBuffer;
    private const int PARTICLE_STRIDE = 28;

    private ComputeBuffer _quadPoints;
    private const int QUAD_STRIDE = 12;

    #endregion

    #region setup
    // Use this for initialization
    void Start()
    {
        //Find compute kernel
        _updateParticlesKernel = ParticleCalculation.FindKernel("UpdateParticles");

        //Create particle buffer
        _particlesBuffer = new ComputeBuffer(Span * Span, PARTICLE_STRIDE);

        Particle[] particles = new Particle[Span * Span];

        for (int i = 0; i < Span * Span; ++i)
        {
            particles[i].Position = Random.insideUnitSphere * 100;
            particles[i].Color = Vector3.one; //white
            particles[i].Size = Random.value;
        }

        _particlesBuffer.SetData(particles);

        //Create quad buffer
        _quadPoints = new ComputeBuffer(6, QUAD_STRIDE);

        _quadPoints.SetData(new[] {
			new Vector3(-.5f, .5f),
			new Vector3(.5f, .5f),
			new Vector3(.5f, -.5f),
			new Vector3(.5f, -.5f),
			new Vector3(-.5f, -.5f),
			new Vector3(-.5f, .5f)
		});
    }
    #endregion

    #region Compute update
    // Update is called once per frame
    void Update()
    {
        //Bind resources to compute shader
        ParticleCalculation.SetBuffer(_updateParticlesKernel, "particles", _particlesBuffer);
//        ParticleCalculation.SetBuffer(_updateParticlesKernel, "temperature", _temperature);

        ParticleCalculation.SetFloat("time", Time.time * Speed);

        var pos = GravityCamera.transform.position;
        ParticleCalculation.SetVector("_GridTransform", new Vector4(pos.x,pos.z,GravityCamera.orthographicSize*2));
        ParticleCalculation.SetFloat("spacing", Spacing);
        ParticleCalculation.SetFloat("ceilingHeight", Ceiling);
        ParticleCalculation.SetFloat("floorHeight", Floor);
        ParticleCalculation.SetFloat("heightExponent", HeightExponent);
        ParticleCalculation.SetFloat("maximumSize", MaximumSize);
        ParticleCalculation.SetFloat("minimumSize", MinimumSize);
        ParticleCalculation.SetInt("span", Span);
        
        ParticleCalculation.SetTexture(_updateParticlesKernel, "Heightmap", Heightmap);
        ParticleCalculation.SetTexture(_updateParticlesKernel, "HueTexture", ParticleColors);
        
        ParticleCalculation.SetTexture(_updateParticlesKernel, "_NebulaSurfaceHeight", NebulaSurfaceHeight);
        ParticleCalculation.SetTexture(_updateParticlesKernel, "_NebulaPatchHeight", NebulaPatchHeight);
        ParticleCalculation.SetTexture(_updateParticlesKernel, "_NebulaPatch", NebulaPatch);
        ParticleCalculation.SetTexture(_updateParticlesKernel, "_NebulaTint", NebulaTint);
        
        ParticleCalculation.SetFloat("_NebulaFillDensity", Settings.DefaultEnvironment.Nebula.FillDensity);
        ParticleCalculation.SetFloat("_SafetyDistance", Settings.DefaultEnvironment.Nebula.FillDistance);
        ParticleCalculation.SetFloat("_NebulaFloorDensity", Settings.DefaultEnvironment.Nebula.FloorDensity);
        ParticleCalculation.SetFloat("_NebulaPatchDensity", Settings.DefaultEnvironment.Nebula.PatchDensity);
        ParticleCalculation.SetFloat("_NebulaFloorOffset", Settings.DefaultEnvironment.Nebula.FloorOffset);
        ParticleCalculation.SetFloat("_NebulaFloorBlend", Settings.DefaultEnvironment.Nebula.FloorBlend);
        ParticleCalculation.SetFloat("_NebulaPatchBlend", Settings.DefaultEnvironment.Nebula.PatchBlend);
        // Shader.SetGlobalFloat("_TintExponent", Settings.DefaultEnvironment.);
        ParticleCalculation.SetFloat("_NoiseScale", Settings.DefaultEnvironment.Noise.Scale);
        ParticleCalculation.SetFloat("_NoiseExponent", Settings.DefaultEnvironment.Noise.Exponent);
        ParticleCalculation.SetFloat("_NoiseAmplitude", Settings.DefaultEnvironment.Noise.Amplitude);
        ParticleCalculation.SetFloat("_NoiseSpeed", Settings.DefaultEnvironment.Noise.Speed);
        ParticleCalculation.SetFloat("_FlowScale", Settings.DefaultEnvironment.Flow.Scale);
        ParticleCalculation.SetFloat("_FlowAmplitude", Settings.DefaultEnvironment.Flow.Amplitude);
        _flowScroll += Settings.DefaultEnvironment.Flow.ScrollSpeed * Time.deltaTime;
        ParticleCalculation.SetFloat("_FlowScroll", _flowScroll);
        ParticleCalculation.SetFloat("_FlowSpeed", Settings.DefaultEnvironment.Flow.Speed);

        //Dispatch, launch threads on GPU
        int numberOfGroups = Mathf.CeilToInt((float)Span * Span / GROUP_SIZE);
        ParticleCalculation.Dispatch(_updateParticlesKernel, numberOfGroups, 1, 1);
        
        ParticleMaterial.SetBuffer("particles", _particlesBuffer);
        ParticleMaterial.SetBuffer("quadPoints", _quadPoints);
        Graphics.DrawProcedural(ParticleMaterial, new Bounds(pos, new Vector3(Spacing * Span, 2048, Spacing * Span)), MeshTopology.Triangles, 6, Span * Span, TargetCamera, null, ShadowCastingMode.Off, false, 0);
    }
    #endregion

    #region rendering
    // void OnRenderObject()
    // {
    //     if (Camera.current == TargetCamera || Camera.current.name == "SceneCamera")
    //     {
    //         //Bind resources to material
    //         ParticleMaterial.SetBuffer("particles", _particlesBuffer);
    //         ParticleMaterial.SetBuffer("quadPoints", _quadPoints);
    //         // ParticleMaterial.SetFloat("velocityStretch", ParticleVelocityStretch);
    //         // ParticleMaterial.SetVector("velocity", PlayerVelocity);
    //         
    //         //Set the pass
    //         ParticleMaterial.SetPass(0);
    //
    //         //Draw
    //         Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, Span * Span);
    //     }
    // }
    #endregion

    #region cleanup
    void OnDestroy()
    {
//        m_emitterProxies.Release();
//        _temperature.Release();
        if(_particlesBuffer!=null)
        {
            _particlesBuffer.Release();
            _quadPoints.Release();
        }
//        m_livingProxyIndices.Release();
    }
    #endregion
}
