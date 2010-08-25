﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using PlatformEngine;
using NFSEngine;
using Microsoft.Xna.Framework;
using Carmageddon.Physics;
using Carmageddon.Parsers;

namespace Carmageddon.Gfx
{

    class CurrentSkid
    {
        public VehicleWheel Wheel;
        public float StartTime;
        public Vector3 StartPosition, EndPosition;
        public bool IsActive = true;
    }

    class SkidMarkBuffer
    {
        const float SKID_TIME = 0.2f;
        private int _maxSkids;

        VertexPositionTexture[] _particles;
        DynamicVertexBuffer _vertexBuffer;
        VertexDeclaration _vertexDeclaration;

        int _firstNewParticle;
        int _firstFreeParticle;
        Texture2D _texture, _defaultTexture;
        Vehicle _vehicle;

        private List<CurrentSkid> _currentSkids = new List<CurrentSkid>();

        public SkidMarkBuffer(Vehicle vehicle, int maxSkids)
        {
            _maxSkids = maxSkids;
            _vehicle = vehicle;
            _particles = new VertexPositionTexture[_maxSkids * 6];

            _vertexDeclaration = new VertexDeclaration(Engine.Device, VertexPositionTexture.VertexElements);

            // Create a dynamic vertex buffer.
            int size = VertexPositionTexture.SizeInBytes * _particles.Length;

            _vertexBuffer = new DynamicVertexBuffer(Engine.Device, size, BufferUsage.WriteOnly);

            MatFile matfile = new MatFile(GameVariables.BasePath + "Data\\Material\\skidmark.mat");
            matfile.Materials[0].ResolveTexture();
            _defaultTexture = _texture = matfile.Materials[0].Texture;
        }

        public void SetTexture(Texture2D texture)
        {
            if (_texture != texture)
            {
                _texture = texture;
                _firstFreeParticle = _firstNewParticle = 0;
            }
        }

        private void Update()
        {
            foreach (var wheel in _vehicle.Chassis.Wheels)
            {
                if (wheel.IsSkiddingLat || wheel.IsSkiddingLng)
                {
                    AddSkid(wheel, wheel.ContactPoint);
                }
            }
        

            for (int i = 0; i < _currentSkids.Count; i++)
            {
                CurrentSkid skid = _currentSkids[i];
                if (!skid.IsActive)
                {
                    _currentSkids.RemoveAt(i);
                    i--;
                    AddSkidToBuffer(skid);
                }
                else if (Helpers.HasTimePassed(SKID_TIME, _currentSkids[i].StartTime))
                {
                    AddSkidToBuffer(skid);
                    skid.StartPosition = skid.EndPosition;
                    skid.StartTime = Engine.TotalSeconds;
                }
            }

            int nbrTempSkids = 0;
            for (int i = 0; i < _currentSkids.Count; i++)
            {
                if (_currentSkids[i].IsActive && !Helpers.HasTimePassed(SKID_TIME, _currentSkids[i].StartTime))
                {
                    //temp
                    _currentSkids[i].EndPosition = _currentSkids[i].Wheel.ContactPoint;
                    AddSkidToBuffer(_currentSkids[i]);
                    nbrTempSkids++;
                }
            }

            AddNewParticlesToVertexBuffer();

            _firstFreeParticle -= 6 * nbrTempSkids;
            if (_firstFreeParticle < 0)
                _firstFreeParticle = _particles.Length - (-_firstFreeParticle);
            _firstNewParticle = _firstFreeParticle;
        }


