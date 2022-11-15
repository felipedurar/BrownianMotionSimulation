using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Brownian_Motion_Simulation
{
    public partial class Form1 : Form
    {
        public List<Particle> Particles { get; set; } = new List<Particle>();
        public List<Particle> DustParticles { get; set; } = new List<Particle>();
        public Thread BehaviorThread { get; set; }
        public bool BehaviorSwitch { get; set; } = true;

        public int CollisionPasses = 10;

        public Random random { get; set; } = new Random();

        public long CycleCounter = 0;
        public long LastCycleTime = 0;
        public long LastDeltaTime = 0;

        public const int FluidParticlesAmount = 100;
        public const int DustParticlesAmount = 2;

        // Every 20 passes record the position
        public const int RecorderMultiplier = 15;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            BehaviorThread = new Thread(BehaviorProc);

            InitParticles();

            DateTime now = DateTime.UtcNow;
            LastCycleTime = new DateTimeOffset(now).ToUnixTimeMilliseconds();

            BehaviorThread.Start();
        }

        private void InitParticles()
        {
            for (int c = 0; c < FluidParticlesAmount; c++)
                GenerateRandomFluidParticle();
            for (int c = 0; c < DustParticlesAmount; c++)
                GenerateRandomDustParticle();
        }

        private void GenerateRandomFluidParticle()
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0d);
            float speed = random.Next(2, 10);

            Particle particle = new Particle();
            particle.Radius = 5; // random.Next(3, 5);
            particle.Position = new PointF((float)(random.NextDouble() * (Width - (particle.Radius * 2))), (float)(random.NextDouble() * (Width - (particle.Radius * 2))));
            particle.Velocity = new PointF((float)(Math.Sin(angle) * speed), (float)(Math.Cos(angle) * speed));
            particle.Color = Color.Yellow; // Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
            particle.Mass = particle.Radius;

            particle.Id = Particles.Count;
            Particles.Add(particle);
        }

        private void GenerateRandomDustParticle()
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2.0d);
            float speed = random.Next(2, 3);

            Particle particle = new Particle();
            particle.Radius = random.Next(20, 25);
            particle.Position = new PointF((float)(random.NextDouble() * (Width - (particle.Radius * 2))), (float)(random.NextDouble() * (Width - (particle.Radius * 2))));
            particle.Velocity = new PointF((float)(Math.Sin(angle) * speed), (float)(Math.Cos(angle) * speed));
            particle.Color = Color.FromArgb(random.Next(50, 150), random.Next(50, 150), random.Next(50, 150));
            particle.Mass = particle.Radius * 10;

            particle.Id = Particles.Count;
            Particles.Add(particle);
            DustParticles.Add(particle);
        }

        private void BehaviorProc()
        {
            while (BehaviorSwitch)
            {
                for (int c = 0; c < CollisionPasses; c++)
                {
                    // TODO: Use this deltatime for relative movement of particles
                    DateTime now = DateTime.UtcNow;
                    long cTime = new DateTimeOffset(now).ToUnixTimeMilliseconds();
                    LastDeltaTime = cTime - LastCycleTime;
                    LastCycleTime = cTime;

                    // Apply force for every particle
                    foreach (Particle particle in Particles)
                        particle.ApplyForce(CollisionPasses * 1.0f);

                    // Check for collision
                    foreach (Particle particleA in Particles)
                    {
                        foreach (Particle particleB in Particles)
                        {
                            // Ignore if the same
                            if (particleA.Id == particleB.Id) continue;

                            // Collision Check
                            double distance = modulusOfVector(particleB.Position.X - particleA.Position.X, particleB.Position.Y - particleA.Position.Y);
                            if (distance <= (particleA.Radius + particleB.Radius))
                            {
                                //Thread.Sleep(1000);

                                // If the collision was already calculated just ignore this step
                                if (particleA.CollisionWithIds.Contains(particleB.Id))
                                    continue;

                                // Elastic Collision Calc
                                // Calc Result Velocities
                                PointF velResParticleA = calcResultantVelocityOfCollision(particleA.Position, particleB.Position, 
                                    particleA.Velocity, particleB.Velocity, particleA.Mass, particleB.Mass);
                                PointF velResParticleB = calcResultantVelocityOfCollision(particleB.Position, particleA.Position,
                                    particleB.Velocity, particleA.Velocity, particleB.Mass, particleA.Mass);
                                particleA.Velocity = velResParticleA;
                                particleB.Velocity = velResParticleB;

                                particleA.CollisionWithIds.Add(particleB.Id);
                                particleB.CollisionWithIds.Add(particleA.Id);
                            } 
                            else
                            {
                                // Stopped colliding, so remove the collision relationship
                                if (particleA.CollisionWithIds.Contains(particleB.Id))
                                {
                                    particleA.CollisionWithIds.Remove(particleB.Id);
                                    particleB.CollisionWithIds.Remove(particleA.Id);
                                }

                            }

                        }
                    }

                    // Check for corner collision
                    foreach (Particle particle in Particles)
                    {
                        if (particle.Position.X - particle.Radius < 0)
                            particle.Velocity = new PointF(particle.Velocity.X < 0.0f ? particle.Velocity.X * -1.0f : particle.Velocity.X, particle.Velocity.Y);
                        if (particle.Position.Y - particle.Radius < 0)
                            particle.Velocity = new PointF(particle.Velocity.X, particle.Velocity.Y < 0.0f ? particle.Velocity.Y * -1.0f : particle.Velocity.Y);

                        if (particle.Position.X + (particle.Radius * 2) > Width)
                            particle.Velocity = new PointF(particle.Velocity.X > 0.0f ? particle.Velocity.X * -1.0f : particle.Velocity.X, particle.Velocity.Y);
                        if (particle.Position.Y + (particle.Radius * 2) > (Height - 25))
                            particle.Velocity = new PointF(particle.Velocity.X, particle.Velocity.Y > 0.0f ? particle.Velocity.Y * -1.0f : particle.Velocity.Y);
                    }

                }

                CycleCounter++;

                // Record the Position
                if (CycleCounter % RecorderMultiplier == 0)
                {
                    foreach (Particle cParticle in DustParticles)
                        cParticle.Path.Add(new PointF(cParticle.Position.X, cParticle.Position.Y));
                }

                //Thread.Sleep(10);
            }
        }

        private double modulusOfVector(double x, double y)
            => Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

        private double calcVectorAngle(double x, double y)
            => Math.Atan2(y, x);

        private float calcXVelocityResultOfCollision(double v1, double v2, double m1, double m2, double theta1, double theta2, double phy)
            => (float)((((v1 * Math.Cos(theta1 - phy) * (m1 - m2)) + (2 * m2 * v2 * Math.Cos(theta2 - phy))) * Math.Cos(phy) / (m1 + m2)) + 
            ((v1 * Math.Sin(theta1 - phy) * Math.Cos(phy + (Math.PI / 2)))));

        private float calcYVelocityResultOfCollision(double v1, double v2, double m1, double m2, double theta1, double theta2, double phy)
            => (float)((((v1 * Math.Cos(theta1 - phy) * (m1 - m2)) + (2 * m2 * v2 * Math.Cos(theta2 - phy))) * Math.Sin(phy) / (m1 + m2)) +
            ((v1 * Math.Sin(theta1 - phy) * Math.Sin(phy + (Math.PI / 2)))));

        private PointF calcResultantVelocityOfCollision(PointF pos1, PointF pos2, PointF vel1, PointF vel2, float mass1, float mass2)
        {
            double v1 = modulusOfVector(vel1.X, vel1.Y);
            double v2 = modulusOfVector(vel2.X, vel2.Y);
            double theta1 = calcVectorAngle(vel1.X, vel1.Y);
            double theta2 = calcVectorAngle(vel2.X, vel2.Y);
            double phy = angleBetweenTwoPoints(pos1, pos2);
            float xResultVelocity = calcXVelocityResultOfCollision(v1, v2, mass1, mass2, theta1, theta2, phy);
            float yResultVelocity = calcYVelocityResultOfCollision(v1, v2, mass1, mass2, theta1, theta2, phy);
            return new PointF(xResultVelocity, yResultVelocity);
        }

        private double angleBetweenTwoPoints(PointF p1, PointF p2)
            => calcVectorAngle(p2.X - p1.X, p2.Y - p1.Y);


        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            // Draw Dust Particle Paths
            foreach (Particle particle in DustParticles)
            {
                PointF lastPosition = new PointF();
                for (int i = 0; i < particle.Path.Count; i++)
                {
                    // For Initialization
                    if (i == 0)
                    {
                        lastPosition = particle.Path[i];
                        continue;
                    }

                    // Draw Line Segment
                    e.Graphics.DrawLine(new Pen(new SolidBrush(Color.Red)), lastPosition, particle.Path[i]);
                    lastPosition = particle.Path[i];
                }
            }

            // Draw Every Particle
            Font massFont = new Font("Arial", 10, FontStyle.Bold);
            foreach (Particle particle in Particles)
            {
                e.Graphics.FillPie(new SolidBrush(particle.Color), new Rectangle((int)particle.Position.X - (int)particle.Radius, (int)particle.Position.Y - (int)particle.Radius,
                    (int)(particle.Radius * 2), (int)(particle.Radius * 2)), 0, 360);

                if (particle.Mass > 15)
                {
                    string massToShow = (particle.Mass * 10).ToString();
                    SizeF txtSz = e.Graphics.MeasureString(massToShow, massFont);
                    e.Graphics.DrawString(massToShow, massFont, new SolidBrush(Color.White), 
                        particle.Position.X - (txtSz.Width / 2), particle.Position.Y - (txtSz.Height / 2));
                }
                    
            }

            // Draw Console
            Font domainDetailsFont = new Font("Arial", 8);
            int tmpY = 5;
            const int textSpacing = 12;
            e.Graphics.DrawString("Domain Details:", domainDetailsFont, new SolidBrush(Color.White), 5, tmpY); tmpY += textSpacing;
            e.Graphics.DrawString("- Fluid: " + FluidParticlesAmount + " x O2 Molecules", domainDetailsFont, new SolidBrush(Color.White), 5, tmpY); tmpY += textSpacing;
            e.Graphics.DrawString("- Dust: " + DustParticlesAmount + " Particles", domainDetailsFont, new SolidBrush(Color.White), 5, tmpY); tmpY += textSpacing;
            e.Graphics.DrawString("- Collision Subpasses: " + CollisionPasses, domainDetailsFont, new SolidBrush(Color.White), 5, tmpY); tmpY += textSpacing;
            e.Graphics.DrawString("- Cycle: " + CycleCounter + " - L. Delta Time: " + LastDeltaTime + "ms", domainDetailsFont, new SolidBrush(Color.White), 5, tmpY); tmpY += textSpacing;

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            BehaviorThread.Abort();
        }
    }
}
