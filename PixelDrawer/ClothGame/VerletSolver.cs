using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Xna.Framework;

namespace PixelDrawer
{
    public class VerletSolver
    {
        public Vector3 Gravity {  get; set; } = new Vector3 (0, 10f, 0);
        public float Floor { get; set; } = 250f;
        public int SimulationPasses { get; set; } = 8;

        public ReadOnlyCollection<VerletBody> Bodies { get; }

        private readonly List<VerletBody> _bodies;
        private List<LineConstraint> _lineConstraints;

        public VerletSolver(int initialBodiesCount = 100)
        {
            _bodies = new List<VerletBody>(initialBodiesCount);
            Bodies = _bodies.AsReadOnly();
        }

        public void AddBody(VerletBody body)
        {
            _bodies.Add(body);
        }
        public void RemoveBody(VerletBody other)
        {
            _bodies.Remove(other);
        }

        public void AddLineConstraint(LineConstraint lineConstraint)
        {
            _lineConstraints ??= new List<LineConstraint>();
            _lineConstraints.Add(lineConstraint);
        }
        public void RemoveLineConstraint(LineConstraint lineConstraint)
        {
            _lineConstraints?.Remove(lineConstraint);
        }

        public void Update(GameTime gameTime)
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body.IsStatic)
                    continue;

                var velocity = body.CurrentPosition - body.LastPosition;
                body.LastPosition = body.CurrentPosition;
                body.CurrentPosition += velocity + Gravity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            // Apply constraints.
            for (int i = 0; i <  SimulationPasses; i++)
            {
                // Line.
                foreach (var lineConstraint in _lineConstraints)
                {
                    if (lineConstraint.Body1.IsStatic
                        && lineConstraint.Body2.IsStatic)
                        continue;

                    var direction = lineConstraint.Body1.CurrentPosition - lineConstraint.Body2.CurrentPosition;
                    var distance = direction.Length();
                    var error = lineConstraint.Length - distance;

                    var power = lineConstraint.Body1.IsStatic || lineConstraint.Body2.IsStatic ? 1f : 0.5f;
                    var force = error * power * direction / distance;

                    if (!lineConstraint.Body1.IsStatic)
                        lineConstraint.Body1.CurrentPosition += force;

                    if (!lineConstraint.Body2.IsStatic)
                        lineConstraint.Body2.CurrentPosition -= force;
                }

                // Collisions.
                foreach (var body in _bodies)
                {
                    if (body.IsStatic || body.Radius <= 0f)
                        continue;

                    foreach (var otherBody in _bodies)
                    {
                        if (body == otherBody) 
                            continue;

                        var direction = body.CurrentPosition - otherBody.CurrentPosition;
                        var squaredDistance = direction.LengthSquared();

                        var collisionRadius = body.Radius + otherBody.Radius;
                        var isCollided = squaredDistance <= collisionRadius * collisionRadius;
                        if (isCollided)
                        {
                            var distance = MathF.Sqrt(squaredDistance);
                            var error = collisionRadius - distance;
                            var force = error * direction / distance;

                            if (otherBody.IsStatic)
                            {
                                body.CurrentPosition += force;
                            }
                            else
                            {
                                var collisionMass = body.Mass + otherBody.Mass;
                                var power = otherBody.Mass / collisionMass;
                                var otherPower = body.Mass / collisionMass;

                                body.CurrentPosition += force * power;
                                otherBody.CurrentPosition -= force * otherPower;
                            }
                        }
                    }

                    if (body.CurrentPosition.Y + body.Radius > Floor)
                    {
                        body.CurrentPosition = body.CurrentPosition with { Y = Floor - body.Radius };
                    }
                }
            }
        }
    }

    public class LineConstraint
    {
        public float Length { get; set; }
        public VerletBody Body1 { get; set; }
        public VerletBody Body2 { get; set; }
    }

    public class VerletBody
    {
        public bool IsStatic { get; set; }
        public float Radius { get; set; }
        public float Mass { get; set; } = 1f;

        public Vector3 CurrentPosition { get; set; }
        public Vector3 LastPosition { get; set; }
    }
}
