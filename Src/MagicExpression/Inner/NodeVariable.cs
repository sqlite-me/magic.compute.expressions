using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeVariable : NodeData
    {
        public NodeVariable(NodeOperatorType nodeOperatorType, int orignalIndex) : base(NodeType.Variable, nodeOperatorType.VariableName, orignalIndex)
        {
            this.NodeOperatorTypeIs = nodeOperatorType;
        }

        public NodeOperatorType NodeOperatorTypeIs { get; }

        public override Expression GetExpression()
        {
            return NodeOperatorTypeIs.GetVariableExpression();
        }
    }
}
