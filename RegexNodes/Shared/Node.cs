﻿using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegexNodes.Shared
{
    public interface INodeOutput
    {
        Vector2L OutputPos { get; }
        string CssName { get; }
        string CssColor { get; }

        string GetOutput();
    }

    public interface INode : IPositionable, INodeOutput
    {
        string Title { get; }
        string NodeInfo { get; }
        bool IsCollapsed { get; set; }

        string CachedValue { get; set; }
        string GetValueAndUpdateCache();

        List<NodeInput> NodeInputs { get; }
        InputProcedural PreviousNode { get; }

        void MoveBy(long x, long y);
        void MoveBy(Vector2L delta);
        void CalculateInputsPos();
        IEnumerable<NodeInput> GetInputsRecursive();
    }

    public abstract class Node : INode
    {
        private Vector2L pos;

        public Vector2L Pos
        {
            get => pos;
            set
            {
                pos = value;
                CalculateInputsPos();
            }
        }
        public InputProcedural PreviousNode { get; } = new InputProcedural();

        private readonly List<NodeInput> nodeInputs;
        public virtual List<NodeInput> NodeInputs => nodeInputs;
        public abstract string Title { get; }
        public abstract string NodeInfo { get; }

        public string CachedValue { get; set; } = "";

        public Vector2L OutputPos => Pos + new Vector2L(150, 14);

        public bool IsCollapsed { get; set; }

        public Node()
        {
            nodeInputs = GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(prop => Attribute.IsDefined(prop, typeof(NodeInputAttribute)))
                    .Select(prop => prop.GetValue(this) as NodeInput)
                    .ToList();
            
            UpdateCache(GetValue());
        }

        [Obsolete("Update cache in derived class instead")]
        public string GetValueAndUpdateCache()
        {
            string value = GetValue();
            //Console.WriteLine("Updating cache value to " + value);
            CachedValue = value;
            return value;
        }

        /// <summary>
        /// Set the position of each input based on the position of the node
        /// </summary>
        public void CalculateInputsPos()
        {
            //TODO: refactor using GetHeight() on each input
            PreviousNode.Pos = new Vector2L(Pos.x + 2, Pos.y + 13);
            if (IsCollapsed)
            {
                int startHeight = 13;
                foreach (var input in NodeInputs)
                {
                    switch (input)
                    {
                        case InputProcedural input1:
                            input1.Pos = new Vector2L(Pos.x + 2, Pos.y + startHeight);
                            break;
                        case InputCollection input1:
                            input1.Pos = new Vector2L(Pos.x + 2, Pos.y + startHeight);
                            foreach (var input2 in input1.Inputs)
                            {
                                input2.Pos = new Vector2L(Pos.x + 2, Pos.y + startHeight);
                            }
                            break;
                    }
                }
            }
            else
            {
                int startHeight = 44;
                int inputHeight = 32;
                //TODO: Support disabled inputs
                foreach (var input in NodeInputs)
                {
                    switch (input)
                    {
                        case InputProcedural input1:
                            input1.Pos = new Vector2L(Pos.x, Pos.y + startHeight);
                            startHeight += inputHeight;
                            break;
                        case InputCollection input1:
                            startHeight += 28;
                            input1.Pos = new Vector2L(Pos.x, Pos.y + startHeight);
                            foreach (var input2 in input1.Inputs)
                            {
                                input2.Pos = new Vector2L(Pos.x, Pos.y + startHeight);
                                startHeight += inputHeight;
                            }
                            break;
                        default:
                            startHeight += 50;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Get all of the inputs to the node, including the 'previous' input and the sub-inputs of any InputCollections.
        /// InputCollections themselves are not returned.
        /// </summary>
        public IEnumerable<NodeInput> GetInputsRecursive()
        {
            yield return PreviousNode;
            foreach(var input in nodeInputs)
            {
                if(input is InputCollection coll)
                {
                    foreach (var subInput in coll.Inputs)
                        yield return subInput;
                }
                else
                {
                    yield return input;
                }
            }
        }

        public string CssName => Title.Replace(" ", "").ToLowerInvariant();
        public string CssColor => $"var(--col-node-{CssName})";

        public string UpdateCache(string result)
        {
            CachedValue = result;
            return result;
        }

        public void MoveBy(long x, long y)
        {
            Pos = new Vector2L(Pos.x + x, Pos.y + y);
            CalculateInputsPos();
        }
        public void MoveBy(Vector2L delta) => MoveBy(delta.x, delta.y);

        public virtual string GetOutput()
        {
            //TODO: use cache
            return PreviousNode.InputNode?.GetOutput() + GetValue();
        }

        protected abstract string GetValue();
    }
}
