﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Particle3DSample;

namespace Carmageddon.Gfx
{
    class TyreSmokeParticleSystem : ParticleSystem
    {
        
        static TyreSmokeParticleSystem _instance;
        public static TyreSmokeParticleSystem Instance
        {
            get
            {
                if (_instance == null) _instance = new TyreSmokeParticleSystem();
                return _instance;
            }
        }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "Content/smoke";

            settings.MaxParticles = 100;

            settings.Duration = TimeSpan.FromSeconds(1);

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 2;

            settings.MinVerticalVelocity = 2;
            settings.MaxVerticalVelocity = 5;

            settings.Gravity = new Vector3(0, -5, 0);

            settings.EndVelocity = 0.75f;

            settings.MinStartSize = 5;
            settings.MaxStartSize = 7;

            settings.MinEndSize = 10;
            settings.MaxEndSize = 30;
        }
    }
}