        public void Render()
        {
            Update();
            GraphicsDevice device = Engine.Device;

            device.Vertices[0].SetSource(_vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
            device.VertexDeclaration = _vertexDeclaration;

            GameVariables.CurrentEffect.CurrentTechnique.Passes[0].Begin();

            device.Textures[0] = _texture ?? _defaultTexture;
            device.RenderState.DepthBias = -0.00002f;
            CullMode oldCullMode = Engine.Device.RenderState.CullMode;
            device.RenderState.CullMode = CullMode.None;
           
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, _maxSkids * 2);
            GameVariables.CurrentEffect.CurrentTechnique.Passes[0].End();

            device.RenderState.CullMode = oldCullMode;

            //de-activate all skids before next update
            foreach (CurrentSkid skid in _currentSkids)
                skid.IsActive = false;

            device.RenderState.DepthBias = 0;
        }


        void AddNewParticlesToVertexBuffer()
        {
            int stride = VertexPositionTexture.SizeInBytes;


            if (_firstNewParticle < _firstFreeParticle)
            {
                // If the new particles are all in one consecutive range,
                // we can upload them all in a single call.
                _vertexBuffer.SetData(_firstNewParticle * stride, _particles,
                                     _firstNewParticle,
                                     _firstFreeParticle - _firstNewParticle,
                                     stride, SetDataOptions.NoOverwrite);
            }
            else
            {
                // If the new particle range wraps past the end of the queue
                // back to the start, we must split them over two upload calls.
                _vertexBuffer.SetData(_firstNewParticle * stride, _particles,
                                     _firstNewParticle,
                                     _particles.Length - _firstNewParticle,
                                     stride, SetDataOptions.NoOverwrite);

                if (_firstFreeParticle > 0)
                {
                    _vertexBuffer.SetData(0, _particles,
                                         0, _firstFreeParticle,
                                         stride, SetDataOptions.NoOverwrite);
                }
            }

            // Move the particles we just uploaded from the new to the active queue.
            _firstNewParticle = _firstFreeParticle;
        }


        #region Public Methods

        public void AddSkid(VehicleWheel wheel, Vector3 pos)
        {
            CurrentSkid skid = null;
            foreach (CurrentSkid cskid in _currentSkids)
            {
                if (cskid.Wheel == wheel)
                {
                    skid = cskid; break;
                }
            }
            if (skid == null)
            {
                skid = new CurrentSkid { Wheel = wheel, StartPosition = pos, EndPosition = pos, StartTime = Engine.TotalSeconds };
                _currentSkids.Add(skid);
            }
            else
            {
                skid.IsActive = true;
                skid.EndPosition = pos;
            }
        }

        private void AddSkidToBuffer(CurrentSkid skid)
        {
            int p1, p2;
            

            float thickness = 0.15f;

            Vector3 direction = skid.EndPosition - skid.StartPosition;
            direction.Normalize();

            Vector3 normal = Vector3.Cross(direction, Vector3.UnitY);
            normal.Normalize();

            _particles[_firstFreeParticle].Position = skid.StartPosition - normal * thickness;
            _particles[_firstFreeParticle].TextureCoordinate = new Vector2(0, 1);

            _firstFreeParticle++;
            p1 = _firstFreeParticle;
            _particles[_firstFreeParticle].Position = skid.EndPosition - normal * thickness;
            _particles[_firstFreeParticle].TextureCoordinate = new Vector2(3, 1);

            _firstFreeParticle++;
            p2 = _firstFreeParticle;
            _particles[_firstFreeParticle].Position = skid.StartPosition + normal * thickness;
            _particles[_firstFreeParticle].TextureCoordinate = Vector2.Zero;
            

            _firstFreeParticle++;
            _particles[_firstFreeParticle] = _particles[p2];
            _firstFreeParticle++;
            _particles[_firstFreeParticle] = _particles[p1];
            _firstFreeParticle++;
            _particles[_firstFreeParticle].Position = skid.EndPosition + normal * thickness;
            _particles[_firstFreeParticle].TextureCoordinate = new Vector2(3, 0);

            _firstFreeParticle = (_firstFreeParticle + 1) % _particles.Length;
        }

        
        #endregion

        internal void Reset()
        {
            _currentSkids.Clear();
        }
    }
}
