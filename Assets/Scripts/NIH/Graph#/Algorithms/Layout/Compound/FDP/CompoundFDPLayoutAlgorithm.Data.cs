﻿using System;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Linq;
using QuickGraph;
using System.Collections.Generic;
using System.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Compound.FDP
{
    public partial class CompoundFDPLayoutAlgorithm<TVertex, TEdge, TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        /// <summary>
        /// Informations for compound vertices.
        /// </summary>
        private readonly IDictionary<TVertex, CompoundVertexData> _compoundVertexDatas =
            new Dictionary<TVertex, CompoundVertexData>();

        /// <summary>
        /// Informations for the simple vertices.
        /// </summary>
        private readonly IDictionary<TVertex, SimpleVertexData> _simpleVertexDatas =
            new Dictionary<TVertex, SimpleVertexData>();

        /// <summary>
        /// Informations for all kind of vertices.
        /// </summary>
        private readonly IDictionary<TVertex, VertexData> _vertexDatas =
            new Dictionary<TVertex, VertexData>();

        /// <summary>
        /// The levels of the graph (generated by the containment associations).
        /// </summary>
        private readonly IList<HashSet<TVertex>> _levels =
            new List<HashSet<TVertex>>();

        public IList<HashSet<TVertex>> Levels
        {
            get { return _levels; }
        }

        private class RemovedTreeNodeData<TVertex1, TEdge1>
        {
            public readonly TVertex1 Vertex;
            public readonly TEdge1 Edge;

            public RemovedTreeNodeData(TVertex1 vertex, TEdge1 edge)
            {
                Vertex = vertex;
                Edge = edge;
            }
        }

        /// <summary>
        /// The list of the removed root-tree-nodes and edges by it's level
        /// (level = distance from the closest not removed node).
        /// </summary>
        private readonly Stack<IList<RemovedTreeNodeData<TVertex,TEdge>>> _removedRootTreeNodeLevels =
            new Stack<IList<RemovedTreeNodeData<TVertex,TEdge>>>();

        private readonly HashSet<TVertex> _removedRootTreeNodes =
            new HashSet<TVertex>();

        private readonly HashSet<TEdge> _removedRootTreeEdges =
            new HashSet<TEdge>();

        /// <summary>
        /// Temporary dictionary for the inner canvas sizes (do not depend on this!!! inside 
        /// the algorithm, use the vertexData objects instead).
        /// </summary>
        private IDictionary<TVertex, float2> _innerCanvasSizes;

        /// <summary>
        /// The dictionary of the initial vertex sizes.
        /// DO NOT USE IT AFTER THE INITIALIZATION.
        /// </summary>
        private readonly IDictionary<TVertex, float2> _vertexSizes;

        /// <summary>
        /// The dictionary of the vertex bordex.
        /// DO NOT USE IT AFTER THE INITIALIZATION.
        /// </summary>
        // private readonly IDictionary<TVertex, Thickness> _vertexBorders;

        /// <summary>
        /// The dictionary of the layout types of the compound vertices.
        /// DO NOT USE IT AFTER THE INITIALIZATION.
        /// </summary>
        private readonly IDictionary<TVertex, CompoundVertexInnerLayoutType> _layoutTypes;

        private readonly IMutableCompoundGraph<TVertex, TEdge> _compoundGraph;

        /// <summary>
        /// Represents the root vertex.
        /// </summary>
        private readonly CompoundVertexData _rootCompoundVertex =
            new CompoundVertexData(
                null, null, false, new float2(),
                new float2(), //new Thickness(),
                CompoundVertexInnerLayoutType.Automatic);

        #region Constructors
        public CompoundFDPLayoutAlgorithm(
            TGraph visitedGraph,
            IDictionary<TVertex, float2> vertexSizes,
            // IDictionary<TVertex, Thickness> vertexBorders,
            IDictionary<TVertex, CompoundVertexInnerLayoutType> layoutTypes)
            : this(visitedGraph, vertexSizes, /*vertexBorders,*/ layoutTypes, null, null)
        {
        }

        public CompoundFDPLayoutAlgorithm(
            TGraph visitedGraph,
            IDictionary<TVertex, float2> vertexSizes,
            // IDictionary<TVertex, Thickness> vertexBorders,
            IDictionary<TVertex, CompoundVertexInnerLayoutType> layoutTypes,
            IDictionary<TVertex, float2> vertexPositions,
            CompoundFDPLayoutParameters oldParameters)
            : base(visitedGraph, vertexPositions, oldParameters)
        {
            _vertexSizes = vertexSizes;
            //_vertexBorders = vertexBorders;
            _layoutTypes = layoutTypes;

            if (VisitedGraph is ICompoundGraph<TVertex, TEdge>)
                _compoundGraph = new CompoundGraph<TVertex, TEdge>(VisitedGraph as ICompoundGraph<TVertex, TEdge>);
            else
                _compoundGraph = new CompoundGraph<TVertex, TEdge>(VisitedGraph);
        }
        #endregion

        public int LevelOfVertex(TVertex vertex)
        {
            return _vertexDatas[vertex].Level;
        }

        #region ICompoundLayoutAlgorithm<TVertex,TEdge,TGraph> Members

        public IDictionary<TVertex, float2> InnerCanvasSizes
        {
            get
            {
                return _compoundVertexDatas.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.InnerCanvasSize);
            }
        }

        #endregion

        #region Nested type: CompoundVertexData

        /// <summary>
        /// Data for the compound vertices.
        /// </summary>
        private class CompoundVertexData : VertexData
        {
            /// <summary>
            /// The thickness of the borders of the compound vertex.
            /// </summary>
            // public readonly Thickness Borders;

            /// <summary>
            /// Gets the layout type of the compound vertex.
            /// </summary>
            public readonly CompoundVertexInnerLayoutType InnerVertexLayoutType;

            private float2 _innerCanvasSize;

            private float2 _size;

            private ICollection<VertexData> _children;

            public CompoundVertexData(TVertex vertex,
                                      VertexData movableParent,
                                      bool isFixedToParent,
                                      float2 position,
                                      float2 size,
                                      // Thickness borders,
                                      CompoundVertexInnerLayoutType innerVertexLayoutType)
                : base(vertex, movableParent, isFixedToParent, position)
            {
                //Borders = borders;

                //calculate the size of the inner canvas
                InnerCanvasSize = new float2(max(0.0f, size.x),// - Borders.Left - Borders.Right
                                           max(0.0f, size.y));// - Borders.Top - Borders.Bottom
                InnerVertexLayoutType = innerVertexLayoutType;
            }

            /// <summary>
            /// The size of the inner canvas of the compound vertex.
            /// </summary>
            public float2 InnerCanvasSize
            {
                get { return _innerCanvasSize; }
                set
                {
                    _innerCanvasSize = value;

                    //set the size of the canvas
                    _size = new float2(_innerCanvasSize.x,// + Borders.Left + Borders.Right,
                                     _innerCanvasSize.y); //+ Borders.Top + Borders.Bottom
                }
            }

            /// <summary>
            /// The overall size of the vertex (inner canvas size + borders + ...).
            /// </summary>
            public override float2 Size
            {
                get { return _size; }
            }

            /// <summary>
            /// Modifies the position of the children with the given
            /// vector.
            /// </summary>
            /// <param name="force">The vector of the position modification.</param>
            private void PropogateToChildren(float2 force)
            {
                foreach (var child in _children)
                {
                    child.ApplyForce(force);
                }
            }

            public ICollection<VertexData> Children
            {
                get { return _children; }
                set
                {
                    _children = value;
                }
            }

            internal override void ApplyForce(float2 force)
            {
                Position += force;
                PropogateToChildren(force);
                RecalculateBounds();
            }

            public float2 InnerCanvasCenter
            {
                get
                {
                    return new float2(
                        Position.x - Size.x / 2 + /*Borders.Left +*/ InnerCanvasSize.x / 2,
                        Position.y - Size.y / 2 + /*Borders.Top +*/ InnerCanvasSize.y / 2
                        );
                }
                set
                {
                    Position = new float2(
                        value.x - InnerCanvasSize.x / 2 /*- Borders.Left*/ + Size.x / 2,
                        value.y - InnerCanvasSize.y / 2 /*- Borders.Top*/ + Size.y / 2
                        );
                }
            }

            public void RecalculateBounds()
            {
                if (_children == null)
                {
                    InnerCanvasSize = new float2(); //TODO padding
                    return;
                }

                float2 topLeft = new float2(float.PositiveInfinity, float.PositiveInfinity);
                float2 bottomRight = new float2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var child in _children)
                {
                    topLeft.x = Math.Min(topLeft.x, child.Position.x - child.Size.x / 2);
                    topLeft.y = Math.Min(topLeft.y, child.Position.y - child.Size.y / 2);

                    bottomRight.x = Math.Max(bottomRight.x, child.Position.x + child.Size.x / 2);
                    bottomRight.y = Math.Max(bottomRight.y, child.Position.y + child.Size.y / 2);
                }
                InnerCanvasSize = new float2(bottomRight.y - topLeft.y, bottomRight.y - topLeft.y);
                InnerCanvasCenter = new float2((topLeft.x + bottomRight.x) / 2.0f, (topLeft.y + bottomRight.y) / 2.0f);
            }
        }

        #endregion

        #region Nested type: SimpleVertexData

        private class SimpleVertexData : VertexData
        {
            /// <summary>
            /// The size of the vertex.
            /// </summary>
            private readonly float2 _size;

            public SimpleVertexData(TVertex vertex, VertexData movableParent, bool isFixed, float2 position, float2 size)
                : base(vertex, movableParent, isFixed, position)
            {
                _size = size;
            }

            /// <summary>
            /// Gets the actual size of the vertex (inner size + border + anything else...).
            /// </summary>
            public override float2 Size
            {
                get { return _size; }
            }

            internal override void ApplyForce(float2 force)
            {
                Position += force;
            }
        }

        #endregion

        #region Nested type: VertexData

        /// <summary>
        /// Data for the simple vertices.
        /// </summary>
        private abstract class VertexData
        {
            /// <summary>
            /// Gets the vertex which is wrapped by this object.
            /// </summary>
            public readonly TVertex Vertex;
            public CompoundVertexData Parent;
            private float2 _springForce;
            private float2 _repulsionForce;
            private float2 _gravitationForce;
            private float2 _applicationForce;
            private float2 _previousForce;
            private float2 _childrenForce;
            protected VertexData _movableParent;

            protected VertexData(TVertex vertex, VertexData movableParent, bool isFixedToParent, float2 position)
            {
                Vertex = vertex;
                MovableParent = movableParent;
                IsFixedToParent = isFixedToParent;
                Parent = null;
                Position = position;
            }

            /// <summary>
            /// If the vertex is fixed (cannot be moved), that's it's parent
            /// that could be moved (if there's any).
            /// 
            /// This property can only be set once.
            /// </summary>
            public VertexData MovableParent
            {
                get { return _movableParent; }
                set
                {
                    _movableParent = value;
                }
            }

            /// <summary>
            /// Gets or sets that the position of the vertex is fixed to
            /// it's parent vertex or not.
            /// </summary>
            public bool IsFixedToParent { get; set; }

            /// <summary>
            /// Gets the actual size of the vertex (inner size + border + anything else...).
            /// </summary>
            public abstract float2 Size { get; }

            /// <summary>
            /// The level of the vertex inside the graph.
            /// </summary>
            public int Level;

            /// <summary>
            /// The position of the vertex.
            /// </summary>
            public float2 Position;

            /// <summary>
            /// Gets or sets the spring force.
            /// </summary>
            public float2 SpringForce
            {
                get { return IsFixedToParent ? new float2() : _springForce; }
                set
                {
                    if (IsFixedToParent)
                        _springForce = new float2();
                    else _springForce = value;
                }
            }

            /// <summary>
            /// Gets or sets the spring force.
            /// </summary>
            public float2 RepulsionForce
            {
                get { return IsFixedToParent ? new float2() : _repulsionForce; }
                set
                {
                    if (IsFixedToParent)
                        _repulsionForce = new float2();
                    else _repulsionForce = value;
                }
            }

            /// <summary>
            /// Gets or sets the spring force.
            /// </summary>
            public float2 GravitationForce
            {
                get { return IsFixedToParent ? new float2() : _gravitationForce; }
                set
                {
                    if (IsFixedToParent)
                        _gravitationForce = new float2();
                    else _gravitationForce = value;
                }
            }

            /// <summary>
            /// Gets or sets the spring force.
            /// </summary>
            public float2 ApplicationForce
            {
                get { return IsFixedToParent ? new float2() : _applicationForce; }
                set
                {
                    if (IsFixedToParent)
                        _applicationForce = new float2();
                    else _applicationForce = value;
                }
            }

            internal abstract void ApplyForce(float2 force);
            public float2 ApplyForce(float limit)
            {
                var force = _springForce
                    + _repulsionForce
                    + _gravitationForce
                    + _applicationForce
                    + 0.5f * _childrenForce;

                Parent._childrenForce += force;

                if (length(force) > limit)
                    force *= limit / length(force);
                force += 0.7f * _previousForce;
                if (length(force) > limit)
                    force *= limit / length(force);

                ApplyForce(force);
                _springForce = new float2();
                _repulsionForce = new float2();
                _gravitationForce = new float2();
                _applicationForce = new float2();
                _childrenForce = new float2();

                _previousForce = force;
                return force;
            }
        }

        #endregion
    }
}