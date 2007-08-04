#region MIT License
/*
 * Copyright (c) 2005-2007 Jonathan Mark Porter. http://physics2d.googlepages.com/
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal 
 * in the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of 
 * the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be 
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion




#if UseDouble
using Scalar = System.Double;
#else
using Scalar = System.Single;
#endif
using System;

using AdvanceMath;
using AdvanceMath.Geometry2D;
using Physics2DDotNet.Math2D;

namespace Physics2DDotNet
{
#if !CompactFramework && !WindowsCE && !PocketPC && !XBOX360 
    [Serializable]
#endif
    public sealed class MultiPartPolygon : Shape
    {
        private static Vector2D[] ConcatVertexes(Vector2D[][] polygons)
        {
            if (polygons == null) { throw new ArgumentNullException("polygons"); }
            if (polygons.Length == 0) { throw new ArgumentOutOfRangeException("polygons"); }
            int totalLength = 0;
            Vector2D[] polygon;
            for (int index = 0; index < polygons.Length; ++index)
            {
                polygon = polygons[index];
                if (polygon == null) { throw new ArgumentNullException("polygons"); }
                totalLength += polygon.Length;
            }
            Vector2D[] result = new Vector2D[totalLength];
            int offset = 0;
            for (int index = 0; index < polygons.Length; ++index)
            {
                polygon = polygons[index];
                polygon.CopyTo(result, offset);
                offset += polygon.Length;
            }
            return result;
        }
        /// <summary>
        /// Gets the Inertia of a MultiPartPolygon
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static Scalar InertiaOfMultiPartPolygon(Vector2D[][] polygons)
        {
            if (polygons == null) { throw new ArgumentNullException("polygons"); }
            if (polygons.Length == 0) { throw new ArgumentOutOfRangeException("polygons"); }
            Scalar denom = 0;
            Scalar numer = 0;
            Scalar a, b, c, d;
            Vector2D v1, v2;
            for (int polyIndex = 0; polyIndex < polygons.Length; ++polyIndex)
            {
                Vector2D[] vertexes = polygons[polyIndex];
                if (vertexes == null) { throw new ArgumentNullException("polygons"); }
                if (vertexes.Length == 0) { throw new ArgumentOutOfRangeException("polygons"); }
                if (vertexes.Length == 1) { break; }
                v1 = vertexes[vertexes.Length - 1];
                for (int index = 0; index < vertexes.Length; index++, v1 = v2)
                {
                    v2 = vertexes[index];
                    Vector2D.Dot(ref v2, ref v2, out a);
                    Vector2D.Dot(ref v2, ref v1, out b);
                    Vector2D.Dot(ref v1, ref v1, out c);
                    Vector2D.ZCross(ref v1, ref v2, out d);
                    d = Math.Abs(d);
                    numer += d;
                    denom += (a + b + c) * d;
                }
            }
            if (numer == 0) { return 1; }
            return denom / (numer * 6);
        }

        public static Scalar GetArea(Vector2D[][] polygons)
        {
            if (polygons == null) { throw new ArgumentNullException("polygons"); }
            if (polygons.Length == 0) { throw new ArgumentOutOfRangeException("polygons"); }
            Scalar result = 0;
            Scalar temp;
            Vector2D[] polygon;
            for (int index = 0; index < polygons.Length; ++index)
            {
                polygon = polygons[index];
                if (polygon == null) { throw new ArgumentNullException("polygons"); }
                BoundingPolygon.GetArea(polygon, out temp);
                result += temp;
            }
            return result;
        }

        public static Vector2D GetCentroid(Vector2D[][] polygons)
        {
            if (polygons == null) { throw new ArgumentNullException("polygons"); }
            if (polygons.Length == 0) { throw new ArgumentOutOfRangeException("polygons"); }

            
            Scalar temp, area, areaTotal;
            Vector2D v1, v2;
            Vector2D[] vertices;
            areaTotal = 0;
            Vector2D result = Vector2D.Zero;
            for (int index1 = 0; index1 < polygons.Length; ++index1)
            {
                vertices = polygons[index1];
                if (vertices == null) { throw new ArgumentNullException("polygons"); }
                if (vertices.Length < 3) { throw new ArgumentOutOfRangeException("polygons", "There must be at least 3 vertices"); }
                v1 = vertices[vertices.Length - 1];
                area = 0;
                for (int index = 0; index < vertices.Length; ++index, v1 = v2)
                {
                    v2 = vertices[index];
                    Vector2D.ZCross(ref v1, ref v2, out temp);
                    area += temp;
                    result.X += ((v1.X + v2.X) * temp);
                    result.Y += ((v1.Y + v2.Y) * temp);
                }
                areaTotal += Math.Abs(area);
            }
            temp = 1 / (areaTotal * 3);
            result.X *= temp;
            result.Y *= temp;
            return result;
        }

        public static Vector2D[][] MakeCentroidOrigin(Vector2D[][] polygons)
        {
            Vector2D centroid = GetCentroid(polygons);
            Vector2D[][] result = new Vector2D[polygons.Length][];
            for (int index = 0; index < polygons.Length; ++index)
            {
                result[index] = OperationHelper.ArrayRefOp<Vector2D, Vector2D, Vector2D>(polygons[index], ref centroid, Vector2D.Subtract);
            }
            return result;
        }

        private Vector2D[][] polygons;


        private DistanceGrid grid; 

        #region constructors

        public MultiPartPolygon(Vector2D[][] polygons, Scalar gridSpacing)
            : this(polygons, gridSpacing, InertiaOfMultiPartPolygon(polygons)) { }

        public MultiPartPolygon(Vector2D[][] polygons, Scalar gridSpacing, Scalar momentOfInertiaMultiplier)
            : base(ConcatVertexes(polygons), momentOfInertiaMultiplier)
        {
            if (gridSpacing <= 0) { throw new ArgumentOutOfRangeException("gridSpacing"); }
            this.polygons = polygons;
            this.grid = new DistanceGrid(this, gridSpacing);
        }
        
        private MultiPartPolygon(MultiPartPolygon copy)
            : base(copy)
        {
            this.grid = copy.grid;
            this.polygons = copy.polygons;
        }
        #endregion

        public Vector2D[][] Polygons
        {
            get { return polygons; }
        }

        public override bool CanGetIntersection
        {
            get { return true; }
        }

        public override bool CanGetDistance
        {
            get { return true; }
        }
        public override bool BroadPhaseDetectionOnly
        {
            get { return false; }
        }
        public override bool CanGetCustomIntersection
        {
            get { return false; }
        }

        protected override void CalcBoundingRectangle()
        {
            BoundingRectangle.FromVectors(base.vertexes, out rect);
        }

        public override bool TryGetIntersection(Vector2D vector, out IntersectionInfo info)
        {
            Vector2D local;
            Vector2D.Transform(ref matrix2DInv.VertexMatrix, ref vector, out local);
            if (grid.TryGetIntersection(local, out info))
            {
                Vector2D.Transform(ref matrix2D.NormalMatrix, ref info.Normal, out info.Normal);
                info.Position = vector;
                return true;
            }
            return false;
        }

        public override void GetDistance(ref Vector2D vector, out Scalar result)
        {
            result = Scalar.MaxValue;
            Scalar temp;
            for (int index = 0; index < polygons.Length; ++index)
            {
                BoundingPolygon.GetDistance(polygons[index], ref vector, out temp);
                if (temp < result)
                {
                    result = temp;
                }
            }
        }
        public override bool TryGetCustomIntersection(Body other, out object customIntersectionInfo)
        {
            throw new NotSupportedException();
        }
        public override Shape Duplicate()
        {
            return new MultiPartPolygon(this);
        }
    }
}