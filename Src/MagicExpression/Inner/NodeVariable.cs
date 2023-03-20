using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeVariable : NodeData
    {
        public NodeVariable(NodeOperatorType nodeOperatorType,string allExpressoin, int startIndex,int endIndex) : base( allExpressoin, startIndex, endIndex)
        {
            this.NodeOperatorTypeIs = nodeOperatorType;
        }

        public NodeOperatorType NodeOperatorTypeIs { get; }
        public override bool ExpClosed { get => true; set => throw new Exception("can not change in Variable"); }
        public override bool NodeComplated { get => true; }
        public override Expression GetExpression()
        {
            return NodeOperatorTypeIs.GetVariableExpression();
        }
    }
}
