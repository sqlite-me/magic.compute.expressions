using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeBracket : NodeData
    {
        public NodeBracket(string orginalStr, int orignalIndex)
            : base(NodeType.Bracket, orginalStr, orignalIndex)
        {
        }
        public int Start => OrignalIndex;
        public int Length => OrginalStr.Length;
        public List<NodeBracket> BracketNodes { get; } = new List<NodeBracket>();
        public bool Closed => Length > 0;
        public List<NodeData> NodeDatas { get; } = new List<NodeData>();

        public void AddNode(NodeData nodeData)
        {
            nodeData.Parent = this;
            NodeDatas.Add(nodeData);
        }
        public void AddBracketNode(NodeBracket nodeBracket)
        {
            AddNode(nodeBracket);
            BracketNodes.Add(nodeBracket);
        }

        public override Expression GetExpression()
        {
            return NodeDatas[0].GetExpression();
        }
    }
}
