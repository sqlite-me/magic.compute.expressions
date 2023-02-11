using System;
using System.Collections.Generic;
using System.Text;

namespace MagicExpression.Inner
{
    internal abstract class NodeOperator : NodeData
    {
        public NodeOperator(NodeType nodeType, string @operator, string orginalStr, int orignalIndex)
            : base(nodeType, orginalStr, orignalIndex)
        {
            this.Operator = @operator;
        }

        public string Operator { get; }
    }
}
