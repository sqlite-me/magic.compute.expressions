using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeOperatorSingle : NodeOperator
    {
        public NodeOperatorSingle(string @operator, int orignalIndex)
            : base(NodeType.Operator_Single, @operator, @operator, orignalIndex)
        {
        }

        public NodeData Target { get; set; }

        public override Expression GetExpression()
        {
            Expression expression;
            switch (Operator)
            {
                case "!":
                    expression = Expression.IsFalse(Target.GetExpression());
                    break;
                case "~":
                    expression = Expression.Not(Target.GetExpression());
                    break;
                default:
                    throw new NotImplementedException(Operator);
            }
            return expression;
        }
    }
}
