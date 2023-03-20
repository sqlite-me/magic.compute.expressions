using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace MagicExpression.Inner
{
    internal class NodeConst : NodeData
    {
        public NodeConst(object? value, string allExpressoin, int startIndex, int endIndex) : base( allExpressoin, startIndex, endIndex)
        {
            ConstantExpression = Expression.Constant(value);
        }

        public ConstantExpression ConstantExpression { get; }
        public Object? Value { get=> ConstantExpression?.Value; }
        public override bool ExpClosed { get => true; set => throw new Exception("can not change in const"); }
        public override bool NodeComplated { get => true;  }
        public override Expression GetExpression()
        {
            return ConstantExpression;
        }
    }
}
