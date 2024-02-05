using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace magic.compute.expressions.Inner
{
    internal class NodeOperatorTernary : NodeOperator
    {
        public NodeOperatorTernary(string allExpressoin, int startIndex, int endIndex=-1)
            : base(allExpressoin, startIndex, endIndex)
        {
        }


        public NodeData First { get; set; }
        public NodeData Second { get; set; }
        public NodeData Third { get; set; }
        public override bool NodeComplated => First != null && Second != null && Third != null;
        public override Expression GetExpression()
        {
            var condition = First.GetExpression();
            var second = Second.GetExpression();
            var third = Third.GetExpression();
            if (second.Type != third.Type)
            {
                if (second.Type.IsSubclassOf(third.Type))
                {
                    second = Expression.Convert(second, third.Type);
                }
                else if (third.Type.IsSubclassOf(second.Type))
                {
                    third = Expression.Convert(third, second.Type);
                }
                else
                {
                    var secondTypeCode = Type.GetTypeCode(second.Type);
                    var thirdTypeCode = Type.GetTypeCode(third.Type);
                    if (secondTypeCode > thirdTypeCode && thirdTypeCode >= TypeCode.Char && secondTypeCode <= TypeCode.Decimal)
                    {
                        third = Expression.Convert(third, second.Type);
                    }
                    else if (secondTypeCode < thirdTypeCode && secondTypeCode >= TypeCode.Char && thirdTypeCode <= TypeCode.Decimal)
                    {
                        second = Expression.Convert(second, third.Type);
                    }
                    else
                    {
                        if (secondTypeCode != TypeCode.Object)
                        {
                            second = Expression.Convert(second, typeof(object));
                        }
                        if (thirdTypeCode != TypeCode.Object)
                        {
                            third = Expression.Convert(third, typeof(object));
                        }
                    }
                }
            }
            return Expression.Condition(Expression.Convert(condition,typeof(bool)), second, third);
        }
    }
}
