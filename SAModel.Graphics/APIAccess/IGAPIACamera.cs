using SATools.SAModel.Structs;

namespace SATools.SAModel.Graphics.APIAccess
{
    /// <summary>
    /// Camera API Access
    /// </summary>
    public interface IGAPIACamera
    {
        /// <summary>
        /// Updates the camera directions when the cameras rotation changes
        /// </summary>
        /// <param name="up"></param>
        /// <param name="forward"></param>
        /// <param name="right"></param>
        void UpdateDirections(Vector3 rotation, out Vector3 up, out Vector3 forward, out Vector3 right);

        /// <summary>
        /// Sets the projection matrix to a new orthographic matrix
        /// </summary>
        /// <param name="width">Ortho width in world-units</param>
        /// <param name="height">Ortho height in world-units</param>
        /// <param name="zNear">Nearplane</param>
        /// <param name="zFar">Farplane</param>
        void SetOrtographicMatrix(float width, float height, float zNear, float zFar);

        /// <summary>
        /// Sets the projection matrix to a new perspective matrix
        /// </summary>
        /// <param name="fovy">Fov in radians</param>
        /// <param name="aspect">Output aspect ratio</param>
        /// <param name="zNear">Nearplane</param>
        /// <param name="zFar">Farplane</param>
        void SetPerspectiveMatrix(float fovy, float aspect, float zNear, float zFar);

        /// <summary>
        /// Sets the view matrix
        /// </summary>
        /// <param name="position">Camera position</param>
        /// <param name="rotation">Camera rotation</param>
        void SetViewMatrix(Vector3 position, Vector3 rotation);

        void SetOrbitViewMatrix(Vector3 position, Vector3 rotation, Vector3 orbitOffset);

        /// <summary>
        /// Returns a position in world space as view position
        /// </summary>
        /// <param name="position">Position to transform</param>
        /// <returns></returns>
        Vector3 ToViewPos(Vector3 position);
    }
}
