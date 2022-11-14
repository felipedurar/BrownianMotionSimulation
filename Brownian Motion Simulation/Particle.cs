using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brownian_Motion_Simulation
{
    public class Particle
    {
        public int Id { get; set; } = 0;

        public PointF Position { get; set; } = new PointF(10, 10);
        public PointF Velocity { get; set; } = new PointF(5, 5);

        public float Mass { get; set; } = 10;
        public float Radius { get; set; } = 10;
        public Color Color { get; set; } = Color.White;

        public List<PointF> Path { get; set; } = new List<PointF>();

        public List<int> CollisionWithIds = new List<int>();

        public void ApplyForce(float divider = 1.0f, float deltaTime = 1.0f)
        {
            Position = new PointF((Position.X + (Velocity.X / divider)) * deltaTime, (Position.Y + (Velocity.Y / divider) * deltaTime));
        }

        public Particle() { }
        public Particle(PointF position)
        {
            Position = position;
        }
    }
}
