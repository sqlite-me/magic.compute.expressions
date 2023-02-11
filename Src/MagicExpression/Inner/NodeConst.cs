using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeConst : NodeData
    {
        public NodeConst(object? value, string orginalStr, int orignalIndex) : base(NodeType.Const, orginalStr, orignalIndex)
        {
            ConstantExpression = Expression.Constant(value);
        }

        public ConstantExpression ConstantExpression { get; }

        public override Expression GetExpression()
        {
            return ConstantExpression;
        }
    }
}
