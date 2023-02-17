using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal abstract class NodeData
    {
        public NodeData(NodeType nodeType, string orginalStr, int orignalIndex)
        {
            this.NodeType = nodeType;
            OrginalStr = orginalStr;
            OrignalIndex = orignalIndex;
        }
        public NodeType NodeType { get; }
        public NodeData? Parent { get; set; }
        public string OrginalStr { get; }
        public int OrignalIndex { get; }
        public abstract Expression GetExpression();
    }
}
