using SATools.SAModel.Graphics.APIAccess;
using SATools.SAModel.Structs;
using System;
using static SATools.SACommon.MathHelper;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Camera class
    /// </summary>
    public sealed class Camera
    {
        private readonly IGAPIACamera _apiAccess;

        private Vector3 _position;
        private Vector3 _rotation;

        private Vector3 _forward;
        private Vector3 _right;
        private Vector3 _up;

        private bool _orthographic;
        private bool _orbiting;
        private float _distance;

        private float _fov;
        private float _aspect;
        private float _viewDist;

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
        {
            get
            {
                return _position - _forward * _distance;
            }
        }

        /// <summary>
        /// The rotation of the camera in world space
        /// </summary>
        public Vector3 Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                _apiAccess.UpdateDirections(_rotation, out _up, out _forward, out _right);
                UpdateViewMatrix();
            }
        }

        /// <summary>
        /// The Cameras global forward Direction
        /// </summary>
        public Vector3 Forward => _forward;

        /// <summary>
        /// The Cameras global right Direction
        /// </summary>
        public Vector3 Right => _right;

        /// <summary>
        /// The Cameras global up Direction
        /// </summary>
        public Vector3 Up => _up;

        /// <summary>
        /// Whether the control scheme is set to orbiting
        /// </summary>
        public bool Orbiting
        {
            get
            {
                return _orbiting;
            }
            set
            {
                if(_orbiting == value)
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
            get
            {
                return _distance;
            }
            set
            {
                _distance = Math.Min(_viewDist, Math.Max(0.0001f, value));
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
                if(_orthographic == value)
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
        /// The direction in which the camera is viewing
        /// </summary>
        public Vector3 ViewDir
        {
            get => _forward;
        }

        /// <summary>
        /// Creates a new camera from the resolution ratio
        /// </summary>
        /// <param name="aspect"></param>
        public Camera(float aspect, IGAPIACamera apiAccess)
        {
            _apiAccess = apiAccess;

            _orbiting = true;
            _distance = 50;
            _fov = DegToRad(50);
            _aspect = aspect;
            _orthographic = false;
            _viewDist = 3000;

            _apiAccess.UpdateDirections(_rotation, out _up, out _forward, out _right);
            UpdateViewMatrix();
            UpdateProjectionMatrix();
        }

        private void UpdateViewMatrix()
        {
            if(_orbiting)
            {
                _apiAccess.SetOrbitViewMatrix(_position, _rotation, _forward * (_orthographic ? _viewDist * 0.5f : _distance));
            }
            else
            {
                _apiAccess.SetViewMatrix(_position, _rotation);
            }
        }

        private void UpdateProjectionMatrix()
        {
            if(_orthographic && _orbiting)
            {
                _apiAccess.SetOrtographicMatrix(_distance * _aspect, _distance, 0.1f, _viewDist);
            }
            else
            {
                _apiAccess.SetPerspectiveMatrix(_fov, _aspect, 0.1f, _viewDist);
            }
        }

        public bool CanRender(Bounds bounds)
        {
            Vector3 viewLocation = _apiAccess.ToViewPos(bounds.Position);
            return viewLocation.Length - bounds.Radius <= _viewDist
                && viewLocation.Z <= bounds.Radius;
        }
    }
}
