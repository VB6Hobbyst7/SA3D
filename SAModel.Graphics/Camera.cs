using SATools.SAModel.Structs;
using System;
using System.Numerics;
using static SATools.SACommon.MathHelper;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Camera class
    /// </summary>
    public sealed class Camera
    {
        public const float NearPlane = 1f;

        #region private field

        /// <summary>
        /// For <see cref="Position"/>
        /// </summary>
        private Vector3 _position;

        /// <summary>
        /// for <see cref="Rotation"/>
        /// </summary>
        private Vector3 _rotation;

        /// <summary>
        /// for <see cref="Forward"/>
        /// </summary>
        private Vector3 _forward;

        /// <summary>
        /// for <see cref="Right"/>
        /// </summary>
        private Vector3 _right;

        /// <summary>
        /// for <see cref="Up"/>
        /// </summary>
        private Vector3 _up;

        /// <summary>
        /// for <see cref="Orthographic"/>
        /// </summary>
        private bool _orthographic;

        /// <summary>
        /// for <see cref="Orbiting"/>
        /// </summary>
        private bool _orbiting;

        /// <summary>
        /// for <see cref="Distance"/>
        /// </summary>
        private float _distance;

        /// <summary>
        /// for <see cref="FieldOfView"/>
        /// </summary>
        private float _fov;

        /// <summary>
        /// for <see cref="Aspect"/>
        /// </summary>
        private float _aspect;

        /// <summary>
        /// for <see cref="ViewDistance"/>
        /// </summary>
        private float _viewDist;

        #endregion

        #region properties

        /// <summary>
        /// Position of the camera in world space <br/>
        /// Position of focus in orbit mode
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// Position of camera in world space (regardless of orbit mode
        /// </summary>
        public Vector3 Realposition
            => _position - _forward * _distance;

        /// <summary>
        /// The rotation of the camera in world space
        /// </summary>
        public Vector3 Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                UpdateDirections();
            }
        }

        /// <summary>
        /// The Cameras global forward Direction
        /// </summary>
        public Vector3 Forward
            => _forward;

        /// <summary>
        /// The Cameras global right Direction
        /// </summary>
        public Vector3 Right
            => _right;

        /// <summary>
        /// The Cameras global up Direction
        /// </summary>
        public Vector3 Up
            => _up;

        /// <summary>
        /// Whether the control scheme is set to orbiting
        /// </summary>
        public bool Orbiting
        {
            get => _orbiting;
            set
            {
                if (_orbiting == value)
                    return;
                _orbiting = value;

                var t = _forward * _distance;
                Position += _orbiting ? t : -t;

                UpdateViewMatrix();
                UpdateProjectionMatrix();
            }
        }

        /// <summary>
        /// The orbiting distance
        /// </summary>
        public float Distance
        {
            get => _distance;
            set
            {
                _distance = Math.Min(_viewDist, Math.Max(NearPlane, value));
                UpdateViewMatrix();
                UpdateProjectionMatrix();
            }
        }

        /// <summary>
        /// The field of view
        /// </summary>
        public float FieldOfView
        {
            get => RadToDeg(_fov);
            set
            {
                _fov = DegToRad(value);
                UpdateProjectionMatrix();
            }
        }

        /// <summary>
        /// The screen aspect
        /// </summary>
        public float Aspect
        {
            get => _aspect;
            set
            {
                _aspect = value;
                UpdateProjectionMatrix();
            }
        }

        /// <summary>
        /// Whether the camera is set to orthographic
        /// </summary>
        public bool Orthographic
        {
            get => _orthographic;
            set
            {
                if (_orthographic == value)
                    return;
                _orthographic = value;
                UpdateViewMatrix();
                UpdateProjectionMatrix();
            }
        }

        /// <summary>
        /// The view/render distance
        /// </summary>
        public float ViewDistance
        {
            get => _viewDist;
            set
            {
                _viewDist = value;
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// Camera View Matrix
        /// </summary>
        public Matrix4x4 ViewMatrix { get; private set; }

        /// <summary>
        /// Camera projection matrix
        /// </summary>
        public Matrix4x4 ProjectionMatrix { get; private set; }

        #endregion

        /// <summary>
        /// Creates a new camera from the resolution ratio
        /// </summary>
        /// <param name="aspect"></param>
        public Camera(float aspect)
        {
            _orbiting = true;
            _distance = 50;
            _fov = DegToRad(50);
            _aspect = aspect;
            _orthographic = false;
            _viewDist = 3000;

            UpdateDirections();
            UpdateProjectionMatrix();
        }

        /// <summary>
        /// Recalculates the directions
        /// </summary>
        private void UpdateDirections()
        {
            Matrix4x4 matX = Matrix4x4.CreateRotationX(DegToRad(_rotation.X));
            Matrix4x4 matY = Matrix4x4.CreateRotationY(DegToRad(_rotation.Y));
            Matrix4x4 matZ = Matrix4x4.CreateRotationZ(DegToRad(_rotation.Z));

            Matrix4x4 rot = Matrix4x4.Transpose(matZ * matY * matX);

            _forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, rot));
            _up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, rot));
            _right = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitX, rot));

            UpdateViewMatrix();
        }

        /// <summary>
        /// Recalculates the view matrix
        /// </summary>
        private void UpdateViewMatrix()
        {
            Matrix4x4 posMtx = Matrix4x4.CreateTranslation(-_position);

            Matrix4x4 matX = Matrix4x4.CreateRotationX(DegToRad(_rotation.X));
            Matrix4x4 matY = Matrix4x4.CreateRotationY(DegToRad(_rotation.Y));
            Matrix4x4 matZ = Matrix4x4.CreateRotationZ(DegToRad(_rotation.Z));

            Matrix4x4 rotMtx = matZ * matY * matX;

            ViewMatrix = posMtx * rotMtx;

            if (_orbiting)
            {
                Vector3 orbitOffset = _forward * (_orthographic ? _viewDist * 0.5f : _distance);
                Matrix4x4 orbitMatrix = Matrix4x4.CreateTranslation(orbitOffset);
                ViewMatrix = orbitMatrix * ViewMatrix;
            }
        }

        /// <summary>
        /// Recalculates projection matrix
        /// </summary>
        private void UpdateProjectionMatrix()
        {
            Matrix4x4 result = default;

            if (_orthographic && _orbiting)
            {
                var invRL = 1.0f / (_distance * _aspect);
                var invTB = 1.0f / _distance;
                var invFN = 1.0f / (_viewDist - NearPlane);

                result.M11 = 2 * invRL;
                result.M22 = 2 * invTB;
                result.M33 = -2 * invFN;

                result.M43 = -(_viewDist + NearPlane) * invFN;
            }
            else
            {
                var top = NearPlane * MathF.Tan(0.5f * _fov);
                var right = top * _aspect;

                result.M11 = 2.0f * NearPlane / (right * 2);
                result.M22 = 2.0f * NearPlane / (top * 2);
                result.M33 = -(_viewDist + NearPlane) / (_viewDist - NearPlane);
                result.M34 = -1;
                result.M43 = -(2.0f * _viewDist * NearPlane) / (_viewDist - NearPlane);
            }
            ProjectionMatrix = result;
        }

        /// <summary>
        /// Checks wether bounds are rendable
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool CanRender(Bounds bounds)
        {
            Vector3 viewLocation = Vector3.Transform(bounds.Position, ViewMatrix);
            return viewLocation.Length() - bounds.Radius <= _viewDist
                && viewLocation.Z <= bounds.Radius;
        }
    }
}
